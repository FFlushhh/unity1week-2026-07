using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Playing開始からの経過時間に応じて、設定済みの被写体Prefabを生成します。
/// </summary>
public sealed class SubjectTimelineController : MonoBehaviour
{
    private enum SubjectSpawnMode
    {
        Fixed,
        Random,
    }

    [Serializable]
    private sealed class SubjectSpawnRoute
    {
        [SerializeField]
        private GameObject subjectPrefab;

        [SerializeField]
        private Vector2 spawnPosition = new(-10f, 0f);

        [SerializeField]
        private SubjectMoveDirection moveDirection = SubjectMoveDirection.LeftToRight;

        [SerializeField, Min(0f)]
        private float moveSpeed = 2f;

        [SerializeField, Min(0.01f)]
        private float scale = 1f;

        [SerializeField]
        private bool usePathAnchorForSpawnPosition;

        [SerializeField, Min(0f)]
        private float verticalSwayAmplitude;

        [SerializeField, Min(0f)]
        private float verticalSwayFrequencyHz;

        [SerializeField, Min(0f)]
        private float selectionWeight = 1f;

        public SubjectSpawnRoute() { }

        public SubjectSpawnRoute(
            GameObject subjectPrefab,
            Vector2 spawnPosition,
            SubjectMoveDirection moveDirection,
            float moveSpeed,
            float scale,
            bool usePathAnchorForSpawnPosition,
            float verticalSwayAmplitude,
            float verticalSwayFrequencyHz
        )
        {
            this.subjectPrefab = subjectPrefab;
            this.spawnPosition = spawnPosition;
            this.moveDirection = moveDirection;
            this.moveSpeed = moveSpeed;
            this.scale = scale;
            this.usePathAnchorForSpawnPosition = usePathAnchorForSpawnPosition;
            this.verticalSwayAmplitude = verticalSwayAmplitude;
            this.verticalSwayFrequencyHz = verticalSwayFrequencyHz;
        }

        public GameObject SubjectPrefab => subjectPrefab;

        public Vector2 SpawnPosition => spawnPosition;

        public SubjectMoveDirection MoveDirection => moveDirection;

        public float MoveSpeed => moveSpeed;

        public float Scale => scale;

        public bool UsePathAnchorForSpawnPosition => usePathAnchorForSpawnPosition;

        public float VerticalSwayAmplitude => verticalSwayAmplitude;

        public float VerticalSwayFrequencyHz => verticalSwayFrequencyHz;

        public float SelectionWeight => selectionWeight;
    }

    [Serializable]
    private sealed class SubjectSpawnSetting
    {
        [Header("Spawn Mode")]
        [SerializeField]
        private SubjectSpawnMode spawnMode;

        [Header("Fixed Spawn")]
        [SerializeField]
        private GameObject subjectPrefab;

        [SerializeField, Min(0f)]
        private float spawnTimeSeconds = 1f;

        [SerializeField]
        private Vector2 spawnPosition = new(-10f, 0f);

        [SerializeField]
        private SubjectMoveDirection moveDirection = SubjectMoveDirection.LeftToRight;

        [SerializeField, Min(0f)]
        private float moveSpeed = 2f;

        [SerializeField, Min(0.01f)]
        private float scale = 1f;

        [SerializeField]
        private bool usePathAnchorForSpawnPosition;

        [SerializeField, Min(0f)]
        private float verticalSwayAmplitude;

        [SerializeField, Min(0f)]
        private float verticalSwayFrequencyHz;

        [Header("Random Spawn")]
        [SerializeField, Range(0f, 1f)]
        private float appearanceProbability = 1f;

        [SerializeField, Min(1)]
        private int minimumSpawnCount = 1;

        [SerializeField, Min(1)]
        private int maximumSpawnCount = 1;

        [SerializeField, Min(0f)]
        private float earliestSpawnTimeSeconds;

        [SerializeField, Min(0f)]
        private float latestSpawnTimeSeconds = 1f;

        [SerializeField, Min(0f)]
        private float minimumSpawnIntervalSeconds;

        [SerializeField]
        private SubjectSpawnRoute[] randomRoutes;

        public bool IsRandom => spawnMode == SubjectSpawnMode.Random;

        public float FixedSpawnTimeSeconds => spawnTimeSeconds;

        public SubjectSpawnRoute CreateFixedRoute()
        {
            return new SubjectSpawnRoute(
                subjectPrefab,
                spawnPosition,
                moveDirection,
                moveSpeed,
                scale,
                usePathAnchorForSpawnPosition,
                verticalSwayAmplitude,
                verticalSwayFrequencyHz
            );
        }

        public bool TryCreateRandomRequest(out SubjectSpawnScheduleRequest request)
        {
            request = default;

            if (randomRoutes == null)
            {
                return false;
            }

            var routeWeights = new float[randomRoutes.Length];
            for (var index = 0; index < randomRoutes.Length; index++)
            {
                if (randomRoutes[index] == null)
                {
                    return false;
                }

                routeWeights[index] = randomRoutes[index].SelectionWeight;
            }

            request = new SubjectSpawnScheduleRequest(
                appearanceProbability,
                minimumSpawnCount,
                maximumSpawnCount,
                earliestSpawnTimeSeconds,
                latestSpawnTimeSeconds,
                minimumSpawnIntervalSeconds,
                routeWeights
            );
            return true;
        }

        public bool TryGetRandomRoute(int routeIndex, out SubjectSpawnRoute route)
        {
            route = null;
            if (
                randomRoutes == null
                || routeIndex < 0
                || routeIndex >= randomRoutes.Length
                || randomRoutes[routeIndex] == null
            )
            {
                return false;
            }

            route = randomRoutes[routeIndex];
            return true;
        }
    }

    private readonly struct ScheduledSubjectSpawn
    {
        public ScheduledSubjectSpawn(float spawnTimeSeconds, SubjectSpawnRoute route, int sortOrder)
        {
            SpawnTimeSeconds = spawnTimeSeconds;
            Route = route;
            SortOrder = sortOrder;
        }

        public float SpawnTimeSeconds { get; }

        public SubjectSpawnRoute Route { get; }

        public int SortOrder { get; }
    }

    [SerializeField]
    private Stage0Controller stageController;

    [SerializeField]
    private Transform subjectSpawnRoot;

    [SerializeField]
    private SubjectSpawnSetting[] spawnSettings;

    private readonly List<GameObject> spawnedSubjects = new();
    private readonly List<ScheduledSubjectSpawn> scheduledSpawns = new();
    private System.Random spawnRandom;
    private bool hasStoppedForTerminalState;
    private float elapsedTimeSeconds;
    private int nextScheduledSpawnIndex;

    private void Awake()
    {
        spawnRandom = new System.Random(
            unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode())
        );
    }

    private void Start()
    {
        ResetTimeline();

        if (stageController == null)
        {
            Debug.LogError("[SubjectTimelineController] Stage controller is not assigned.", this);
            return;
        }

        stageController.StateChanged += HandleStageStateChanged;
        if (stageController.CurrentState == Stage0Controller.Stage0State.Playing)
        {
            BuildSpawnSchedule();
        }

        RunTimelineAsync(destroyCancellationToken).Forget();
    }

    private void OnDestroy()
    {
        if (stageController != null)
        {
            stageController.StateChanged -= HandleStageStateChanged;
        }
    }

    private async UniTask RunTimelineAsync(CancellationToken cancellationToken)
    {
        var previousState = stageController.CurrentState;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentState = stageController.CurrentState;
                if (
                    currentState == Stage0Controller.Stage0State.Playing
                    && previousState != Stage0Controller.Stage0State.Playing
                )
                {
                    ResetTimeline();
                    BuildSpawnSchedule();
                }

                if (IsTerminalState(currentState) && !hasStoppedForTerminalState)
                {
                    StopSpawnedSubjects();
                    hasStoppedForTerminalState = true;
                }

                if (IsTimelineRunning(currentState))
                {
                    elapsedTimeSeconds += Time.deltaTime;
                    SpawnDueSubjects();
                }

                previousState = currentState;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // シーン破棄に伴う停止は正常な終了として扱う。
        }
    }

    private void HandleStageStateChanged(Stage0Controller.Stage0State state)
    {
        if (!IsTerminalState(state) || hasStoppedForTerminalState)
        {
            return;
        }

        StopSpawnedSubjects();
        hasStoppedForTerminalState = true;
    }

    private static bool IsTimelineRunning(Stage0Controller.Stage0State state)
    {
        return state == Stage0Controller.Stage0State.Playing
            || state == Stage0Controller.Stage0State.CapturedWaitingForTimeout;
    }

    private static bool IsTerminalState(Stage0Controller.Stage0State state)
    {
        return state == Stage0Controller.Stage0State.GameOver
            || state == Stage0Controller.Stage0State.Completed;
    }

    private void ResetTimeline()
    {
        elapsedTimeSeconds = 0f;
        nextScheduledSpawnIndex = 0;
        hasStoppedForTerminalState = false;
        scheduledSpawns.Clear();
        DestroySpawnedSubjects();
    }

    private void BuildSpawnSchedule()
    {
        if (spawnSettings == null)
        {
            return;
        }

        var sortOrder = 0;
        for (var settingIndex = 0; settingIndex < spawnSettings.Length; settingIndex++)
        {
            var spawnSetting = spawnSettings[settingIndex];
            if (spawnSetting == null)
            {
                Debug.LogError(
                    $"[SubjectTimelineController] Spawn setting {settingIndex} is not assigned.",
                    this
                );
                continue;
            }

            if (!spawnSetting.IsRandom)
            {
                scheduledSpawns.Add(
                    new ScheduledSubjectSpawn(
                        spawnSetting.FixedSpawnTimeSeconds,
                        spawnSetting.CreateFixedRoute(),
                        sortOrder++
                    )
                );
                continue;
            }

            if (!spawnSetting.TryCreateRandomRequest(out var request))
            {
                Debug.LogError(
                    $"[SubjectTimelineController] Random spawn setting {settingIndex} has no valid routes.",
                    this
                );
                continue;
            }

            if (!SubjectSpawnScheduleBuilder.TryBuild(request, spawnRandom, out var schedule))
            {
                Debug.LogError(
                    $"[SubjectTimelineController] Random spawn setting {settingIndex} is invalid.",
                    this
                );
                continue;
            }

            foreach (var entry in schedule)
            {
                if (!spawnSetting.TryGetRandomRoute(entry.RouteIndex, out var route))
                {
                    Debug.LogError(
                        $"[SubjectTimelineController] Random spawn setting {settingIndex} selected an invalid route.",
                        this
                    );
                    continue;
                }

                scheduledSpawns.Add(
                    new ScheduledSubjectSpawn(entry.SpawnTimeSeconds, route, sortOrder++)
                );
            }
        }

        scheduledSpawns.Sort(CompareScheduledSpawns);
    }

    private void SpawnDueSubjects()
    {
        while (
            nextScheduledSpawnIndex < scheduledSpawns.Count
            && elapsedTimeSeconds >= scheduledSpawns[nextScheduledSpawnIndex].SpawnTimeSeconds
        )
        {
            SpawnSubject(scheduledSpawns[nextScheduledSpawnIndex].Route);
            nextScheduledSpawnIndex++;
        }
    }

    private void SpawnSubject(SubjectSpawnRoute spawnRoute)
    {
        if (spawnRoute.SubjectPrefab == null)
        {
            Debug.LogError("[SubjectTimelineController] Subject prefab is not assigned.", this);
            return;
        }

        if (subjectSpawnRoot == null)
        {
            Debug.LogError("[SubjectTimelineController] Subject spawn root is not assigned.", this);
            return;
        }

        var subject = Instantiate(spawnRoute.SubjectPrefab, subjectSpawnRoot);
        subject.transform.localPosition = spawnRoute.SpawnPosition;
        subject.transform.localScale *= spawnRoute.Scale;

        if (!TryPositionPathAnchorAtSpawnPosition(subject, spawnRoute))
        {
            Destroy(subject);
            return;
        }

        var subjectMover = subject.GetComponent<SubjectMover>();
        if (subjectMover == null)
        {
            Debug.LogError(
                "[SubjectTimelineController] Subject prefab has no SubjectMover.",
                subject
            );
            Destroy(subject);
            return;
        }

        subjectMover.Configure(
            spawnRoute.MoveDirection,
            spawnRoute.MoveSpeed,
            spawnRoute.VerticalSwayAmplitude,
            spawnRoute.VerticalSwayFrequencyHz
        );
        spawnedSubjects.Add(subject);
    }

    private bool TryPositionPathAnchorAtSpawnPosition(
        GameObject subject,
        SubjectSpawnRoute spawnRoute
    )
    {
        if (!spawnRoute.UsePathAnchorForSpawnPosition)
        {
            return true;
        }

        if (!subject.TryGetComponent<StageSubject>(out var stageSubject))
        {
            Debug.LogError(
                "[SubjectTimelineController] Subject prefab has no StageSubject for its path anchor.",
                subject
            );
            return false;
        }

        if (stageSubject.PathAnchor == null)
        {
            Debug.LogError(
                "[SubjectTimelineController] Subject prefab has no path anchor assigned.",
                subject
            );
            return false;
        }

        var desiredAnchorPosition = subjectSpawnRoot.TransformPoint(spawnRoute.SpawnPosition);
        subject.transform.position += desiredAnchorPosition - stageSubject.PathAnchor.position;
        return true;
    }

    private void StopSpawnedSubjects()
    {
        foreach (var spawnedSubject in spawnedSubjects)
        {
            if (
                spawnedSubject != null
                && spawnedSubject.TryGetComponent<SubjectMover>(out var subjectMover)
            )
            {
                subjectMover.Stop();
            }
        }
    }

    private void DestroySpawnedSubjects()
    {
        foreach (var spawnedSubject in spawnedSubjects)
        {
            if (spawnedSubject != null)
            {
                spawnedSubject.SetActive(false);
                Destroy(spawnedSubject);
            }
        }

        spawnedSubjects.Clear();
    }

    private static int CompareScheduledSpawns(
        ScheduledSubjectSpawn left,
        ScheduledSubjectSpawn right
    )
    {
        var timeComparison = left.SpawnTimeSeconds.CompareTo(right.SpawnTimeSeconds);
        return timeComparison != 0 ? timeComparison : left.SortOrder.CompareTo(right.SortOrder);
    }
}

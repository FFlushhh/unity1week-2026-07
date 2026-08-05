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

        public bool TryGetInitialRoute(System.Random random, out SubjectSpawnRoute route)
        {
            route = null;
            if (!IsRandom)
            {
                route = CreateFixedRoute();
                return route.SubjectPrefab != null;
            }

            if (random == null || randomRoutes == null || randomRoutes.Length == 0)
            {
                return false;
            }

            var totalWeight = 0f;
            foreach (var randomRoute in randomRoutes)
            {
                if (randomRoute == null)
                {
                    return false;
                }

                totalWeight += Mathf.Max(0f, randomRoute.SelectionWeight);
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            var selectedWeight = (float)(random.NextDouble() * totalWeight);
            foreach (var randomRoute in randomRoutes)
            {
                selectedWeight -= Mathf.Max(0f, randomRoute.SelectionWeight);
                if (selectedWeight <= 0f)
                {
                    route = randomRoute;
                    return true;
                }
            }

            route = randomRoutes[^1];
            return true;
        }
    }

    private readonly struct ScheduledSubjectSpawn
    {
        public ScheduledSubjectSpawn(
            float spawnTimeSeconds,
            SubjectSpawnRoute route,
            bool isHorizontallyMirrored,
            int sortOrder
        )
        {
            SpawnTimeSeconds = spawnTimeSeconds;
            Route = route;
            IsHorizontallyMirrored = isHorizontallyMirrored;
            SortOrder = sortOrder;
        }

        public float SpawnTimeSeconds { get; }

        public SubjectSpawnRoute Route { get; }

        public bool IsHorizontallyMirrored { get; }

        public int SortOrder { get; }
    }

    [SerializeField]
    private Stage1Controller stageController;

    [SerializeField]
    private Transform subjectSpawnRoot;

    [SerializeField]
    private SubjectSpawnSetting[] spawnSettings;

    [SerializeField, Range(0f, 1f)]
    private float oppositeSideProbability = 0.5f;

    [Header("Initial Subjects")]
    [SerializeField, Min(0)]
    private int initialSpawnMinimumCount = 2;

    [SerializeField, Min(0)]
    private int initialSpawnMaximumCount = 3;

    [SerializeField]
    private Vector2 initialSpawnXRange = new(-5f, 5f);

    private readonly List<GameObject> spawnedSubjects = new();
    private readonly List<ScheduledSubjectSpawn> scheduledSpawns = new();
    private readonly HashSet<int> assignedSubjectSortingOrders = new();
    private System.Random spawnRandom;
    private bool hasStoppedForGameOver;
    private bool hasEnteredPlaying;
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
        if (stageController.CurrentState == Stage1Controller.Stage1State.StartMessage)
        {
            BuildSpawnSchedule();
            SpawnInitialSubjects();
        }
        else if (stageController.CurrentState == Stage1Controller.Stage1State.Playing)
        {
            BuildSpawnSchedule();
            hasEnteredPlaying = true;
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
                    currentState == Stage1Controller.Stage1State.Playing
                    && previousState != Stage1Controller.Stage1State.Playing
                )
                {
                    if (hasEnteredPlaying)
                    {
                        ResetTimeline();
                        BuildSpawnSchedule();
                    }

                    hasEnteredPlaying = true;
                }

                if (IsGameOverState(currentState) && !hasStoppedForGameOver)
                {
                    StopSpawnedSubjects();
                    hasStoppedForGameOver = true;
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

    private void HandleStageStateChanged(Stage1Controller.Stage1State state)
    {
        if (!IsGameOverState(state) || hasStoppedForGameOver)
        {
            return;
        }

        StopSpawnedSubjects();
        hasStoppedForGameOver = true;
    }

    private static bool IsTimelineRunning(Stage1Controller.Stage1State state)
    {
        return state != Stage1Controller.Stage1State.GameOver;
    }

    private static bool IsGameOverState(Stage1Controller.Stage1State state)
    {
        return state == Stage1Controller.Stage1State.GameOver;
    }

    private void ResetTimeline(bool destroySpawnedSubjects = true)
    {
        elapsedTimeSeconds = 0f;
        nextScheduledSpawnIndex = 0;
        hasStoppedForGameOver = false;
        scheduledSpawns.Clear();
        assignedSubjectSortingOrders.Clear();
        if (destroySpawnedSubjects)
        {
            DestroySpawnedSubjects();
        }
    }

    private void SpawnInitialSubjects()
    {
        if (spawnSettings == null || initialSpawnMaximumCount <= 0)
        {
            return;
        }

        var candidateRoutes = new List<SubjectSpawnRoute>();
        var candidateSubjectIds = new HashSet<SubjectId>();
        foreach (var spawnSetting in spawnSettings)
        {
            if (
                spawnSetting == null
                || !spawnSetting.TryGetInitialRoute(spawnRandom, out var route)
                || route.SubjectPrefab == null
                || !route.SubjectPrefab.TryGetComponent<StageSubject>(out var stageSubject)
                || !candidateSubjectIds.Add(stageSubject.Id)
            )
            {
                continue;
            }

            candidateRoutes.Add(route);
        }

        var minimumCount = Mathf.Clamp(initialSpawnMinimumCount, 0, candidateRoutes.Count);
        var maximumCount = Mathf.Clamp(
            initialSpawnMaximumCount,
            minimumCount,
            candidateRoutes.Count
        );
        var spawnCount = spawnRandom.Next(minimumCount, maximumCount + 1);
        var minimumX = Mathf.Min(initialSpawnXRange.x, initialSpawnXRange.y);
        var maximumX = Mathf.Max(initialSpawnXRange.x, initialSpawnXRange.y);

        for (var index = 0; index < spawnCount; index++)
        {
            var selectedRouteIndex = spawnRandom.Next(index, candidateRoutes.Count);
            (candidateRoutes[index], candidateRoutes[selectedRouteIndex]) = (
                candidateRoutes[selectedRouteIndex],
                candidateRoutes[index]
            );

            var route = candidateRoutes[index];
            var spawnPosition = route.SpawnPosition;
            spawnPosition.x = Mathf.Lerp(minimumX, maximumX, (float)spawnRandom.NextDouble());
            var initialRoute = new SubjectSpawnRoute(
                route.SubjectPrefab,
                spawnPosition,
                route.MoveDirection,
                route.MoveSpeed,
                route.Scale,
                route.UsePathAnchorForSpawnPosition,
                route.VerticalSwayAmplitude,
                route.VerticalSwayFrequencyHz
            );
            SpawnSubject(initialRoute, ShouldSpawnFromOppositeSide());
        }
    }

    private void BuildSpawnSchedule(bool includeFixedSpawns = true)
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
                if (!includeFixedSpawns)
                {
                    continue;
                }

                scheduledSpawns.Add(
                    new ScheduledSubjectSpawn(
                        spawnSetting.FixedSpawnTimeSeconds,
                        spawnSetting.CreateFixedRoute(),
                        ShouldSpawnFromOppositeSide(),
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
                    new ScheduledSubjectSpawn(
                        entry.SpawnTimeSeconds,
                        route,
                        ShouldSpawnFromOppositeSide(),
                        sortOrder++
                    )
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
            var scheduledSpawn = scheduledSpawns[nextScheduledSpawnIndex];
            SpawnSubject(scheduledSpawn.Route, scheduledSpawn.IsHorizontallyMirrored);
            nextScheduledSpawnIndex++;
        }

        if (scheduledSpawns.Count > 0 && nextScheduledSpawnIndex >= scheduledSpawns.Count)
        {
            StartNextRandomSpawnBatch();
        }
    }

    private void StartNextRandomSpawnBatch()
    {
        elapsedTimeSeconds = 0f;
        nextScheduledSpawnIndex = 0;
        scheduledSpawns.Clear();
        BuildSpawnSchedule(includeFixedSpawns: false);
    }

    private void SpawnSubject(SubjectSpawnRoute spawnRoute, bool isHorizontallyMirrored)
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

        var spawnPosition = spawnRoute.SpawnPosition;
        var moveDirection = spawnRoute.MoveDirection;
        if (isHorizontallyMirrored)
        {
            spawnPosition.x = -spawnPosition.x;
            moveDirection = ReverseMoveDirection(moveDirection);
        }

        var subject = Instantiate(spawnRoute.SubjectPrefab, subjectSpawnRoot);
        subject.transform.localPosition = spawnPosition;
        subject.transform.localScale *= spawnRoute.Scale;

        if (!TryPositionPathAnchorAtSpawnPosition(subject, spawnRoute, spawnPosition))
        {
            Destroy(subject);
            return;
        }

        AssignUniqueSortingOrder(subject);

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
            moveDirection,
            spawnRoute.MoveSpeed,
            spawnRoute.VerticalSwayAmplitude,
            spawnRoute.VerticalSwayFrequencyHz
        );
        ApplyHorizontalMirror(subject, isHorizontallyMirrored);
        spawnedSubjects.Add(subject);
    }

    private void AssignUniqueSortingOrder(GameObject subject)
    {
        if (
            !subject.TryGetComponent<StageSubject>(out var stageSubject)
            || stageSubject.SubjectRenderer == null
        )
        {
            return;
        }

        var sortingOrder = stageSubject.SubjectRenderer.sortingOrder;
        while (!assignedSubjectSortingOrders.Add(sortingOrder))
        {
            sortingOrder++;
        }

        stageSubject.SubjectRenderer.sortingOrder = sortingOrder;
    }

    private bool TryPositionPathAnchorAtSpawnPosition(
        GameObject subject,
        SubjectSpawnRoute spawnRoute,
        Vector2 spawnPosition
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

        var desiredAnchorPosition = subjectSpawnRoot.TransformPoint(spawnPosition);
        subject.transform.position += desiredAnchorPosition - stageSubject.PathAnchor.position;
        return true;
    }

    private bool ShouldSpawnFromOppositeSide()
    {
        return spawnRandom.NextDouble() < Mathf.Clamp01(oppositeSideProbability);
    }

    private static SubjectMoveDirection ReverseMoveDirection(SubjectMoveDirection moveDirection)
    {
        return moveDirection == SubjectMoveDirection.LeftToRight
            ? SubjectMoveDirection.RightToLeft
            : SubjectMoveDirection.LeftToRight;
    }

    /// <summary>
    /// 見た目をSpriteRenderer.flipXで反転するとき、判断ポイントとColliderも
    /// 左右反転させ、見た目と撮影判定の左右差をなくす。
    /// </summary>
    private static void ApplyHorizontalMirror(GameObject subject, bool isHorizontallyMirrored)
    {
        if (!isHorizontallyMirrored || !subject.TryGetComponent<StageSubject>(out var stageSubject))
        {
            return;
        }

        if (stageSubject.SubjectRenderer != null)
        {
            stageSubject.SubjectRenderer.flipX = !stageSubject.SubjectRenderer.flipX;
        }

        MirrorJudgementPointHorizontally(stageSubject);
        MirrorColliderHorizontally(subject);
    }

    private static void MirrorJudgementPointHorizontally(StageSubject stageSubject)
    {
        if (stageSubject.JudgementPoint == null)
        {
            return;
        }

        var localPosition = stageSubject.JudgementPoint.localPosition;
        localPosition.x = -localPosition.x;
        stageSubject.JudgementPoint.localPosition = localPosition;
    }

    private static void MirrorColliderHorizontally(GameObject subject)
    {
        if (!subject.TryGetComponent<PolygonCollider2D>(out var polygonCollider))
        {
            Debug.LogWarning(
                $"[SubjectTimelineController] {subject.name} has no PolygonCollider2D to mirror. "
                    + "Its collider will not match the flipped sprite.",
                subject
            );
            return;
        }

        var offset = polygonCollider.offset;
        offset.x = -offset.x;
        polygonCollider.offset = offset;

        for (var pathIndex = 0; pathIndex < polygonCollider.pathCount; pathIndex++)
        {
            var path = polygonCollider.GetPath(pathIndex);
            for (var pointIndex = 0; pointIndex < path.Length; pointIndex++)
            {
                var point = path[pointIndex];
                point.x = -point.x;
                path[pointIndex] = point;
            }

            polygonCollider.SetPath(pathIndex, path);
        }
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

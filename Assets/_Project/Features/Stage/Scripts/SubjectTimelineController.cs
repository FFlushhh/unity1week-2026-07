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
    [Serializable]
    private sealed class SubjectSpawnSetting
    {
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

        public GameObject SubjectPrefab => subjectPrefab;

        public float SpawnTimeSeconds => spawnTimeSeconds;

        public Vector2 SpawnPosition => spawnPosition;

        public SubjectMoveDirection MoveDirection => moveDirection;

        public float MoveSpeed => moveSpeed;

        public float Scale => scale;
    }

    [SerializeField]
    private Stage0Controller stageController;

    [SerializeField]
    private Transform subjectSpawnRoot;

    [SerializeField]
    private SubjectSpawnSetting[] spawnSettings;

    private readonly List<GameObject> spawnedSubjects = new();
    private bool[] hasSpawned;
    private bool hasStoppedForTerminalState;
    private float elapsedTimeSeconds;

    private void Start()
    {
        ResetTimeline();

        if (stageController == null)
        {
            Debug.LogError("[SubjectTimelineController] Stage controller is not assigned.", this);
            return;
        }

        stageController.StateChanged += HandleStageStateChanged;
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
        hasSpawned = new bool[spawnSettings?.Length ?? 0];
        hasStoppedForTerminalState = false;
        DestroySpawnedSubjects();
    }

    private void SpawnDueSubjects()
    {
        if (spawnSettings == null)
        {
            return;
        }

        for (var index = 0; index < spawnSettings.Length; index++)
        {
            if (hasSpawned[index] || elapsedTimeSeconds < spawnSettings[index].SpawnTimeSeconds)
            {
                continue;
            }

            hasSpawned[index] = true;
            SpawnSubject(spawnSettings[index]);
        }
    }

    private void SpawnSubject(SubjectSpawnSetting spawnSetting)
    {
        if (spawnSetting.SubjectPrefab == null)
        {
            Debug.LogError("[SubjectTimelineController] Subject prefab is not assigned.", this);
            return;
        }

        if (subjectSpawnRoot == null)
        {
            Debug.LogError("[SubjectTimelineController] Subject spawn root is not assigned.", this);
            return;
        }

        var subject = Instantiate(spawnSetting.SubjectPrefab, subjectSpawnRoot);
        subject.transform.localPosition = spawnSetting.SpawnPosition;
        subject.transform.localScale *= spawnSetting.Scale;

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

        subjectMover.Configure(spawnSetting.MoveDirection, spawnSetting.MoveSpeed);
        spawnedSubjects.Add(subject);
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
}

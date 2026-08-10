using System;
using UnityEngine;

/// <summary>
/// 撮影時間中のランダムなピンボケを開始し、PhotoPreviewへ反映します。
/// </summary>
public sealed class StageRandomDefocusController : MonoBehaviour
{
    [SerializeField]
    private Stage1Controller stageController;

    [SerializeField]
    private StagePhotoFocusPresentation photoFocusPresentation;

    [SerializeField, Tooltip("ゲーム開始時のピンボケ解消SEを再生する既存コンポーネント")]
    private MonoBehaviour focusReleaseSoundTrigger;

    [Header("Occurrence")]
    [SerializeField, Range(0f, 1f)]
    private float gameOccurrenceProbability = 0.2f;

    [SerializeField, Min(0.01f)]
    private float drawIntervalSeconds = 0.5f;

    private RandomDefocusTimeline timeline;
    private float playingStartedAt;
    private bool wasRandomDefocusVisible;

    private void OnEnable()
    {
        if (stageController == null)
        {
            Debug.LogError(
                "[StageRandomDefocusController] Stage controller is not assigned.",
                this
            );
            return;
        }

        if (photoFocusPresentation == null)
        {
            Debug.LogError(
                "[StageRandomDefocusController] Photo focus presentation is not assigned.",
                this
            );
            return;
        }

        stageController.StateChanged += HandleStageStateChanged;
        if (stageController.CurrentState == Stage1Controller.Stage1State.Playing)
        {
            StartRandomDefocus();
        }
    }

    private void OnDisable()
    {
        if (stageController != null)
        {
            stageController.StateChanged -= HandleStageStateChanged;
        }

        timeline = null;
        wasRandomDefocusVisible = false;
        if (photoFocusPresentation != null)
        {
            photoFocusPresentation.SetRandomDefocusStrength(0f);
        }
    }

    private void Update()
    {
        if (timeline == null)
        {
            return;
        }

        var state = EvaluateCurrentState();
        var isRandomDefocusVisible = state.BlurStrength > 0f;
        if (wasRandomDefocusVisible && !isRandomDefocusVisible && focusReleaseSoundTrigger != null)
        {
            focusReleaseSoundTrigger.SendMessage("PlaySE", SendMessageOptions.DontRequireReceiver);
        }

        wasRandomDefocusVisible = isRandomDefocusVisible;
        if (photoFocusPresentation != null)
        {
            photoFocusPresentation.SetRandomDefocusStrength(state.BlurStrength);
        }
    }

    internal RandomDefocusState EvaluateCurrentState()
    {
        if (timeline == null)
        {
            return default;
        }

        return timeline.Evaluate(Mathf.Max(0f, Time.time - playingStartedAt));
    }

    private void HandleStageStateChanged(Stage1Controller.Stage1State state)
    {
        if (state != Stage1Controller.Stage1State.Playing)
        {
            return;
        }

        StartRandomDefocus();
    }

    private void StartRandomDefocus()
    {
        var random = new System.Random(
            unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode())
        );
        timeline = RandomDefocusTimeline.Create(
            stageController.PlayingDuration,
            drawIntervalSeconds,
            gameOccurrenceProbability,
            random
        );
        playingStartedAt = Time.time;
        wasRandomDefocusVisible = false;
        photoFocusPresentation.SetRandomDefocusStrength(0f);
    }
}

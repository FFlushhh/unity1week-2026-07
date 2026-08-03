using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ResultScene;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game_Stage0の成功・失敗時に、撮影結果の引き渡しとシーン遷移を担当します。
/// </summary>
public sealed class Stage0SceneTransitionController : MonoBehaviour
{
    [SerializeField]
    private Stage0Controller stageController;

    [SerializeField]
    private StagePhotoCaptureController stagePhotoCaptureController;

    [SerializeField]
    private string playerName = "プレイヤー";

    [SerializeField]
    private string locationName = "Stage 0";

    [SerializeField]
    private string resultSceneName = "ResultScene";

    [SerializeField]
    private string titleSceneName = "Title";

    private bool hasStartedResultTransition;
    private bool hasStartedTitleTransition;
    private CancellationTokenSource lifetimeCancellation;

    [Header("Transition Presentation")]
    [SerializeField]
    private Animator transitionAnimator;

    [SerializeField]
    private string transitionTriggerName = "Start";

    // ★ 0.6秒の待機時間を変数化（デフォルトは 0.6 秒）
    [SerializeField]
    private float transitionWaitDuration = 0.6f;

    /// <summary>
    /// テスト時などに待機時間を変更・スキップするためのプロパティ
    /// </summary>
    public float TransitionWaitDuration
    {
        get => transitionWaitDuration;
        set => transitionWaitDuration = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            destroyCancellationToken
        );
    }

    private void OnDestroy()
    {
        lifetimeCancellation?.Cancel();
        lifetimeCancellation?.Dispose();
        lifetimeCancellation = null;
    }

    private void OnEnable()
    {
        if (stageController == null)
        {
            Debug.LogError(
                "[Stage0SceneTransitionController] Stage controller is not assigned.",
                this
            );
            return;
        }

        stageController.StateChanged += HandleStageStateChanged;
    }

    private void OnDisable()
    {
        if (stageController != null)
        {
            stageController.StateChanged -= HandleStageStateChanged;
        }
    }

    /// <summary>
    /// Game Over UIのボタンからTitleへ戻ります。
    /// </summary>
    public void ReturnToTitle()
    {
        if (hasStartedTitleTransition)
        {
            return;
        }

        hasStartedTitleTransition = true;
        ReturnToTitleAsync(GetLifetimeCancellationToken()).Forget();
    }

    private void HandleStageStateChanged(Stage0Controller.Stage0State state)
    {
        if (state != Stage0Controller.Stage0State.Completed || hasStartedResultTransition)
        {
            return;
        }

        hasStartedResultTransition = true;
        TransitionToResultAsync(GetLifetimeCancellationToken()).Forget();
    }

    private CancellationToken GetLifetimeCancellationToken()
    {
        if (lifetimeCancellation == null)
        {
            lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                destroyCancellationToken
            );
        }

        return lifetimeCancellation.Token;
    }

    private async UniTask TransitionToResultAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ★ アニメーションを再生
            if (transitionAnimator != null && !string.IsNullOrEmpty(transitionTriggerName))
            {
                transitionAnimator.SetTrigger(transitionTriggerName);
            }

            // ★ 固定値(0.6f) から transitionWaitDuration に変更
            if (transitionWaitDuration > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(transitionWaitDuration),
                    cancellationToken: cancellationToken
                );
            }

            if (stagePhotoCaptureController != null)
            {
                await stagePhotoCaptureController.WaitForCapturePresentationAsync(
                    cancellationToken
                );
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!TryTransferCapturedPhoto(out var transferredData, out var previousData))
        {
            return;
        }

        try
        {
            var loadOperation = SceneManager.LoadSceneAsync(resultSceneName);
            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"Could not start loading scene '{resultSceneName}'."
                );
            }

            await loadOperation.ToUniTask();
        }
        catch (Exception exception)
        {
            if (ResultDataTransporter.CurrentData == transferredData)
            {
                ResultDataTransporter.CurrentData = previousData;
            }

            if (transferredData.CapturedImage != null)
            {
                Destroy(transferredData.CapturedImage);
            }

            Debug.LogException(exception, this);
        }
    }

    private async UniTask ReturnToTitleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.NextFrame(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError(
                $"[Stage0SceneTransitionController] Title scene '{titleSceneName}' cannot be loaded.",
                this
            );
            return;
        }

        try
        {
            var loadOperation = SceneManager.LoadSceneAsync(titleSceneName);
            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"Could not start loading scene '{titleSceneName}'."
                );
            }

            await loadOperation.ToUniTask();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private bool TryTransferCapturedPhoto(
        out ResultData transferredData,
        out ResultData previousData
    )
    {
        transferredData = null;
        previousData = ResultDataTransporter.CurrentData;

        if (!Application.CanStreamedLevelBeLoaded(resultSceneName))
        {
            Debug.LogError(
                $"[Stage0SceneTransitionController] Result scene '{resultSceneName}' cannot be loaded.",
                this
            );
            return false;
        }

        if (stagePhotoCaptureController == null)
        {
            Debug.LogError(
                "[Stage0SceneTransitionController] Photo capture controller is not assigned.",
                this
            );
            return false;
        }

        var capturedPhoto = stagePhotoCaptureController.TakeCapturedPhoto();
        if (capturedPhoto == null)
        {
            Debug.LogError(
                "[Stage0SceneTransitionController] No captured photo is available for the Result scene.",
                this
            );
            return false;
        }

        try
        {
            // ★ PlayerPrefs から保存された名前を取得（設定されていなければ Inspector の playerName を使用）
            string currentName = PlayerPrefs.GetString("PLAYER_NAME", playerName);

            transferredData = StageResultDataFactory.Create(
                capturedPhoto,
                currentName, // ★ ここを固定値ではなく取得した名前で渡す
                locationName
            );
            ResultDataTransporter.CurrentData = transferredData;
            return true;
        }
        catch (Exception exception)
        {
            if (capturedPhoto.Image != null)
            {
                Destroy(capturedPhoto.Image);
            }

            Debug.LogException(exception, this);
            return false;
        }
    }
}

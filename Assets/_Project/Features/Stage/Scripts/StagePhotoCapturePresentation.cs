using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class StagePhotoCapturePresentation : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private CanvasGroup shutterBlackout;

    [SerializeField]
    private RawImage capturedPhotoPreview;

    [SerializeField]
    private RectTransform capturedPhotoPreviewTransform;

    [Header("Durations")]
    [SerializeField]
    [Min(0f)]
    private float blackoutFadeInDuration = 0.08f;

    [SerializeField]
    [Min(0f)]
    private float blackoutHoldDuration = 0.08f;

    [SerializeField]
    [Min(0f)]
    private float blackoutFadeOutDuration = 0.08f;

    [SerializeField]
    [Min(0f)]
    private float capturedPhotoScaleDuration = 0.16f;

    private CancellationTokenSource activePresentationCancellation;
    private bool isPlaying;
    private bool hasInitialPreviewScale;
    private Vector3 initialPreviewScale;

    public bool IsPlaying => isPlaying;

    public UniTask PlayAsync(CancellationToken cancellationToken)
    {
        if (isPlaying)
        {
            return UniTask.CompletedTask;
        }

        return PlayInternalAsync(cancellationToken);
    }

    public async UniTask WaitForCompletionAsync(CancellationToken cancellationToken)
    {
        while (isPlaying)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    public void ResetPresentation()
    {
        activePresentationCancellation?.Cancel();
        activePresentationCancellation?.Dispose();
        activePresentationCancellation = null;
        isPlaying = false;

        EnsureInitialPreviewScale();
        SetBlackout(false, 0f);
        SetPreviewVisible(false);
        SetPreviewScale(initialPreviewScale);
    }

    private async UniTask PlayInternalAsync(CancellationToken cancellationToken)
    {
        EnsureInitialPreviewScale();
        isPlaying = true;
        activePresentationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        var presentationCancellation = activePresentationCancellation;
        var completed = false;

        try
        {
            SetPreviewVisible(false);
            SetPreviewScale(initialPreviewScale);
            SetBlackout(true, 0f);

            await FadeBlackoutAsync(0f, 1f, blackoutFadeInDuration, presentationCancellation.Token);
            await DelayAsync(blackoutHoldDuration, presentationCancellation.Token);
            await FadeBlackoutAsync(
                1f,
                0f,
                blackoutFadeOutDuration,
                presentationCancellation.Token
            );
            SetBlackout(false, 0f);

            await RevealCapturedPhotoAsync(presentationCancellation.Token);
            completed = true;
        }
        catch (OperationCanceledException)
        {
            // Scene破棄や再プレイ時に黒画面を残さないため、キャンセルは通常の終了として扱う。
        }
        finally
        {
            SetBlackout(false, 0f);

            if (!completed)
            {
                SetPreviewVisible(false);
                SetPreviewScale(initialPreviewScale);
            }

            if (activePresentationCancellation == presentationCancellation)
            {
                activePresentationCancellation = null;
                isPlaying = false;
            }

            presentationCancellation.Dispose();
        }
    }

    private async UniTask FadeBlackoutAsync(
        float from,
        float to,
        float duration,
        CancellationToken cancellationToken
    )
    {
        if (duration <= 0f)
        {
            SetBlackout(true, to);
            return;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetBlackout(true, Mathf.Lerp(from, to, elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            elapsed += Time.unscaledDeltaTime;
        }

        SetBlackout(true, to);
    }

    private async UniTask RevealCapturedPhotoAsync(CancellationToken cancellationToken)
    {
        if (capturedPhotoPreview == null)
        {
            return;
        }

        SetPreviewVisible(true);

        if (capturedPhotoPreviewTransform == null)
        {
            return;
        }

        if (capturedPhotoScaleDuration <= 0f)
        {
            SetPreviewScale(initialPreviewScale);
            return;
        }

        var elapsed = 0f;
        while (elapsed < capturedPhotoScaleDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = elapsed / capturedPhotoScaleDuration;
            var easeOutProgress = 1f - ((1f - progress) * (1f - progress));
            capturedPhotoPreviewTransform.localScale = Vector3.LerpUnclamped(
                Vector3.zero,
                initialPreviewScale,
                easeOutProgress
            );
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            elapsed += Time.unscaledDeltaTime;
        }

        SetPreviewScale(initialPreviewScale);
    }

    private static UniTask DelayAsync(float duration, CancellationToken cancellationToken)
    {
        return duration <= 0f
            ? UniTask.CompletedTask
            : UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                cancellationToken
            );
    }

    private void EnsureInitialPreviewScale()
    {
        if (capturedPhotoPreviewTransform == null && capturedPhotoPreview != null)
        {
            capturedPhotoPreviewTransform = capturedPhotoPreview.rectTransform;
        }

        if (hasInitialPreviewScale || capturedPhotoPreviewTransform == null)
        {
            return;
        }

        initialPreviewScale = capturedPhotoPreviewTransform.localScale;
        hasInitialPreviewScale = true;
    }

    private void SetBlackout(bool visible, float alpha)
    {
        if (shutterBlackout == null)
        {
            return;
        }

        shutterBlackout.alpha = alpha;
        shutterBlackout.gameObject.SetActive(visible);
    }

    private void SetPreviewVisible(bool visible)
    {
        if (capturedPhotoPreview != null)
        {
            capturedPhotoPreview.gameObject.SetActive(visible);
        }
    }

    private void SetPreviewScale(Vector3 scale)
    {
        if (capturedPhotoPreviewTransform != null && hasInitialPreviewScale)
        {
            capturedPhotoPreviewTransform.localScale = scale;
        }
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game_Stage1開始直後のピント合わせ演出を管理します。
/// PhotoPreviewのRawImageにぼかしシェーダのマテリアルを実行時だけ割り当て、
/// 強度を徐々に0へ下げてから短い待機を挟みます。
/// PlayAsyncの完了 = 撮影タイムを開始してよいタイミングです。
/// </summary>
public sealed class StagePhotoFocusPresentation : MonoBehaviour
{
    private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");
    private static readonly int MaxBlurRadiusPixelsId = Shader.PropertyToID("_MaxBlurRadiusPixels");
    private static readonly int SourceTexelSizeId = Shader.PropertyToID("_SourceTexelSize");
    private static readonly int BlurLodScaleId = Shader.PropertyToID("_BlurLodScale");

    [Header("Target")]
    [SerializeField]
    private RawImage photoPreview;

    [SerializeField]
    private Material blurMaterialSource;

    [Header("Blur")]
    [SerializeField, Range(0f, 1f)]
    private float initialBlurStrength = 1f;

    [SerializeField, Min(0f)]
    private float maxBlurRadiusPixels = 16f;

    [SerializeField, Min(0f)]
    private float blurLodScale = 0.5f;

    [Header("Durations")]
    [SerializeField, Min(0f)]
    private float blurClearDuration = 1.6f;

    [SerializeField, Min(0f)]
    private float postBlurWaitDuration = 0.4f;

    private Material runtimeBlurMaterial;
    private CancellationTokenSource activePresentationCancellation;
    private bool isPlaying;
    private float openingBlurStrength;
    private float randomDefocusBlurStrength;

    public bool IsPlaying => isPlaying;

    internal void SetRandomDefocusStrength(float strength)
    {
        randomDefocusBlurStrength = Mathf.Clamp01(strength);
        RefreshBlurMaterial();
    }

    internal bool TryBlitWithBlurStrength(
        RenderTexture source,
        RenderTexture destination,
        float blurStrength
    )
    {
        if (source == null || destination == null || blurStrength <= 0f)
        {
            return false;
        }

        if (!TryAttachBlurMaterial())
        {
            return false;
        }

        runtimeBlurMaterial.SetVector(SourceTexelSizeId, ResolveSourceTexelSize(source));
        runtimeBlurMaterial.SetFloat(BlurStrengthId, Mathf.Clamp01(blurStrength));
        try
        {
            Graphics.Blit(source, destination, runtimeBlurMaterial);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            RefreshBlurMaterial();
        }
    }

    public UniTask PlayAsync(CancellationToken cancellationToken)
    {
        return isPlaying ? UniTask.CompletedTask : PlayInternalAsync(cancellationToken);
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
        openingBlurStrength = 0f;
        RefreshBlurMaterial();
    }

    private void OnDisable()
    {
        // 再プレイ・シーン破棄でぼかしを残さないため、無効化時に必ずリセットする。
        ResetPresentation();
        randomDefocusBlurStrength = 0f;
        RefreshBlurMaterial();
    }

    private void OnDestroy()
    {
        ResetPresentation();
        if (runtimeBlurMaterial != null)
        {
            Destroy(runtimeBlurMaterial);
            runtimeBlurMaterial = null;
        }
    }

    private async UniTask PlayInternalAsync(CancellationToken cancellationToken)
    {
        isPlaying = true;

        // 参照未設定でも「解除時間 + 待機時間」の進行だけは必ず守る（配線ミスでゲーム進行を止めない）。
        var hasBlur = TryAttachBlurMaterial();

        activePresentationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        var presentationCancellation = activePresentationCancellation;

        try
        {
            if (hasBlur)
            {
                SetOpeningBlurStrength(initialBlurStrength);
            }

            await ClearBlurAsync(hasBlur, presentationCancellation.Token);
            await DelayAsync(postBlurWaitDuration, presentationCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Scene破棄や再プレイ時にぼけたまま残さないため、キャンセルは通常の終了として扱う。
        }
        finally
        {
            SetOpeningBlurStrength(0f);

            if (activePresentationCancellation == presentationCancellation)
            {
                activePresentationCancellation = null;
                isPlaying = false;
            }

            presentationCancellation.Dispose();
        }
    }

    private async UniTask ClearBlurAsync(bool hasBlur, CancellationToken cancellationToken)
    {
        if (blurClearDuration <= 0f)
        {
            if (hasBlur)
            {
                SetOpeningBlurStrength(0f, keepBlurMaterialAttached: true);
            }
            return;
        }

        var elapsed = 0f;
        while (elapsed < blurClearDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hasBlur)
            {
                SetOpeningBlurStrength(
                    Mathf.SmoothStep(initialBlurStrength, 0f, elapsed / blurClearDuration)
                );
            }
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            elapsed += Time.unscaledDeltaTime;
        }

        if (hasBlur)
        {
            SetOpeningBlurStrength(0f, keepBlurMaterialAttached: true);
        }
    }

    private bool TryAttachBlurMaterial()
    {
        if (photoPreview == null || blurMaterialSource == null)
        {
            Debug.LogError(
                "[StagePhotoFocusPresentation] Photo preview or blur material is not assigned.",
                this
            );
            return false;
        }

        if (runtimeBlurMaterial == null)
        {
            // 共有マテリアル資産を実行時に書き換えないよう、必ずインスタンスを作る。
            runtimeBlurMaterial = new Material(blurMaterialSource)
            {
                hideFlags = HideFlags.DontSave,
            };
        }

        runtimeBlurMaterial.SetFloat(MaxBlurRadiusPixelsId, maxBlurRadiusPixels);
        runtimeBlurMaterial.SetFloat(BlurLodScaleId, blurLodScale);
        runtimeBlurMaterial.SetVector(
            SourceTexelSizeId,
            ResolveSourceTexelSize(photoPreview.texture)
        );
        photoPreview.material = runtimeBlurMaterial;
        return true;
    }

    private static Vector4 ResolveSourceTexelSize(Texture texture)
    {
        // RawImageのテクスチャはCanvasRenderer経由でマテリアル外から差し込まれるため
        // _MainTex_TexelSizeは当てにできない。ここで明示的に解像度を渡す。
        var width = texture != null ? Mathf.Max(1, texture.width) : 1;
        var height = texture != null ? Mathf.Max(1, texture.height) : 1;
        return new Vector4(1f / width, 1f / height, width, height);
    }

    private void RefreshBlurMaterial(bool keepBlurMaterialAttached = false)
    {
        var effectiveBlurStrength = Mathf.Max(openingBlurStrength, randomDefocusBlurStrength);
        if ((effectiveBlurStrength > 0f || keepBlurMaterialAttached) && TryAttachBlurMaterial())
        {
            runtimeBlurMaterial.SetFloat(BlurStrengthId, effectiveBlurStrength);
            return;
        }

        DetachBlurMaterial();
    }

    private void DetachBlurMaterial()
    {
        if (runtimeBlurMaterial != null)
        {
            runtimeBlurMaterial.SetFloat(BlurStrengthId, 0f);
        }

        if (photoPreview != null && ReferenceEquals(photoPreview.material, runtimeBlurMaterial))
        {
            // 既定UIマテリアルへ戻す＝撮影タイム中の描画コスト増をゼロにする。
            photoPreview.material = null;
        }
    }

    private void SetOpeningBlurStrength(float strength, bool keepBlurMaterialAttached = false)
    {
        openingBlurStrength = Mathf.Clamp01(strength);
        RefreshBlurMaterial(keepBlurMaterialAttached);
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
}

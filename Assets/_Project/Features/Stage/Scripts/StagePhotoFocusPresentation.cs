using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game_Stage0開始直後のピント合わせ演出を管理します。
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
    private float maxBlurRadiusPixels = 5f;

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

    public bool IsPlaying => isPlaying;

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
        DetachBlurMaterial();
    }

    private void OnDisable()
    {
        // 再プレイ・シーン破棄でぼかしを残さないため、無効化時に必ずリセットする。
        ResetPresentation();
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
                SetBlurStrength(initialBlurStrength);
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
            DetachBlurMaterial();

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
                SetBlurStrength(0f);
            }
            return;
        }

        var elapsed = 0f;
        while (elapsed < blurClearDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hasBlur)
            {
                SetBlurStrength(
                    Mathf.SmoothStep(initialBlurStrength, 0f, elapsed / blurClearDuration)
                );
            }
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            elapsed += Time.unscaledDeltaTime;
        }

        if (hasBlur)
        {
            SetBlurStrength(0f);
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
        runtimeBlurMaterial.SetVector(SourceTexelSizeId, ResolveSourceTexelSize());
        photoPreview.material = runtimeBlurMaterial;
        return true;
    }

    private Vector4 ResolveSourceTexelSize()
    {
        // RawImageのテクスチャはCanvasRenderer経由でマテリアル外から差し込まれるため
        // _MainTex_TexelSizeは当てにできない。ここで明示的に解像度を渡す。
        var texture = photoPreview.texture;
        var width = texture != null ? Mathf.Max(1, texture.width) : 1;
        var height = texture != null ? Mathf.Max(1, texture.height) : 1;
        return new Vector4(1f / width, 1f / height, width, height);
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

    private void SetBlurStrength(float strength)
    {
        // CanvasRendererは同じMaterialインスタンス参照を保持しているため、
        // SetFloatのみでCanvasリビルド無しに反映される。
        if (runtimeBlurMaterial != null)
        {
            runtimeBlurMaterial.SetFloat(BlurStrengthId, Mathf.Clamp01(strength));
        }
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

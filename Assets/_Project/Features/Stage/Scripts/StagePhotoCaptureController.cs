using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// シャッター入力を受け取り、1プレイにつき1回だけ撮影対象を確定します。
/// </summary>
public sealed class StagePhotoCaptureController : MonoBehaviour
{
    [SerializeField]
    private Stage1Controller stageController;

    [SerializeField]
    private PhotoFrameSubjectJudge photoFrameSubjectJudge;

    [SerializeField]
    private Button shutterButton;

    [SerializeField]
    private Camera photoCamera;

    [SerializeField]
    private RawImage capturedPhotoPreview;

    [SerializeField]
    private StagePhotoCapturePresentation capturePresentation;

    [SerializeField]
    private StageRandomDefocusController randomDefocusController;

    [SerializeField]
    private StagePhotoFocusPresentation photoFocusPresentation;

    [Tooltip(
        "リザルト画面での基礎スコア名などに使われる、撮影した画像の名前。未設定ならステージ名で処理"
    )]
    [SerializeField]
    private string capturedImageName = "";

    private readonly List<StageSubject> capturedSubjects = new();
    private CapturedPhoto capturedPhoto;
    private InputAction shutterAction;
    private bool hasCaptured;

    public bool HasCaptured => hasCaptured;

    public IReadOnlyList<StageSubject> CapturedSubjects => capturedSubjects;

    public CapturedPhoto CapturedPhoto => capturedPhoto;

    public UniTask WaitForCapturePresentationAsync(CancellationToken cancellationToken)
    {
        return capturePresentation == null
            ? UniTask.CompletedTask
            : capturePresentation.WaitForCompletionAsync(cancellationToken);
    }

    /// <summary>
    /// 撮影結果の所有権を呼び出し元へ移します。
    /// 移譲後のTexture2DはStage側で破棄しないため、受け取った側が破棄します。
    /// </summary>
    public CapturedPhoto TakeCapturedPhoto()
    {
        if (capturedPhoto == null)
        {
            return null;
        }

        var photoToTransfer = capturedPhoto;
        capturedPhoto = null;

        if (capturePresentation != null)
        {
            capturePresentation.ResetPresentation();
        }

        if (capturedPhotoPreview != null)
        {
            capturedPhotoPreview.texture = null;
            capturedPhotoPreview.gameObject.SetActive(false);
        }

        return photoToTransfer;
    }

    private void OnEnable()
    {
        if (stageController != null)
        {
            stageController.StateChanged += HandleStageStateChanged;
            if (stageController.CurrentState == Stage1Controller.Stage1State.Playing)
            {
                ResetCapture();
            }
        }

        if (shutterButton != null)
        {
            shutterButton.onClick.AddListener(HandleShutterButtonClicked);
        }

        EnableShutterInput();
    }

    private void OnDisable()
    {
        if (stageController != null)
        {
            stageController.StateChanged -= HandleStageStateChanged;
        }

        if (shutterButton != null)
        {
            shutterButton.onClick.RemoveListener(HandleShutterButtonClicked);
        }

        if (shutterAction != null)
        {
            shutterAction.performed -= HandleShutterPerformed;
            shutterAction.Disable();
            shutterAction.Dispose();
            shutterAction = null;
        }
    }

    private void OnDestroy()
    {
        ReleaseCapturedPhoto();
    }

    /// <summary>
    /// 現在の写真枠内にいる被写体を確定し、撮影済み状態へ移行します。
    /// </summary>
    public bool TryCapture()
    {
        if (hasCaptured || stageController == null || photoFrameSubjectJudge == null)
        {
            return false;
        }

        if (stageController.CurrentState != Stage1Controller.Stage1State.Playing)
        {
            return false;
        }

        // 被写体の移動直後でも、撮影画像と遮蔽判定で同じTransform位置を使う。
        Physics2D.SyncTransforms();

        var randomDefocusState =
            randomDefocusController != null
                ? randomDefocusController.EvaluateCurrentState()
                : default;
        if (photoFocusPresentation != null)
        {
            photoFocusPresentation.SetRandomDefocusStrength(randomDefocusState.BlurStrength);
        }

        if (!TryCopyPhotoCameraOutput(randomDefocusState.BlurStrength, out var capturedImage))
        {
            return false;
        }

        // 同一フレームのボタン・キー入力が重なっても、2回目以降を無視するため先に確定する。
        CaptureSubjectsInsidePhotoFrame();
        capturedPhoto = new CapturedPhoto(
            capturedImage,
            capturedSubjects,
            randomDefocusState.IsScoreForcedToZero
        );
        ShowCapturedPhotoPreview(capturedImage, showImmediately: capturePresentation == null);

        // 演出中もカウントダウンを継続するため、演出開始より先に撮影済み状態へ遷移する。
        hasCaptured = true;
        stageController.BeginCapturedWaitingForTimeout();
        if (capturePresentation != null)
        {
            capturePresentation.PlayAsync(destroyCancellationToken).Forget();
        }
        return true;
    }

    private void EnableShutterInput()
    {
        shutterAction = CreateShutterAction();
        shutterAction.performed += HandleShutterPerformed;
        shutterAction.Enable();
    }

    private static InputAction CreateShutterAction()
    {
        var action = new InputAction("Shutter", InputActionType.Button);
        action.AddBinding("<Keyboard>/space");
        action.AddBinding("<Keyboard>/enter");
        return action;
    }

    private void HandleShutterPerformed(InputAction.CallbackContext context)
    {
        TryCapture();
    }

    private void HandleShutterButtonClicked()
    {
        TryCapture();
    }

    private void HandleStageStateChanged(Stage1Controller.Stage1State state)
    {
        if (state == Stage1Controller.Stage1State.Playing)
        {
            ResetCapture();
        }
    }

    private void CaptureSubjectsInsidePhotoFrame()
    {
        capturedSubjects.Clear();

        var activeSubjects = FindObjectsByType<StageSubject>(FindObjectsInactive.Exclude);
        foreach (var subject in activeSubjects)
        {
            if (photoFrameSubjectJudge.IsCapturable(subject))
            {
                capturedSubjects.Add(subject);
            }
        }
    }

    private void ResetCapture()
    {
        hasCaptured = false;
        if (capturePresentation != null)
        {
            capturePresentation.ResetPresentation();
        }
        capturedSubjects.Clear();
        ReleaseCapturedPhoto();

        if (capturedPhotoPreview != null)
        {
            capturedPhotoPreview.texture = null;
            capturedPhotoPreview.gameObject.SetActive(false);
        }
    }

    private bool TryCopyPhotoCameraOutput(float blurStrength, out Texture2D capturedImage)
    {
        capturedImage = null;

        if (photoCamera == null || photoCamera.targetTexture == null)
        {
            Debug.LogError(
                "[StagePhotoCaptureController] Photo camera or its RenderTexture is not assigned.",
                this
            );
            return false;
        }

        var source = photoCamera.targetTexture;
        photoCamera.Render();

        var previousActive = RenderTexture.active;
        RenderTexture blurredSource = null;
        try
        {
            var readSource = source;
            if (blurStrength > 0f && photoFocusPresentation != null)
            {
                var descriptor = source.descriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                blurredSource = RenderTexture.GetTemporary(descriptor);
                if (
                    photoFocusPresentation.TryBlitWithBlurStrength(
                        source,
                        blurredSource,
                        blurStrength
                    )
                )
                {
                    readSource = blurredSource;
                }
                else
                {
                    RenderTexture.ReleaseTemporary(blurredSource);
                    blurredSource = null;
                }
            }

            RenderTexture.active = readSource;
            capturedImage = new Texture2D(
                readSource.width,
                readSource.height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: false
            );
            capturedImage.ReadPixels(new Rect(0f, 0f, readSource.width, readSource.height), 0, 0);
            capturedImage.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            if (!string.IsNullOrEmpty(capturedImageName))
            {
                capturedImage.name = capturedImageName;
            }

            return true;
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (blurredSource != null)
            {
                RenderTexture.ReleaseTemporary(blurredSource);
            }
        }
    }

    private void ShowCapturedPhotoPreview(Texture2D capturedImage, bool showImmediately)
    {
        if (capturedPhotoPreview == null)
        {
            return;
        }

        capturedPhotoPreview.texture = capturedImage;
        capturedPhotoPreview.gameObject.SetActive(showImmediately);
    }

    private void ReleaseCapturedPhoto()
    {
        if (capturedPhoto?.Image != null)
        {
            Destroy(capturedPhoto.Image);
        }

        capturedPhoto = null;
    }
}

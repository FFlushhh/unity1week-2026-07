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
    private Stage0Controller stageController;

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
            if (stageController.CurrentState == Stage0Controller.Stage0State.Playing)
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

        if (stageController.CurrentState != Stage0Controller.Stage0State.Playing)
        {
            return false;
        }

        // 被写体の移動直後でも、撮影画像と遮蔽判定で同じTransform位置を使う。
        Physics2D.SyncTransforms();

        if (!TryCopyPhotoCameraOutput(out var capturedImage))
        {
            return false;
        }

        // 同一フレームのボタン・キー入力が重なっても、2回目以降を無視するため先に確定する。
        CaptureSubjectsInsidePhotoFrame();
        capturedPhoto = new CapturedPhoto(capturedImage, capturedSubjects);
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

    private void HandleStageStateChanged(Stage0Controller.Stage0State state)
    {
        if (state == Stage0Controller.Stage0State.Playing)
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

    private bool TryCopyPhotoCameraOutput(out Texture2D capturedImage)
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
        try
        {
            RenderTexture.active = source;
            capturedImage = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: false
            );
            capturedImage.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            capturedImage.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return true;
        }
        finally
        {
            RenderTexture.active = previousActive;
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

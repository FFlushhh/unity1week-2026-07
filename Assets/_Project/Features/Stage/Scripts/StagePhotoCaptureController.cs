using System.Collections.Generic;
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

    private readonly List<StageSubject> capturedSubjects = new();
    private InputAction shutterAction;
    private bool hasCaptured;

    public bool HasCaptured => hasCaptured;

    public IReadOnlyList<StageSubject> CapturedSubjects => capturedSubjects;

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

        // 同一フレームのボタン・キー入力が重なっても、2回目以降を無視するため先に確定する。
        hasCaptured = true;
        CaptureSubjectsInsidePhotoFrame();
        stageController.BeginCapturedWaitingForTimeout();
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
            if (photoFrameSubjectJudge.IsInsidePhotoFrame(subject))
            {
                capturedSubjects.Add(subject);
            }
        }
    }

    private void ResetCapture()
    {
        hasCaptured = false;
        capturedSubjects.Clear();
    }
}

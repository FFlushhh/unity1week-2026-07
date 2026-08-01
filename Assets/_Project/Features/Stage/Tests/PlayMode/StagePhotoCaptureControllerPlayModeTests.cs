using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StagePhotoCaptureControllerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private readonly List<RenderTexture> createdRenderTextures = new();

    [UnityTearDown]
    public IEnumerator TearDownScene()
    {
        foreach (var createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();

        foreach (var renderTexture in createdRenderTextures)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        createdRenderTextures.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator ShutterInputCallbackAndButtonCaptureOnlyOnce()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);
        var shutterButton = GetPrivateField<Button>(captureController, "shutterButton");

        yield return null;

        InvokePrivateMethod(
            captureController,
            "HandleShutterPerformed",
            default(InputAction.CallbackContext)
        );

        Assert.That(captureController.HasCaptured, Is.True);
        Assert.That(
            GetPrivateField<Stage0Controller>(captureController, "stageController").CurrentState,
            Is.EqualTo(Stage0Controller.Stage0State.CapturedWaitingForTimeout)
        );
        Assert.That(captureController.CapturedSubjects, Has.Count.EqualTo(1));
        Assert.That(captureController.CapturedPhoto, Is.Not.Null);
        Assert.That(captureController.CapturedPhoto.Image, Is.Not.Null);
        Assert.That(captureController.CapturedPhoto.GetSubjectCount(SubjectId.Dog), Is.EqualTo(1));
        var capturedPhotoPreview = GetPrivateField<RawImage>(
            captureController,
            "capturedPhotoPreview"
        );
        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.True);
        Assert.That(
            capturedPhotoPreview.texture,
            Is.EqualTo(captureController.CapturedPhoto.Image)
        );

        shutterButton.onClick.Invoke();
        Assert.That(captureController.CapturedSubjects, Has.Count.EqualTo(1));
        Assert.That(captureController.TryCapture(), Is.False);
    }

    [UnityTest]
    public IEnumerator ShutterInputCallbackIgnoresAdditionalInput()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);

        yield return null;

        InvokePrivateMethod(
            captureController,
            "HandleShutterPerformed",
            default(InputAction.CallbackContext)
        );
        var capturedSubjects = captureController.CapturedSubjects;
        InvokePrivateMethod(
            captureController,
            "HandleShutterPerformed",
            default(InputAction.CallbackContext)
        );

        Assert.That(captureController.HasCaptured, Is.True);
        Assert.That(captureController.CapturedSubjects, Is.SameAs(capturedSubjects));
        Assert.That(captureController.CapturedSubjects, Has.Count.EqualTo(1));
    }

    [Test]
    public void CaptureIsIgnoredOutsidePlayingState()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.StartMessage);

        Assert.That(captureController.TryCapture(), Is.False);
        Assert.That(captureController.HasCaptured, Is.False);
        Assert.That(captureController.CapturedSubjects, Is.Empty);
        Assert.That(captureController.CapturedPhoto, Is.Null);
        var capturedPhotoPreview = GetPrivateField<RawImage>(
            captureController,
            "capturedPhotoPreview"
        );
        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.False);
        Assert.That(capturedPhotoPreview.texture, Is.Null);
    }

    [Test]
    public void EnteringPlayingStateResetsTheCapture()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);

        Assert.That(captureController.TryCapture(), Is.True);
        InvokePrivateMethod(
            captureController,
            "HandleStageStateChanged",
            Stage0Controller.Stage0State.Playing
        );

        Assert.That(captureController.HasCaptured, Is.False);
        Assert.That(captureController.CapturedSubjects, Is.Empty);
    }

    [Test]
    public void ShutterActionBindsSpaceAndEnter()
    {
        var shutterAction = InvokePrivateStaticMethod<InputAction>("CreateShutterAction");

        Assert.That(shutterAction, Is.Not.Null);
        var hasSpaceBinding = false;
        var hasEnterBinding = false;
        foreach (var binding in shutterAction.bindings)
        {
            hasSpaceBinding |= binding.path == "<Keyboard>/space";
            hasEnterBinding |= binding.path == "<Keyboard>/enter";
        }

        Assert.That(hasSpaceBinding, Is.True);
        Assert.That(hasEnterBinding, Is.True);
        shutterAction.Dispose();
    }

    private StagePhotoCaptureController CreateCaptureController(Stage0Controller.Stage0State state)
    {
        var stageControllerObject = CreateGameObject("Stage0Controller", active: false);
        var stageController = stageControllerObject.AddComponent<Stage0Controller>();
        SetPrivateField(stageController, "currentState", state);
        SetPrivateField(stageController, "remainingTime", 10f);

        var cameraObject = CreateGameObject("PhotoCamera");
        var photoCamera = cameraObject.AddComponent<Camera>();
        photoCamera.orthographic = true;
        photoCamera.orthographicSize = 5f;
        photoCamera.transform.position = new Vector3(0f, 0f, -10f);
        var photoRenderTexture = new RenderTexture(16, 9, 0, RenderTextureFormat.ARGB32);
        photoRenderTexture.Create();
        photoCamera.targetTexture = photoRenderTexture;
        createdRenderTextures.Add(photoRenderTexture);

        var canvasObject = CreateUiGameObject("PhotoPreviewCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var frameObject = CreateUiGameObject("PhotoFrame");
        frameObject.transform.SetParent(canvasObject.transform, false);

        var judgeObject = CreateGameObject("PhotoFrameSubjectJudge");
        var judge = judgeObject.AddComponent<PhotoFrameSubjectJudge>();
        SetPrivateField(judge, "photoCamera", photoCamera);
        SetPrivateField(judge, "photoFrame", frameObject.GetComponent<RectTransform>());

        var subjectObject = CreateGameObject("Subject");
        var judgementPointObject = CreateGameObject("JudgementPoint");
        judgementPointObject.transform.SetParent(subjectObject.transform);
        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, 10f)
        );
        var subject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(subject, "judgementPoint", judgementPointObject.transform);

        var buttonObject = CreateUiGameObject("ShutterButton");
        var shutterButton = buttonObject.AddComponent<Button>();
        var capturedPhotoPreviewObject = CreateUiGameObject("CapturedPhotoPreview");
        var capturedPhotoPreview = capturedPhotoPreviewObject.AddComponent<RawImage>();
        capturedPhotoPreviewObject.SetActive(false);
        var captureObject = CreateGameObject("StagePhotoCaptureController", active: false);
        var captureController = captureObject.AddComponent<StagePhotoCaptureController>();
        SetPrivateField(captureController, "stageController", stageController);
        SetPrivateField(captureController, "photoFrameSubjectJudge", judge);
        SetPrivateField(captureController, "shutterButton", shutterButton);
        SetPrivateField(captureController, "photoCamera", photoCamera);
        SetPrivateField(captureController, "capturedPhotoPreview", capturedPhotoPreview);
        captureObject.SetActive(true);
        return captureController;
    }

    private GameObject CreateGameObject(string name, bool active = true)
    {
        var gameObject = new GameObject(name);
        gameObject.SetActive(active);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private GameObject CreateUiGameObject(string name)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName, object argument)
    {
        var method = target.GetType().GetMethod(methodName, PrivateInstance);
        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
        method.Invoke(target, new[] { argument });
    }

    private static T InvokePrivateStaticMethod<T>(string methodName)
    {
        var method = typeof(StagePhotoCaptureController).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic
        );
        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
        return (T)method.Invoke(null, null);
    }
}

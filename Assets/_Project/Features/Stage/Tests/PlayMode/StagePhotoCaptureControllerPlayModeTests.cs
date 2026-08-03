using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StagePhotoCaptureControllerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private readonly List<RenderTexture> createdRenderTextures = new();
    private readonly List<Texture2D> createdTextures = new();
    private StageSubject captureSubject;

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

        foreach (var createdTexture in createdTextures)
        {
            if (createdTexture != null)
            {
                Object.DestroyImmediate(createdTexture);
            }
        }

        createdTextures.Clear();
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
    public void TakeCapturedPhotoTransfersOwnershipOnlyOnceAndClearsPreview()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);
        Assert.That(captureController.TryCapture(), Is.True);

        var capturedPhoto = captureController.CapturedPhoto;
        var capturedPhotoPreview = GetPrivateField<RawImage>(
            captureController,
            "capturedPhotoPreview"
        );

        var transferredPhoto = captureController.TakeCapturedPhoto();

        Assert.That(transferredPhoto, Is.SameAs(capturedPhoto));
        Assert.That(captureController.CapturedPhoto, Is.Null);
        Assert.That(captureController.HasCaptured, Is.True);
        Assert.That(capturedPhotoPreview.texture, Is.Null);
        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.False);
        Assert.That(captureController.TakeCapturedPhoto(), Is.Null);
    }

    [UnityTest]
    public IEnumerator UntransferredCapturedImageIsDestroyedWithCaptureController()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);
        Assert.That(captureController.TryCapture(), Is.True);
        var capturedImage = captureController.CapturedPhoto.Image;

        Object.Destroy(captureController.gameObject);
        yield return null;

        Assert.That(capturedImage == null, Is.True);
    }

    [UnityTest]
    public IEnumerator TransferredCapturedImageSurvivesCaptureControllerDestruction()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);
        Assert.That(captureController.TryCapture(), Is.True);
        var transferredPhoto = captureController.TakeCapturedPhoto();
        var capturedImage = transferredPhoto.Image;

        Object.Destroy(captureController.gameObject);
        yield return null;

        Assert.That(capturedImage == null, Is.False);
        Object.DestroyImmediate(capturedImage);
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

    [UnityTest]
    public IEnumerator CaptureWithPresentationAssignsTextureBeforeShowingThePreview()
    {
        var captureController = CreateCaptureController(
            Stage0Controller.Stage0State.Playing,
            withPresentation: true
        );
        var presentation = GetPrivateField<StagePhotoCapturePresentation>(
            captureController,
            "capturePresentation"
        );
        var capturedPhotoPreview = GetPrivateField<RawImage>(
            captureController,
            "capturedPhotoPreview"
        );
        SetPresentationDurations(presentation, 0.02f, 0.02f, 0.02f, 0.02f);
        var photoCamera = GetPrivateField<Camera>(captureController, "photoCamera");
        photoCamera.clearFlags = CameraClearFlags.SolidColor;
        photoCamera.backgroundColor = Color.green;

        Assert.That(captureController.TryCapture(), Is.True);
        Assert.That(
            capturedPhotoPreview.texture,
            Is.EqualTo(captureController.CapturedPhoto.Image)
        );
        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.False);
        Assert.That(presentation.IsPlaying, Is.True);
        var capturedBackgroundPixel = captureController.CapturedPhoto.Image.GetPixel(0, 0);
        Assert.That(capturedBackgroundPixel.g, Is.GreaterThan(0.9f));
        Assert.That(capturedBackgroundPixel.r, Is.LessThan(0.1f));
        Assert.That(capturedBackgroundPixel.b, Is.LessThan(0.1f));
        Assert.That(
            GetPrivateField<Stage0Controller>(captureController, "stageController").CurrentState,
            Is.EqualTo(Stage0Controller.Stage0State.CapturedWaitingForTimeout)
        );

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not complete."
        );

        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.True);
        Assert.That(
            capturedPhotoPreview.texture,
            Is.EqualTo(captureController.CapturedPhoto.Image)
        );
    }

    [UnityTest]
    public IEnumerator TakingCapturedPhotoCancelsPresentationAndKeepsThePreviewHidden()
    {
        var captureController = CreateCaptureController(
            Stage0Controller.Stage0State.Playing,
            withPresentation: true
        );
        var presentation = GetPrivateField<StagePhotoCapturePresentation>(
            captureController,
            "capturePresentation"
        );
        var capturedPhotoPreview = GetPrivateField<RawImage>(
            captureController,
            "capturedPhotoPreview"
        );
        SetPresentationDurations(presentation, 1f, 0f, 0f, 0f);

        Assert.That(captureController.TryCapture(), Is.True);
        Assert.That(presentation.IsPlaying, Is.True);

        var capturedPhoto = captureController.TakeCapturedPhoto();
        yield return null;

        Assert.That(capturedPhoto, Is.Not.Null);
        Assert.That(presentation.IsPlaying, Is.False);
        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.False);
        Assert.That(capturedPhotoPreview.texture, Is.Null);
    }

    [UnityTest]
    public IEnumerator CaptureExcludesOccludedSubjectWithoutRemovingItFromPhotoCameraOutput()
    {
        var captureController = CreateCaptureController(Stage0Controller.Stage0State.Playing);
        var photoCamera = GetPrivateField<Camera>(captureController, "photoCamera");
        var candidate = captureSubject;
        var candidateRenderer = candidate.GetComponent<SpriteRenderer>();
        candidateRenderer.sprite = CreateWhiteSprite();
        candidateRenderer.color = Color.red;
        candidate.transform.localScale = Vector3.one * 2f;

        var frontSubject = CreateSubject(
            "FrontSubject",
            SubjectId.Bird,
            Color.blue,
            sortingOrder: 10
        );
        frontSubject.transform.position = candidate.transform.position;

        yield return null;

        Assert.That(captureController.TryCapture(), Is.True);
        Assert.That(captureController.CapturedSubjects, Has.None.EqualTo(candidate));
        Assert.That(candidateRenderer.enabled, Is.True);
        Assert.That(captureController.CapturedPhoto.GetSubjectCount(SubjectId.Dog), Is.Zero);
        Assert.That(captureController.CapturedPhoto.GetSubjectCount(SubjectId.Bird), Is.EqualTo(1));

        var resultData = StageResultDataFactory.Create(
            captureController.CapturedPhoto,
            "プレイヤー",
            "Stage 0"
        );
        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses[0].BonusName, Is.EqualTo("ハト"));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(1));

        var capturedPixels = captureController.CapturedPhoto.Image.GetPixels32();
        var containsVisibleRed = false;
        foreach (var pixel in capturedPixels)
        {
            if (pixel.r > 200 && pixel.g < 30 && pixel.b < 30)
            {
                containsVisibleRed = true;
                break;
            }
        }

        Assert.That(containsVisibleRed, Is.True);
        Assert.That(photoCamera.targetTexture, Is.Not.Null);
    }

    private StagePhotoCaptureController CreateCaptureController(
        Stage0Controller.Stage0State state,
        bool withPresentation = false
    )
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
        // URPのRender Graphでは、Cameraの出力先RenderTextureに深度バッファが必要。
        var photoRenderTexture = new RenderTexture(128, 72, 24, RenderTextureFormat.ARGB32);
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
        SetPrivateField(subject, "subjectId", SubjectId.Dog);
        subjectObject.layer = LayerMask.NameToLayer("PhotoSubject");
        subjectObject.AddComponent<BoxCollider2D>();
        var subjectRenderer = subjectObject.AddComponent<SpriteRenderer>();
        SetPrivateField(subject, "subjectRenderer", subjectRenderer);
        captureSubject = subject;

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

        if (withPresentation)
        {
            var blackoutObject = CreateUiGameObject("ShutterBlackout");
            var blackout = blackoutObject.AddComponent<CanvasGroup>();
            blackoutObject.SetActive(false);
            var presentation = captureObject.AddComponent<StagePhotoCapturePresentation>();
            SetPrivateField(presentation, "shutterBlackout", blackout);
            SetPrivateField(presentation, "capturedPhotoPreview", capturedPhotoPreview);
            SetPrivateField(
                presentation,
                "capturedPhotoPreviewTransform",
                capturedPhotoPreview.rectTransform
            );
            presentation.ResetPresentation();
            SetPrivateField(captureController, "capturePresentation", presentation);
        }

        captureObject.SetActive(true);
        return captureController;
    }

    private StageSubject CreateSubject(
        string name,
        SubjectId subjectId,
        Color color,
        int sortingOrder
    )
    {
        var subjectObject = CreateGameObject(name);
        subjectObject.layer = LayerMask.NameToLayer("PhotoSubject");
        var renderer = subjectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        subjectObject.AddComponent<BoxCollider2D>();

        var judgementPointObject = CreateGameObject($"{name}JudgementPoint");
        judgementPointObject.transform.SetParent(subjectObject.transform, false);
        var subject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(subject, "judgementPoint", judgementPointObject.transform);
        SetPrivateField(subject, "subjectRenderer", renderer);
        SetPrivateField(subject, "subjectId", subjectId);
        return subject;
    }

    private Sprite CreateWhiteSprite()
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        createdTextures.Add(texture);
        // 既定の100 Pixels Per Unitでは1pxのテストSpriteが0.01ユニットになり、
        // RenderTexture上で画素として確認できないため、1ユニットのSpriteとして作成する。
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 1f
        );
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

    private static IEnumerator WaitUntilOrTimeout(System.Func<bool> predicate, string message)
    {
        const float timeoutSeconds = 1f;
        var startedAt = Time.realtimeSinceStartup;

        while (!predicate())
        {
            if (Time.realtimeSinceStartup - startedAt > timeoutSeconds)
            {
                Assert.Fail(message);
            }

            yield return null;
        }
    }

    private static void SetPresentationDurations(
        StagePhotoCapturePresentation presentation,
        float fadeIn,
        float hold,
        float fadeOut,
        float previewScale
    )
    {
        SetPrivateField(presentation, "blackoutFadeInDuration", fadeIn);
        SetPrivateField(presentation, "blackoutHoldDuration", hold);
        SetPrivateField(presentation, "blackoutFadeOutDuration", fadeOut);
        SetPrivateField(presentation, "capturedPhotoScaleDuration", previewScale);
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

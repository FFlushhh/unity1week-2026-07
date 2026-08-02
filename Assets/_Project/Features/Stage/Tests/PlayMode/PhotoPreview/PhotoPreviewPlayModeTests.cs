using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhotoPreviewPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var stageScene = SceneManager.GetSceneByName("Game_Stage0");
        if (stageScene.IsValid() && stageScene.isLoaded)
        {
            var emptyScene = SceneManager.CreateScene($"{nameof(PhotoPreviewPlayModeTests)}.Empty");
            SceneManager.SetActiveScene(emptyScene);

            var unloadOperation = SceneManager.UnloadSceneAsync(stageScene);
            if (unloadOperation != null)
            {
                yield return unloadOperation;
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator PhotoPreviewKeepsRenderTextureAspectRatio()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var photoPreview = GameObject.Find("PhotoPreview");
        var canvas = GameObject.Find("PhotoPreviewCanvas");
        Assert.That(photoPreview, Is.Not.Null);
        Assert.That(canvas, Is.Not.Null);

        var canvasScaler = canvas.GetComponent<CanvasScaler>();
        Assert.That(canvasScaler, Is.Not.Null);
        Assert.That(
            canvasScaler.uiScaleMode,
            Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize)
        );
        Assert.That(canvasScaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(
            canvasScaler.screenMatchMode,
            Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
        );
        Assert.That(canvasScaler.matchWidthOrHeight, Is.EqualTo(0.5f));

        var aspectRatioFitter = photoPreview.GetComponent<AspectRatioFitter>();
        Assert.That(aspectRatioFitter, Is.Not.Null);
        Assert.That(
            aspectRatioFitter.aspectMode,
            Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent)
        );
        Assert.That(aspectRatioFitter.aspectRatio, Is.EqualTo(16f / 9f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator PhotoPreviewAndPhotoFrameFillTheScreenWithoutLegacyFrameLines()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var viewport = GameObject.Find("PhotoPreviewViewport");
        var photoPreview = GameObject.Find("PhotoPreview");
        Assert.That(viewport, Is.Not.Null);
        Assert.That(photoPreview, Is.Not.Null);
        Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(photoPreview.transform.parent, Is.EqualTo(viewport.transform));

        var viewportRect = viewport.GetComponent<RectTransform>();
        var photoFrame = GameObject.Find("PhotoFrame");
        Assert.That(photoFrame, Is.Not.Null);
        var photoFrameRect = photoFrame.GetComponent<RectTransform>();
        Assert.That(viewportRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(viewportRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(viewportRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(viewportRect.sizeDelta, Is.EqualTo(Vector2.zero));
        Assert.That(photoFrameRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(photoFrameRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(photoFrameRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(photoFrameRect.sizeDelta, Is.EqualTo(Vector2.zero));
        Assert.That(GameObject.Find("FrameTop"), Is.Null);
        Assert.That(GameObject.Find("FrameBottom"), Is.Null);
        Assert.That(GameObject.Find("FrameLeft"), Is.Null);
        Assert.That(GameObject.Find("FrameRight"), Is.Null);
    }

    [UnityTest]
    public IEnumerator CameraUiUsesTheConfiguredDecorationsAndOnlyTheShutterIsInteractive()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var canvas = GameObject.Find("PhotoPreviewCanvas").transform;
        var photoFrame = canvas.Find("PhotoFrame");
        Assert.That(photoFrame, Is.Not.Null);

        AssertDecoration(photoFrame, "CameraUiFrame", Vector2.zero, new Vector2(1920f, 1080f));
        AssertDecoration(
            photoFrame,
            "CameraSwitchDecoration",
            new Vector2(-796f, 377f),
            new Vector2(120f, 120f)
        );
        AssertDecoration(
            photoFrame,
            "PhotoVideoModeDecoration",
            new Vector2(-546f, 15f),
            new Vector2(114f, 297f)
        );
        AssertDecoration(
            photoFrame,
            "ZoomSelectorDecoration",
            new Vector2(-383f, 14f),
            new Vector2(100f, 293f)
        );

        var thumbnailSlot = photoFrame.Find("ThumbnailSlot");
        Assert.That(thumbnailSlot, Is.Not.Null);
        Assert.That(
            thumbnailSlot.GetComponent<RectTransform>().anchoredPosition,
            Is.EqualTo(new Vector2(-793f, -337f))
        );
        Assert.That(
            thumbnailSlot.GetComponent<RectTransform>().sizeDelta,
            Is.EqualTo(new Vector2(146f, 146f))
        );
        AssertDecoration(
            photoFrame,
            "ThumbnailFrame",
            new Vector2(-793f, -337f),
            new Vector2(146f, 146f)
        );

        var shutterButton = photoFrame.Find("ShutterButton");
        Assert.That(shutterButton, Is.Not.Null);
        Assert.That(shutterButton.GetComponent<Button>(), Is.Not.Null);
        Assert.That(
            shutterButton.GetComponent<RectTransform>().anchoredPosition,
            Is.EqualTo(new Vector2(-773f, 12f))
        );
        Assert.That(
            shutterButton.GetComponent<RectTransform>().sizeDelta,
            Is.EqualTo(new Vector2(243f, 243f))
        );
        Assert.That(shutterButton.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(shutterButton.GetComponent<Image>().preserveAspect, Is.True);
        Assert.That(shutterButton.Find("ShutterLabel"), Is.Null);

        AssertDecoration(
            photoFrame,
            "FlashDecoration",
            new Vector2(835f, 377f),
            new Vector2(120f, 120f)
        );
        AssertDecoration(
            photoFrame,
            "LivePhotoDecoration",
            new Vector2(835f, 198f),
            new Vector2(120f, 120f)
        );
        AssertDecoration(
            photoFrame,
            "AspectRatioDecoration",
            new Vector2(835f, 16f),
            new Vector2(120f, 120f)
        );
        AssertDecoration(
            photoFrame,
            "CameraTimerDecoration",
            new Vector2(835f, -159f),
            new Vector2(120f, 120f)
        );
        AssertDecoration(
            photoFrame,
            "CameraControlsMenuDecoration",
            new Vector2(835f, -337f),
            new Vector2(120f, 120f)
        );
    }

    [UnityTest]
    public IEnumerator ShutterBlackoutIsClippedInsideTheViewportAndKeepsThePreviewLayout()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var canvas = GameObject.Find("PhotoPreviewCanvas").transform;
        var viewport = canvas.Find("PhotoPreviewViewport");
        var blackout = viewport.Find("ShutterBlackout");
        var photoPreview = viewport.Find("PhotoPreview");
        var photoFrame = canvas.Find("PhotoFrame");
        var capturedPreview = canvas.Find("CapturedPhotoPreview").GetComponent<RectTransform>();

        var photoFrameRect = photoFrame.GetComponent<RectTransform>();
        var viewportRect = viewport.GetComponent<RectTransform>();
        Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(blackout, Is.Not.Null);
        Assert.That(blackout.parent, Is.EqualTo(viewport));
        Assert.That(viewportRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(viewportRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(photoFrameRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(photoFrameRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(blackout.GetSiblingIndex(), Is.GreaterThan(photoPreview.GetSiblingIndex()));
        Assert.That(photoFrame.GetSiblingIndex(), Is.GreaterThan(viewport.GetSiblingIndex()));

        var blackoutRect = blackout.GetComponent<RectTransform>();
        var blackoutImage = blackout.GetComponent<Image>();
        var blackoutCanvasGroup = blackout.GetComponent<CanvasGroup>();
        Assert.That(blackoutRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(blackoutRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(blackoutRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(blackoutRect.sizeDelta, Is.EqualTo(Vector2.zero));
        Assert.That(blackoutImage.raycastTarget, Is.False);
        Assert.That(blackoutCanvasGroup.alpha, Is.EqualTo(0f));
        Assert.That(blackoutCanvasGroup.interactable, Is.False);
        Assert.That(blackoutCanvasGroup.blocksRaycasts, Is.False);
        Assert.That(blackout.gameObject.activeSelf, Is.False);

        Assert.That(capturedPreview.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(capturedPreview.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(capturedPreview.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(capturedPreview.anchoredPosition, Is.EqualTo(new Vector2(-168f, 105f)));
        Assert.That(capturedPreview.sizeDelta, Is.EqualTo(new Vector2(288f, 162f)));
    }

    [UnityTest]
    public IEnumerator SubjectsAreRenderedOnlyByThePhotoCameraAndSpawnNearItsFrame()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var photoSubjectLayer = LayerMask.NameToLayer("PhotoSubject");
        Assert.That(photoSubjectLayer, Is.GreaterThanOrEqualTo(0));

        var mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        Assert.That(mainCamera.cullingMask & (1 << photoSubjectLayer), Is.EqualTo(0));
        Assert.That(photoCamera.cullingMask & (1 << photoSubjectLayer), Is.Not.EqualTo(0));

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var spawnSettings = GetPrivateField<Array>(timeline, "spawnSettings");
        var halfWidth = photoCamera.orthographicSize * photoCamera.aspect;

        foreach (var spawnSetting in spawnSettings)
        {
            var settingType = spawnSetting.GetType();
            var spawnPosition = (Vector2)
                settingType
                    .GetProperty("SpawnPosition", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);
            var subjectPrefab = (GameObject)
                settingType
                    .GetProperty("SubjectPrefab", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);

            Assert.That(subjectPrefab.layer, Is.EqualTo(photoSubjectLayer));
            Assert.That(Mathf.Abs(spawnPosition.x), Is.EqualTo(9.5f));
            Assert.That(Mathf.Abs(spawnPosition.x), Is.GreaterThan(halfWidth));
            Assert.That(Mathf.Abs(spawnPosition.y), Is.LessThan(photoCamera.orthographicSize));
        }
    }

    [UnityTest]
    public IEnumerator SubjectsHaveCenteredJudgementPointsAndTheSceneJudgeUsesThePhotoFrame()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var judgeObject = GameObject.Find("PhotoFrameSubjectJudge");
        Assert.That(judgeObject, Is.Not.Null);
        var judge = judgeObject.GetComponent<PhotoFrameSubjectJudge>();
        Assert.That(judge, Is.Not.Null);
        var photoCamera = GetPrivateField<Camera>(judge, "photoCamera");
        var photoFrame = GetPrivateField<RectTransform>(judge, "photoFrame");
        Assert.That(photoCamera, Is.EqualTo(GameObject.Find("PhotoCamera").GetComponent<Camera>()));
        Assert.That(
            photoFrame,
            Is.EqualTo(GameObject.Find("PhotoFrame").GetComponent<RectTransform>())
        );

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var spawnSettings = GetPrivateField<Array>(timeline, "spawnSettings");
        foreach (var spawnSetting in spawnSettings)
        {
            var subjectPrefab = (GameObject)
                spawnSetting
                    .GetType()
                    .GetProperty("SubjectPrefab", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);
            var stageSubject = subjectPrefab.GetComponent<StageSubject>();

            Assert.That(stageSubject, Is.Not.Null);
            Assert.That(stageSubject.JudgementPoint, Is.Not.Null);
            Assert.That(stageSubject.JudgementPoint.name, Is.EqualTo("JudgementPoint"));
            Assert.That(stageSubject.JudgementPoint.parent, Is.EqualTo(subjectPrefab.transform));
            Assert.That(stageSubject.JudgementPoint.localPosition, Is.EqualTo(Vector3.zero));
        }
    }

    [UnityTest]
    public IEnumerator SceneJudgeClassifiesThePhotoCameraViewportAndItsBorders()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var judge = GameObject
            .Find("PhotoFrameSubjectJudge")
            .GetComponent<PhotoFrameSubjectJudge>();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        var subjectObject = new GameObject("JudgeTestSubject");
        var judgementPointObject = new GameObject("JudgementPoint");
        judgementPointObject.transform.SetParent(subjectObject.transform);
        var subject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(subject, "judgementPoint", judgementPointObject.transform);

        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, 10f)
        );
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.True);

        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0f, 0.5f, 10f)
        );
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.False);

        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 1f, 10f)
        );
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.False);

        UnityEngine.Object.Destroy(subjectObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SceneProvidesEventSystemAndCaptureControllerForTheShutterButton()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var eventSystemObject = GameObject.Find("EventSystem");
        Assert.That(eventSystemObject, Is.Not.Null);
        Assert.That(eventSystemObject.GetComponent<EventSystem>(), Is.Not.Null);
        Assert.That(eventSystemObject.GetComponent("InputSystemUIInputModule"), Is.Not.Null);

        var captureController = GameObject
            .Find("StagePhotoCaptureController")
            .GetComponent<StagePhotoCaptureController>();
        Assert.That(captureController, Is.Not.Null);
        Assert.That(GetPrivateField<object>(captureController, "shutterAction"), Is.Not.Null);
        Assert.That(GetPrivateField<Button>(captureController, "shutterButton"), Is.Not.Null);
        Assert.That(
            GetPrivateField<Camera>(captureController, "photoCamera"),
            Is.EqualTo(GameObject.Find("PhotoCamera").GetComponent<Camera>())
        );
        Assert.That(
            GameObject.Find("PhotoCamera").GetComponent<Camera>().targetTexture,
            Is.Not.Null
        );
        var capturedPhotoPreview = GameObject
            .Find("PhotoPreviewCanvas")
            .transform.Find("CapturedPhotoPreview")
            .GetComponent<RawImage>();
        Assert.That(
            GetPrivateField<RawImage>(captureController, "capturedPhotoPreview"),
            Is.EqualTo(capturedPhotoPreview)
        );
        Assert.That(capturedPhotoPreview.gameObject.activeSelf, Is.False);

        var capturePresentation = GetPrivateField<StagePhotoCapturePresentation>(
            captureController,
            "capturePresentation"
        );
        Assert.That(capturePresentation, Is.Not.Null);
        Assert.That(
            GetPrivateField<CanvasGroup>(capturePresentation, "shutterBlackout"),
            Is.EqualTo(
                GameObject
                    .Find("PhotoPreviewCanvas")
                    .transform.Find("PhotoPreviewViewport/ShutterBlackout")
                    .GetComponent<CanvasGroup>()
            )
        );
        Assert.That(
            GetPrivateField<RawImage>(capturePresentation, "capturedPhotoPreview"),
            Is.EqualTo(capturedPhotoPreview)
        );
        Assert.That(
            GetPrivateField<RectTransform>(capturePresentation, "capturedPhotoPreviewTransform"),
            Is.EqualTo(capturedPhotoPreview.rectTransform)
        );
        Assert.That(
            GameObject.Find("PhotoPreviewCanvas").GetComponent<Canvas>().renderMode,
            Is.EqualTo(RenderMode.ScreenSpaceOverlay)
        );
    }

    [UnityTest]
    public IEnumerator SceneCaptureDefersPreviewWhileThePresentationRunsAndKeepsTheTimerUpdating()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);
        yield return null;

        var stageController = GameObject.Find("GameController").GetComponent<Stage0Controller>();
        var captureController = GameObject
            .Find("StagePhotoCaptureController")
            .GetComponent<StagePhotoCaptureController>();
        var presentation = GetPrivateField<StagePhotoCapturePresentation>(
            captureController,
            "capturePresentation"
        );
        var capturedPreview = GetPrivateField<RawImage>(captureController, "capturedPhotoPreview");
        var blackout = GetPrivateField<CanvasGroup>(presentation, "shutterBlackout");
        SetPrivateField(stageController, "currentState", Stage0Controller.Stage0State.Playing);
        SetPrivateField(stageController, "remainingTime", 10f);
        SetPresentationDurations(presentation, 0.02f, 0.02f, 0.02f, 0.02f);

        Assert.That(captureController.TryCapture(), Is.True);
        Assert.That(
            stageController.CurrentState,
            Is.EqualTo(Stage0Controller.Stage0State.CapturedWaitingForTimeout)
        );
        Assert.That(presentation.IsPlaying, Is.True);
        Assert.That(blackout.gameObject.activeSelf, Is.True);
        Assert.That(capturedPreview.texture, Is.EqualTo(captureController.CapturedPhoto.Image));
        Assert.That(capturedPreview.gameObject.activeSelf, Is.False);
        var remainingTimeAfterCapture = stageController.RemainingTime;

        yield return null;

        Assert.That(stageController.RemainingTime, Is.LessThan(remainingTimeAfterCapture));

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not complete."
        );

        Assert.That(capturedPreview.gameObject.activeSelf, Is.True);
        Assert.That(capturedPreview.texture, Is.EqualTo(captureController.CapturedPhoto.Image));
    }

    [UnityTest]
    public IEnumerator SceneConnectsResultAndTitleTransitionsWithoutVisibilityCamera()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var gameController = GameObject.Find("GameController");
        var transitionController = gameController.GetComponent<Stage0SceneTransitionController>();
        var stageController = gameController.GetComponent<Stage0Controller>();
        var captureController = GameObject
            .Find("StagePhotoCaptureController")
            .GetComponent<StagePhotoCaptureController>();

        Assert.That(transitionController, Is.Not.Null);
        Assert.That(
            GetPrivateField<Stage0Controller>(transitionController, "stageController"),
            Is.EqualTo(stageController)
        );
        Assert.That(
            GetPrivateField<StagePhotoCaptureController>(
                transitionController,
                "stagePhotoCaptureController"
            ),
            Is.EqualTo(captureController)
        );
        Assert.That(
            GetPrivateField<string>(transitionController, "resultSceneName"),
            Is.EqualTo("ResultScene")
        );
        Assert.That(
            GetPrivateField<string>(transitionController, "titleSceneName"),
            Is.EqualTo("Title")
        );

        var gameOverContent = GetPrivateField<GameObject>(stageController, "gameOverContent");
        var returnToTitleButton = gameOverContent.GetComponentInChildren<Button>(
            includeInactive: true
        );
        Assert.That(returnToTitleButton, Is.Not.Null);
        Assert.That(returnToTitleButton.onClick.GetPersistentEventCount(), Is.EqualTo(1));
        Assert.That(
            returnToTitleButton.onClick.GetPersistentTarget(0),
            Is.EqualTo(transitionController)
        );
        Assert.That(
            returnToTitleButton.onClick.GetPersistentMethodName(0),
            Is.EqualTo("ReturnToTitle")
        );
        Assert.That(GameObject.Find("VisibilityCamera"), Is.Null);
    }

    private static IEnumerator WaitUntilOrTimeout(Func<bool> predicate, string message)
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

    private static void AssertDecoration(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        var decoration = parent.Find(name);
        Assert.That(decoration, Is.Not.Null, $"{name} was not found.");
        Assert.That(decoration.GetComponent<Button>(), Is.Null, $"{name} must be decorative only.");

        var image = decoration.GetComponent<Image>();
        Assert.That(image, Is.Not.Null, $"{name} must have an Image component.");
        Assert.That(image.sprite, Is.Not.Null, $"{name} must reference its Sprite.");
        Assert.That(image.raycastTarget, Is.False, $"{name} must not block shutter input.");
        Assert.That(image.preserveAspect, Is.True);

        var rectTransform = decoration.GetComponent<RectTransform>();
        Assert.That(rectTransform.anchoredPosition, Is.EqualTo(anchoredPosition));
        Assert.That(rectTransform.sizeDelta, Is.EqualTo(sizeDelta));
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
}

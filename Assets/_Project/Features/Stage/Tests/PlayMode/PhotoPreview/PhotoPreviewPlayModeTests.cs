using System;
using System.Collections;
using System.Collections.Generic;
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
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        Assert.That(photoPreview, Is.Not.Null);
        Assert.That(photoCamera, Is.Not.Null);
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
        Assert.That(aspectRatioFitter.aspectRatio, Is.EqualTo(4f / 3f).Within(0.001f));
        var renderTexture = photoCamera.targetTexture;
        Assert.That(renderTexture, Is.Not.Null);
        Assert.That(renderTexture.width, Is.EqualTo(1440));
        Assert.That(renderTexture.height, Is.EqualTo(1080));
    }

    [UnityTest]
    public IEnumerator PhotoPreviewAndPhotoFrameUseTheCenteredFourByThreeCameraArea()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var viewport = GameObject.Find("PhotoPreviewViewport");
        var canvas = GameObject.Find("PhotoPreviewCanvas");
        Assert.That(canvas, Is.Not.Null);
        var photoPreview = GameObject.Find("PhotoPreview");
        Assert.That(viewport, Is.Not.Null);
        Assert.That(photoPreview, Is.Not.Null);
        Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(photoPreview.transform.parent, Is.EqualTo(viewport.transform));

        var viewportRect = viewport.GetComponent<RectTransform>();
        var photoFrame = GameObject.Find("PhotoFrame");
        Assert.That(photoFrame, Is.Not.Null);
        var photoFrameRect = photoFrame.GetComponent<RectTransform>();
        Assert.That(viewportRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(viewportRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(viewportRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(viewportRect.sizeDelta, Is.EqualTo(new Vector2(1440f, 1080f)));
        Assert.That(photoFrameRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(photoFrameRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(photoFrameRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(photoFrameRect.sizeDelta, Is.EqualTo(new Vector2(1440f, 1080f)));
        Assert.That(GameObject.Find("FrameTop"), Is.Null);

        var cameraUiBackground = canvas.transform.Find("CameraUiBackground");
        Assert.That(cameraUiBackground, Is.Not.Null);
        var backgroundRect = cameraUiBackground.GetComponent<RectTransform>();
        var backgroundImage = cameraUiBackground.GetComponent<Image>();
        Assert.That(backgroundRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(backgroundRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(backgroundRect.sizeDelta, Is.EqualTo(Vector2.zero));
        Assert.That(backgroundImage.color, Is.EqualTo(Color.black));
        Assert.That(backgroundImage.raycastTarget, Is.False);
        Assert.That(
            cameraUiBackground.GetSiblingIndex(),
            Is.LessThan(viewport.transform.GetSiblingIndex())
        );
        Assert.That(GameObject.Find("FrameBottom"), Is.Null);
        Assert.That(GameObject.Find("FrameLeft"), Is.Null);
        Assert.That(GameObject.Find("FrameRight"), Is.Null);
    }

    [UnityTest]
    public IEnumerator TimerIsPositionedAtTheTopCenterWithoutOverlappingSideControls()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var timer = GameObject.Find("TimerText");
        Assert.That(timer, Is.Not.Null);

        var timerRect = timer.GetComponent<RectTransform>();
        Assert.That(timerRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
        Assert.That(timerRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
        Assert.That(timerRect.anchoredPosition, Is.EqualTo(new Vector2(0f, -60f)));
        Assert.That(timerRect.sizeDelta, Is.EqualTo(new Vector2(220f, 80f)));
    }

    [UnityTest]
    public IEnumerator CameraUiUsesTheConfiguredDecorationsAndOnlyTheShutterIsInteractive()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var canvas = GameObject.Find("PhotoPreviewCanvas").transform;
        var photoFrame = canvas.Find("PhotoFrame");
        Assert.That(photoFrame, Is.Not.Null);

        Assert.That(photoFrame.Find("CameraUiFrame"), Is.Null);
        AssertDecoration(
            photoFrame,
            "CameraSwitchDecoration",
            new Vector2(-840f, -440f),
            new Vector2(100f, 100f)
        );
        AssertDecoration(
            photoFrame,
            "PhotoVideoModeDecoration",
            new Vector2(-570f, 180f),
            new Vector2(80f, 210f)
        );
        AssertDecoration(
            photoFrame,
            "ZoomSelectorDecoration",
            new Vector2(-570f, -180f),
            new Vector2(75f, 200f)
        );

        var thumbnailSlot = photoFrame.Find("ThumbnailSlot");
        Assert.That(thumbnailSlot, Is.Not.Null);
        var thumbnailSlotRect = thumbnailSlot.GetComponent<RectTransform>();
        Assert.That(thumbnailSlotRect.anchoredPosition, Is.EqualTo(new Vector2(-840f, 440f)));
        Assert.That(thumbnailSlotRect.sizeDelta, Is.EqualTo(new Vector2(146f, 146f)));

        var thumbnailMask = thumbnailSlot.Find("ThumbnailMask");
        Assert.That(thumbnailMask, Is.Not.Null);
        var thumbnailMaskRect = thumbnailMask.GetComponent<RectTransform>();
        Assert.That(thumbnailMaskRect.sizeDelta, Is.EqualTo(new Vector2(120f, 120f)));
        var maskImage = thumbnailMask.GetComponent<Image>();
        Assert.That(maskImage, Is.Not.Null);
        Assert.That(maskImage.raycastTarget, Is.False);
        var mask = thumbnailMask.GetComponent<Mask>();
        Assert.That(mask, Is.Not.Null);
        Assert.That(mask.showMaskGraphic, Is.False);

        var capturedPreview = thumbnailMask.Find("CapturedPhotoPreview");
        Assert.That(capturedPreview, Is.Not.Null);
        Assert.That(capturedPreview.gameObject.activeSelf, Is.False);
        var capturedPreviewRect = capturedPreview.GetComponent<RectTransform>();
        Assert.That(capturedPreviewRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(capturedPreviewRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(capturedPreviewRect.sizeDelta, Is.EqualTo(Vector2.zero));
        Assert.That(capturedPreviewRect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        var capturedPreviewFitter = capturedPreview.GetComponent<AspectRatioFitter>();
        Assert.That(capturedPreviewFitter, Is.Not.Null);
        Assert.That(
            capturedPreviewFitter.aspectMode,
            Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent)
        );
        Assert.That(capturedPreviewFitter.aspectRatio, Is.EqualTo(4f / 3f).Within(0.001f));

        var thumbnailFrame = thumbnailSlot.Find("ThumbnailFrame");
        Assert.That(thumbnailFrame, Is.Not.Null);
        var thumbnailFrameRect = thumbnailFrame.GetComponent<RectTransform>();
        Assert.That(thumbnailFrameRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(thumbnailFrameRect.sizeDelta, Is.EqualTo(new Vector2(146f, 146f)));
        Assert.That(
            thumbnailFrame.GetSiblingIndex(),
            Is.GreaterThan(thumbnailMask.GetSiblingIndex())
        );
        Assert.That(thumbnailFrame.GetComponent<Image>().raycastTarget, Is.False);
        var shutterButton = photoFrame.Find("ShutterButton");
        Assert.That(shutterButton, Is.Not.Null);
        Assert.That(shutterButton.GetComponent<Button>(), Is.Not.Null);
        Assert.That(
            shutterButton.GetComponent<RectTransform>().anchoredPosition,
            Is.EqualTo(new Vector2(-840f, 0f))
        );
        Assert.That(
            shutterButton.GetComponent<RectTransform>().sizeDelta,
            Is.EqualTo(new Vector2(180f, 180f))
        );
        Assert.That(shutterButton.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(shutterButton.GetComponent<Image>().preserveAspect, Is.True);
        Assert.That(shutterButton.Find("ShutterLabel"), Is.Null);

        AssertDecoration(
            photoFrame,
            "FlashDecoration",
            new Vector2(840f, 420f),
            new Vector2(100f, 100f)
        );
        AssertDecoration(
            photoFrame,
            "LivePhotoDecoration",
            new Vector2(840f, 210f),
            new Vector2(100f, 100f)
        );
        AssertDecoration(
            photoFrame,
            "AspectRatioDecoration",
            new Vector2(840f, 0f),
            new Vector2(100f, 100f)
        );
        AssertDecoration(
            photoFrame,
            "CameraTimerDecoration",
            new Vector2(840f, -210f),
            new Vector2(100f, 100f)
        );
        AssertDecoration(
            photoFrame,
            "CameraControlsMenuDecoration",
            new Vector2(840f, -420f),
            new Vector2(100f, 100f)
        );
    }

    [UnityTest]
    public IEnumerator CameraUiUsesOnlyRuntimeSpritesAndTheShutterIsTheOnlyButton()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var photoFrame = GameObject.Find("PhotoFrame").transform;
        Assert.That(photoFrame.Find("CameraUiFrame"), Is.Null);
        AssertSpriteTextureName(photoFrame, "CameraSwitchDecoration", "camera_switch_button");
        AssertSpriteTextureName(photoFrame, "ShutterButton", "shutter_button");
        AssertSpriteTextureName(
            photoFrame.Find("ThumbnailSlot"),
            "ThumbnailFrame",
            "thumbnail_frame"
        );
        AssertSpriteTextureName(
            photoFrame,
            "PhotoVideoModeDecoration",
            "photo_video_mode_selector"
        );
        AssertSpriteTextureName(photoFrame, "ZoomSelectorDecoration", "zoom_selector");
        AssertSpriteTextureName(photoFrame, "FlashDecoration", "flash_button");
        AssertSpriteTextureName(photoFrame, "LivePhotoDecoration", "live_photo_button");
        AssertSpriteTextureName(photoFrame, "AspectRatioDecoration", "aspect_ratio_button");
        AssertSpriteTextureName(photoFrame, "CameraTimerDecoration", "timer_button");
        AssertSpriteTextureName(
            photoFrame,
            "CameraControlsMenuDecoration",
            "camera_controls_menu_button"
        );

        var allowedTextureNames = new HashSet<string>
        {
            "camera_switch_button",
            "shutter_button",
            "thumbnail_frame",
            "photo_video_mode_selector",
            "zoom_selector",
            "flash_button",
            "live_photo_button",
            "aspect_ratio_button",
            "timer_button",
            "camera_controls_menu_button",
        };
        foreach (var image in photoFrame.GetComponentsInChildren<Image>(includeInactive: true))
        {
            Assert.That(image.sprite, Is.Not.Null, $"{image.name} must use a UI Sprite.");
            Assert.That(
                allowedTextureNames.Contains(image.sprite.texture.name),
                Is.True,
                $"{image.name} must not reference a complete preview or sprite sheet."
            );
        }

        var buttons = photoFrame.GetComponentsInChildren<Button>(includeInactive: true);
        Assert.That(buttons, Has.Length.EqualTo(1));
        Assert.That(buttons[0].gameObject.name, Is.EqualTo("ShutterButton"));
        Assert.That(GameObject.Find("ExposureDecoration"), Is.Null);
        Assert.That(GameObject.Find("StylesDecoration"), Is.Null);
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
        var capturedPreview = canvas
            .Find("PhotoFrame/ThumbnailSlot/ThumbnailMask/CapturedPhotoPreview")
            .GetComponent<RectTransform>();

        var photoFrameRect = photoFrame.GetComponent<RectTransform>();
        var viewportRect = viewport.GetComponent<RectTransform>();
        Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(blackout, Is.Not.Null);
        Assert.That(blackout.parent, Is.EqualTo(viewport));
        Assert.That(viewportRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(viewportRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(photoFrameRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(photoFrameRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(blackout.GetSiblingIndex(), Is.GreaterThan(photoPreview.GetSiblingIndex()));
        Assert.That(viewportRect.sizeDelta, Is.EqualTo(new Vector2(1440f, 1080f)));
        Assert.That(photoFrameRect.sizeDelta, Is.EqualTo(new Vector2(1440f, 1080f)));
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

        Assert.That(capturedPreview.parent.name, Is.EqualTo("ThumbnailMask"));
        Assert.That(capturedPreview.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(capturedPreview.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(capturedPreview.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(capturedPreview.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(capturedPreview.sizeDelta, Is.EqualTo(Vector2.zero));
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

        foreach (var spawnRoute in GetConfiguredSpawnRoutes(spawnSettings))
        {
            var spawnPosition = (Vector2)
                spawnRoute
                    .GetType()
                    .GetProperty("SpawnPosition", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnRoute);
            var subjectPrefab = (GameObject)
                spawnRoute
                    .GetType()
                    .GetProperty("SubjectPrefab", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnRoute);

            Assert.That(subjectPrefab.layer, Is.EqualTo(photoSubjectLayer));
            Assert.That(Mathf.Abs(spawnPosition.x), Is.EqualTo(9.5f));
            Assert.That(Mathf.Abs(spawnPosition.x), Is.GreaterThan(halfWidth));
            Assert.That(Mathf.Abs(spawnPosition.y), Is.LessThan(photoCamera.orthographicSize));
        }
    }

    [UnityTest]
    public IEnumerator SubjectsHaveMeaningfulJudgementPointsAndTheSceneJudgeUsesThePhotoFrame()
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
        foreach (var spawnRoute in GetConfiguredSpawnRoutes(spawnSettings))
        {
            var subjectPrefab = (GameObject)
                spawnRoute
                    .GetType()
                    .GetProperty("SubjectPrefab", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnRoute);
            var stageSubject = subjectPrefab.GetComponent<StageSubject>();

            Assert.That(stageSubject, Is.Not.Null);
            Assert.That(stageSubject.JudgementPoint, Is.Not.Null);
            Assert.That(stageSubject.JudgementPoint.name, Is.EqualTo("JudgementPoint"));
            Assert.That(stageSubject.JudgementPoint.parent, Is.EqualTo(subjectPrefab.transform));
            Assert.That(stageSubject.PathAnchor, Is.Not.Null);

            // 判断点はSprite中心ではなく、被写体ごとに意味のある位置（顔・胴体・袋本体）へ置く。
            Assert.That(
                stageSubject.JudgementPoint.localPosition,
                Is.EqualTo(ExpectedJudgementPoints[stageSubject.Id])
            );

            Assert.That(
                subjectPrefab.GetComponent<PolygonCollider2D>(),
                Is.Not.Null,
                $"{stageSubject.Id} must approximate its opaque outline with a PolygonCollider2D."
            );
            Assert.That(
                subjectPrefab.GetComponent<BoxCollider2D>(),
                Is.Null,
                $"{stageSubject.Id} must not keep the placeholder BoxCollider2D."
            );

            // OverlapPointは物理シーンへ登録済みのColliderにしか効かないため、Prefabを実体化して判定する。
            var subjectInstance = UnityEngine.Object.Instantiate(subjectPrefab);
            try
            {
                Physics2D.SyncTransforms();
                var instanceSubject = subjectInstance.GetComponent<StageSubject>();
                var instanceCollider = subjectInstance.GetComponent<PolygonCollider2D>();
                Assert.That(
                    instanceCollider.OverlapPoint(instanceSubject.JudgementPoint.position),
                    Is.True,
                    $"{stageSubject.Id} must place its judgement point inside its own collider."
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(subjectInstance);
            }

            switch (stageSubject.Id)
            {
                case SubjectId.Dog:
                    Assert.That(stageSubject.PathAnchor.name, Is.EqualTo("FootPoint"));
                    Assert.That(
                        stageSubject.PathAnchor.localPosition,
                        Is.EqualTo(new Vector3(0f, -1.86f, 0f))
                    );
                    break;
                case SubjectId.DirtyClothesPerson:
                    Assert.That(stageSubject.PathAnchor.name, Is.EqualTo("FootPoint"));
                    Assert.That(
                        stageSubject.PathAnchor.localPosition,
                        Is.EqualTo(new Vector3(0f, -12.52f, 0f))
                    );
                    break;
                case SubjectId.RabidDog:
                    Assert.That(stageSubject.PathAnchor.name, Is.EqualTo("FootPoint"));
                    Assert.That(
                        stageSubject.PathAnchor.localPosition,
                        Is.EqualTo(new Vector3(0f, -1.57f, 0f))
                    );
                    break;
                default:
                    Assert.That(stageSubject.PathAnchor, Is.EqualTo(subjectPrefab.transform));
                    break;
            }
        }
    }

    /// <summary>
    /// 被写体ごとに確定した判断点のローカル座標。Sprite上の意味のある位置を正本とする。
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<
        SubjectId,
        Vector3
    > ExpectedJudgementPoints = new()
    {
        { SubjectId.Dog, new Vector3(0.58f, 0.6f, 0f) },
        { SubjectId.DirtyClothesPerson, new Vector3(-0.01f, 7.26f, 0f) },
        { SubjectId.RabidDog, new Vector3(1.1f, -0.15f, 0f) },
        { SubjectId.PlasticBag, new Vector3(-0.17f, -0.76f, 0f) },
        { SubjectId.Bird, new Vector3(-0.05f, -0.5f, 0f) },
        { SubjectId.Sparrow, new Vector3(0.1f, -0.67f, 0f) },
    };

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
            .transform.Find("PhotoFrame/ThumbnailSlot/ThumbnailMask/CapturedPhotoPreview")
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

    private static void AssertSpriteTextureName(
        Transform parent,
        string name,
        string expectedTextureName
    )
    {
        Assert.That(parent, Is.Not.Null, $"Parent for {name} was not found.");
        var child = parent.Find(name);
        Assert.That(child, Is.Not.Null, $"{name} was not found.");
        var image = child.GetComponent<Image>();
        Assert.That(image, Is.Not.Null, $"{name} Image was not found.");
        Assert.That(image.sprite, Is.Not.Null, $"{name} Sprite was not found.");
        Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
    }

    private static void AssertDecoration(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        bool preserveAspect = true
    )
    {
        var decoration = parent.Find(name);
        Assert.That(decoration, Is.Not.Null, $"{name} was not found.");
        Assert.That(decoration.GetComponent<Button>(), Is.Null, $"{name} must be decorative only.");

        var image = decoration.GetComponent<Image>();
        Assert.That(image, Is.Not.Null, $"{name} must have an Image component.");
        Assert.That(image.sprite, Is.Not.Null, $"{name} must reference its Sprite.");
        Assert.That(image.raycastTarget, Is.False, $"{name} must not block shutter input.");
        Assert.That(image.preserveAspect, Is.EqualTo(preserveAspect));

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

    private static IEnumerable<object> GetConfiguredSpawnRoutes(Array spawnSettings)
    {
        foreach (var spawnSetting in spawnSettings)
        {
            var settingType = spawnSetting.GetType();
            var isRandom = (bool)
                settingType
                    .GetProperty("IsRandom", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);

            if (!isRandom)
            {
                yield return settingType
                    .GetMethod("CreateFixedRoute", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(spawnSetting, null);
                continue;
            }

            var randomRoutes = GetPrivateField<Array>(spawnSetting, "randomRoutes");
            foreach (var randomRoute in randomRoutes)
            {
                yield return randomRoute;
            }
        }
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

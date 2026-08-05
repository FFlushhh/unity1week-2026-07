using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class Stage1ControllerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private readonly List<Material> createdMaterials = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();

        foreach (var createdMaterial in createdMaterials)
        {
            if (createdMaterial != null)
            {
                Object.DestroyImmediate(createdMaterial);
            }
        }

        createdMaterials.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartMessageHidesTimerAndShutterWhileShowingTheMessage()
    {
        var controller = CreateController(startFocusDuration: 10f, playingDuration: 10f);

        yield return null;

        Assert.That(controller.CurrentState, Is.EqualTo(Stage1Controller.Stage1State.StartMessage));
        Assert.That(GetPrivateField<GameObject>(controller, "startMessage").activeSelf, Is.True);
        Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "shutterButton").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "photoFrame").activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator ZeroPlayingDurationImmediatelyEntersGameOver()
    {
        var controller = CreateController(startFocusDuration: 0f, playingDuration: 0f);

        yield return WaitForState(controller, Stage1Controller.Stage1State.GameOver);

        Assert.That(controller.RemainingTime, Is.Zero);
        Assert.That(GetPrivateField<GameObject>(controller, "gameOverPanel").activeSelf, Is.True);
        Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "photoFrame").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "shutterButton").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "gameOverContent").activeSelf, Is.True);
        Assert.That(GetPrivateField<CanvasGroup>(controller, "gameOverFade").alpha, Is.EqualTo(1f));
        Assert.That(GetPrivateField<TMP_Text>(controller, "timerText").text, Is.EqualTo("0.0"));
    }

    [UnityTest]
    public IEnumerator GameOverHidesContentUntilTheBlackFadeCompletes()
    {
        var controller = CreateController(startFocusDuration: 0f, playingDuration: 0f);
        SetPrivateField(controller, "gameOverFadeDuration", 0.1f);

        yield return WaitForState(controller, Stage1Controller.Stage1State.GameOver);

        Assert.That(
            GetPrivateField<GameObject>(controller, "gameOverContent").activeSelf,
            Is.False
        );
        Assert.That(
            GetPrivateField<CanvasGroup>(controller, "gameOverFade").alpha,
            Is.LessThan(1f)
        );

        yield return new WaitForSeconds(0.2f);

        Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "photoFrame").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "gameOverContent").activeSelf, Is.True);
        Assert.That(GetPrivateField<CanvasGroup>(controller, "gameOverFade").alpha, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator PlayingCountdownReachesZeroAndThenStops()
    {
        var controller = CreateController(startFocusDuration: 0f, playingDuration: 0.2f);

        yield return WaitForState(controller, Stage1Controller.Stage1State.Playing);

        Assert.That(controller.RemainingTime, Is.GreaterThan(0f));
        Assert.That(GetPrivateField<GameObject>(controller, "startMessage").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.True);
        Assert.That(GetPrivateField<GameObject>(controller, "shutterButton").activeSelf, Is.True);
        yield return WaitForState(controller, Stage1Controller.Stage1State.GameOver);

        var remainingTimeAtGameOver = controller.RemainingTime;
        yield return null;

        Assert.That(remainingTimeAtGameOver, Is.Zero);
        Assert.That(controller.RemainingTime, Is.EqualTo(remainingTimeAtGameOver));
        Assert.That(GetPrivateField<TMP_Text>(controller, "timerText").text, Is.EqualTo("0.0"));
    }

    [UnityTest]
    public IEnumerator CapturedWaitingForTimeoutContinuesUntilTimeExpires()
    {
        var controller = CreateController(startFocusDuration: 0f, playingDuration: 10f);

        yield return WaitForState(controller, Stage1Controller.Stage1State.Playing);

        controller.BeginCapturedWaitingForTimeout();

        Assert.That(
            controller.CurrentState,
            Is.EqualTo(Stage1Controller.Stage1State.CapturedWaitingForTimeout)
        );
        Assert.That(controller.RemainingTime, Is.GreaterThan(0f));
        Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.True);
        Assert.That(GetPrivateField<GameObject>(controller, "shutterButton").activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator CaptureAtZeroTransitionsToCompletedOnceAndKeepsGameOverUiHidden()
    {
        var controller = CreateController(startFocusDuration: 0f, playingDuration: 10f);
        var completedNotificationCount = 0;
        controller.StateChanged += state =>
        {
            if (state == Stage1Controller.Stage1State.Completed)
            {
                completedNotificationCount++;
            }
        };

        yield return WaitForState(controller, Stage1Controller.Stage1State.Playing);

        SetPrivateField(controller, "remainingTime", 0f);
        controller.BeginCapturedWaitingForTimeout();
        yield return WaitForState(controller, Stage1Controller.Stage1State.Completed);

        var completedRemainingTime = controller.RemainingTime;
        yield return null;

        Assert.That(controller.RemainingTime, Is.EqualTo(completedRemainingTime));
        Assert.That(completedRemainingTime, Is.Zero);
        Assert.That(completedNotificationCount, Is.EqualTo(1));
        Assert.That(GetPrivateField<GameObject>(controller, "gameOverPanel").activeSelf, Is.False);
        Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.True);
        Assert.That(GetPrivateField<GameObject>(controller, "photoFrame").activeSelf, Is.True);
        Assert.That(GetPrivateField<GameObject>(controller, "shutterButton").activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator PlayingStartsOnlyAfterTheFocusPresentationCompletes()
    {
        var controller = CreateController(startFocusDuration: 0.2f, playingDuration: 10f);
        var focus = GetPrivateField<StagePhotoFocusPresentation>(
            controller,
            "photoFocusPresentation"
        );

        yield return null;

        Assert.That(focus.IsPlaying, Is.True);
        Assert.That(controller.CurrentState, Is.EqualTo(Stage1Controller.Stage1State.StartMessage));

        yield return WaitForState(controller, Stage1Controller.Stage1State.Playing);

        Assert.That(focus.IsPlaying, Is.False);
    }

    [UnityTest]
    public IEnumerator ShutterAndTimerStayHiddenForTheWholeFocusPhase()
    {
        var controller = CreateController(startFocusDuration: 0.15f, playingDuration: 10f);
        var focus = GetPrivateField<StagePhotoFocusPresentation>(
            controller,
            "photoFocusPresentation"
        );

        const double timeoutSeconds = 1d;
        var timeoutAt = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
        while (focus.IsPlaying)
        {
            Assert.That(
                controller.CurrentState,
                Is.EqualTo(Stage1Controller.Stage1State.StartMessage)
            );
            Assert.That(GetPrivateField<GameObject>(controller, "timer").activeSelf, Is.False);
            Assert.That(
                GetPrivateField<GameObject>(controller, "shutterButton").activeSelf,
                Is.False
            );
            Assert.That(controller.RemainingTime, Is.EqualTo(0f));

            if (Time.realtimeSinceStartupAsDouble > timeoutAt)
            {
                Assert.Fail("Focus presentation did not complete in time.");
            }

            yield return null;
        }

        yield return WaitForState(controller, Stage1Controller.Stage1State.Playing);
    }

    [UnityTest]
    public IEnumerator MissingFocusPresentationLogsAnErrorAndStillReachesPlaying()
    {
        var controller = CreateController(startFocusDuration: 0f, playingDuration: 10f);
        SetPrivateField<StagePhotoFocusPresentation>(controller, "photoFocusPresentation", null);

        LogAssert.Expect(
            LogType.Error,
            "[Stage1Controller] Photo focus presentation is not assigned."
        );

        yield return WaitForState(controller, Stage1Controller.Stage1State.Playing);
    }

    [UnityTest]
    public IEnumerator EnteringGameOverDuringTheFocusPhaseClearsTheBlur()
    {
        var controller = CreateController(startFocusDuration: 1f, playingDuration: 10f);
        var focus = GetPrivateField<StagePhotoFocusPresentation>(
            controller,
            "photoFocusPresentation"
        );

        yield return null;

        Assert.That(controller.CurrentState, Is.EqualTo(Stage1Controller.Stage1State.StartMessage));
        Assert.That(focus.IsPlaying, Is.True);

        controller.EnterGameOver();
        yield return null;

        Assert.That(controller.CurrentState, Is.EqualTo(Stage1Controller.Stage1State.GameOver));
        Assert.That(focus.IsPlaying, Is.False);

        yield return new WaitForSeconds(1.2f);

        Assert.That(controller.CurrentState, Is.EqualTo(Stage1Controller.Stage1State.GameOver));
    }

    private Stage1Controller CreateController(float startFocusDuration, float playingDuration)
    {
        var controllerObject = CreateGameObject("Stage1Controller");
        var startMessage = CreateGameObject("StartMessage");
        var timerObject = CreateGameObject("Timer");
        var photoFrame = CreateGameObject("PhotoFrame");
        var shutterButton = CreateGameObject("ShutterButton");
        var gameOverPanel = CreateGameObject("GameOverPanel");
        var gameOverContent = CreateGameObject("GameOverContent");
        var gameOverFade = gameOverPanel.AddComponent<CanvasGroup>();
        var timerText = timerObject.AddComponent<TextMeshProUGUI>();
        var controller = controllerObject.AddComponent<Stage1Controller>();

        var previewObject = CreateGameObject("PhotoPreview");
        var preview = previewObject.AddComponent<RawImage>();
        var blurMaterial = new Material(Shader.Find("Stage/PhotoPreviewBlur"));
        createdMaterials.Add(blurMaterial);
        var focus = controllerObject.AddComponent<StagePhotoFocusPresentation>();
        SetPrivateField(focus, "photoPreview", preview);
        SetPrivateField(focus, "blurMaterialSource", blurMaterial);
        SetPrivateField(focus, "blurClearDuration", startFocusDuration);
        SetPrivateField(focus, "postBlurWaitDuration", 0f);

        gameOverContent.transform.SetParent(gameOverPanel.transform);
        gameOverPanel.SetActive(false);
        SetPrivateField(controller, "photoFocusPresentation", focus);
        SetPrivateField(controller, "playingDuration", playingDuration);
        SetPrivateField(controller, "startMessage", startMessage);
        SetPrivateField(controller, "timer", timerObject);
        SetPrivateField(controller, "photoFrame", photoFrame);
        SetPrivateField(controller, "shutterButton", shutterButton);
        SetPrivateField(controller, "gameOverPanel", gameOverPanel);
        SetPrivateField(controller, "gameOverFadeDuration", 0f);
        SetPrivateField(controller, "gameOverFade", gameOverFade);
        SetPrivateField(controller, "gameOverContent", gameOverContent);
        SetPrivateField(controller, "timerText", timerText);

        return controller;
    }

    private GameObject CreateGameObject(string name)
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static IEnumerator WaitForState(
        Stage1Controller controller,
        Stage1Controller.Stage1State expectedState
    )
    {
        const double timeoutSeconds = 1d;
        var timeoutAt = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
        while (Time.realtimeSinceStartupAsDouble < timeoutAt)
        {
            if (controller.CurrentState == expectedState)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail(
            $"Expected {expectedState}, but the state was {controller.CurrentState} after "
                + $"{timeoutSeconds:0.0} seconds."
        );
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

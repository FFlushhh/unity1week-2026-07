using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Stage0ControllerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();

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
        yield return null;
    }

    [UnityTest]
    public IEnumerator ZeroPlayingDurationImmediatelyEntersGameOver()
    {
        var controller = CreateController(startMessageDuration: 0f, playingDuration: 0f);

        yield return WaitForState(controller, Stage0Controller.Stage0State.GameOver);

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
        var controller = CreateController(startMessageDuration: 0f, playingDuration: 0f);
        SetPrivateField(controller, "gameOverFadeDuration", 0.1f);

        yield return WaitForState(controller, Stage0Controller.Stage0State.GameOver);

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
        var controller = CreateController(startMessageDuration: 0f, playingDuration: 0.2f);

        yield return WaitForState(controller, Stage0Controller.Stage0State.Playing);

        Assert.That(controller.RemainingTime, Is.GreaterThan(0f));
        yield return WaitForState(controller, Stage0Controller.Stage0State.GameOver);

        var remainingTimeAtGameOver = controller.RemainingTime;
        yield return null;

        Assert.That(remainingTimeAtGameOver, Is.Zero);
        Assert.That(controller.RemainingTime, Is.EqualTo(remainingTimeAtGameOver));
        Assert.That(GetPrivateField<TMP_Text>(controller, "timerText").text, Is.EqualTo("0.0"));
    }

    [UnityTest]
    public IEnumerator CapturedWaitingForTimeoutContinuesUntilTimeExpires()
    {
        var controller = CreateController(startMessageDuration: 0f, playingDuration: 10f);

        yield return WaitForState(controller, Stage0Controller.Stage0State.Playing);

        controller.BeginCapturedWaitingForTimeout();

        Assert.That(
            controller.CurrentState,
            Is.EqualTo(Stage0Controller.Stage0State.CapturedWaitingForTimeout)
        );
        Assert.That(controller.RemainingTime, Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator CaptureAtZeroTransitionsToCompletedOnceAndKeepsGameOverUiHidden()
    {
        var controller = CreateController(startMessageDuration: 0f, playingDuration: 10f);
        var completedNotificationCount = 0;
        controller.StateChanged += state =>
        {
            if (state == Stage0Controller.Stage0State.Completed)
            {
                completedNotificationCount++;
            }
        };

        yield return WaitForState(controller, Stage0Controller.Stage0State.Playing);

        SetPrivateField(controller, "remainingTime", 0f);
        controller.BeginCapturedWaitingForTimeout();
        yield return WaitForState(controller, Stage0Controller.Stage0State.Completed);

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

    private Stage0Controller CreateController(float startMessageDuration, float playingDuration)
    {
        var controllerObject = CreateGameObject("Stage0Controller");
        var startMessage = CreateGameObject("StartMessage");
        var timerObject = CreateGameObject("Timer");
        var photoFrame = CreateGameObject("PhotoFrame");
        var shutterButton = CreateGameObject("ShutterButton");
        var gameOverPanel = CreateGameObject("GameOverPanel");
        var gameOverContent = CreateGameObject("GameOverContent");
        var gameOverFade = gameOverPanel.AddComponent<CanvasGroup>();
        var timerText = timerObject.AddComponent<TextMeshProUGUI>();
        var controller = controllerObject.AddComponent<Stage0Controller>();

        gameOverContent.transform.SetParent(gameOverPanel.transform);
        gameOverPanel.SetActive(false);
        SetPrivateField(controller, "startMessageDuration", startMessageDuration);
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
        Stage0Controller controller,
        Stage0Controller.Stage0State expectedState
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

    private static T GetPrivateField<T>(Stage0Controller controller, string fieldName)
    {
        var field = typeof(Stage0Controller).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(controller);
    }

    private static void SetPrivateField<T>(Stage0Controller controller, string fieldName, T value)
    {
        var field = typeof(Stage0Controller).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(controller, value);
    }
}

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
        Assert.That(GetPrivateField<TMP_Text>(controller, "timerText").text, Is.EqualTo("0.0"));
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
    public IEnumerator CaptureAtZeroKeepsCapturedWaitingState()
    {
        var controller = CreateController(startMessageDuration: 0f, playingDuration: 10f);

        yield return WaitForState(controller, Stage0Controller.Stage0State.Playing);

        SetPrivateField(controller, "remainingTime", 0f);
        controller.BeginCapturedWaitingForTimeout();
        yield return null;
        yield return null;

        Assert.That(
            controller.CurrentState,
            Is.EqualTo(Stage0Controller.Stage0State.CapturedWaitingForTimeout)
        );
        Assert.That(controller.RemainingTime, Is.Zero);
        Assert.That(GetPrivateField<GameObject>(controller, "gameOverPanel").activeSelf, Is.False);
    }

    private Stage0Controller CreateController(float startMessageDuration, float playingDuration)
    {
        var controllerObject = CreateGameObject("Stage0Controller");
        var startMessage = CreateGameObject("StartMessage");
        var timerObject = CreateGameObject("Timer");
        var photoFrame = CreateGameObject("PhotoFrame");
        var shutterButton = CreateGameObject("ShutterButton");
        var gameOverPanel = CreateGameObject("GameOverPanel");
        var timerText = timerObject.AddComponent<TextMeshProUGUI>();
        var controller = controllerObject.AddComponent<Stage0Controller>();

        gameOverPanel.SetActive(false);
        SetPrivateField(controller, "startMessageDuration", startMessageDuration);
        SetPrivateField(controller, "playingDuration", playingDuration);
        SetPrivateField(controller, "startMessage", startMessage);
        SetPrivateField(controller, "timer", timerObject);
        SetPrivateField(controller, "photoFrame", photoFrame);
        SetPrivateField(controller, "shutterButton", shutterButton);
        SetPrivateField(controller, "gameOverPanel", gameOverPanel);
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
        const int maximumFrameCount = 60;
        for (var frame = 0; frame < maximumFrameCount; frame++)
        {
            if (controller.CurrentState == expectedState)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail(
            $"Expected {expectedState}, but the state was {controller.CurrentState} after "
                + $"{maximumFrameCount} frames."
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

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class GameFlowSceneTransitionPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private Texture2D capturedImage;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        ResultDataTransporter.CurrentData = null;

        var cleanupScene = SceneManager.CreateScene(
            $"{nameof(GameFlowSceneTransitionPlayModeTests)}.Cleanup.{Guid.NewGuid():N}"
        );
        SceneManager.SetActiveScene(cleanupScene);

        for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
        {
            var scene = SceneManager.GetSceneAt(index);
            if (scene == cleanupScene || !scene.IsValid())
            {
                continue;
            }

            var unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation != null)
            {
                yield return unloadOperation;
            }
        }

        if (capturedImage != null)
        {
            UnityEngine.Object.DestroyImmediate(capturedImage);
        }

        capturedImage = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator SuccessfulCaptureFlowLoadsResultReturnsToTitleAndStartsFreshGame()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var stageController = UnityEngine.Object.FindAnyObjectByType<Stage1Controller>();
        var captureController =
            UnityEngine.Object.FindAnyObjectByType<StagePhotoCaptureController>();
        Assert.That(stageController, Is.Not.Null);
        Assert.That(captureController, Is.Not.Null);

        capturedImage = new Texture2D(2, 2);
        var capturedPhoto = new CapturedPhoto(capturedImage, Array.Empty<StageSubject>());
        SetPrivateField(captureController, "capturedPhoto", capturedPhoto);
        SetPrivateField(captureController, "hasCaptured", true);

        InvokePrivateMethod(
            stageController,
            "TransitionTo",
            Stage1Controller.Stage1State.Completed
        );

        yield return WaitForActiveScene("ResultScene");
        yield return null;

        var resultManager = UnityEngine.Object.FindAnyObjectByType<ResultSceneManager>();
        Assert.That(resultManager, Is.Not.Null);
        Assert.That(ResultDataTransporter.CurrentData, Is.Null);
        Assert.That(GetCapturedPhotoImage(resultManager).texture, Is.SameAs(capturedImage));

        resultManager.OnNextButtonClicked();
        yield return WaitForActiveScene("Title");
        yield return WaitForDestroyed(capturedImage);

        var startButton = GameObject.Find("StartButton").GetComponent<Button>();
        Assert.That(startButton, Is.Not.Null);
        Assert.That(startButton.onClick.GetPersistentEventCount(), Is.GreaterThanOrEqualTo(1));
        var titleManager = startButton.onClick.GetPersistentTarget(0) as MonoBehaviour;
        Assert.That(titleManager, Is.Not.Null);
        Assert.That(titleManager.GetType().Name, Is.EqualTo("TitleManager"));
        SetPrivateField(titleManager, "_fadeDuration", 0f);

        startButton.onClick.Invoke();
        yield return WaitForActiveScene("Game_Stage1");
        yield return null;

        var freshCaptureController =
            UnityEngine.Object.FindAnyObjectByType<StagePhotoCaptureController>();
        Assert.That(freshCaptureController, Is.Not.Null);
        Assert.That(freshCaptureController.HasCaptured, Is.False);
        Assert.That(freshCaptureController.CapturedPhoto, Is.Null);
        Assert.That(ResultDataTransporter.CurrentData, Is.Null);
    }

    [UnityTest]
    public IEnumerator RepeatedGameOverReturnInputLoadsTitleOnce()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var transitionController =
            UnityEngine.Object.FindAnyObjectByType<Stage1SceneTransitionController>();
        Assert.That(transitionController, Is.Not.Null);

        transitionController.ReturnToTitle();
        transitionController.ReturnToTitle();

        yield return WaitForActiveScene("Title");

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Title"));
        Assert.That(ResultDataTransporter.CurrentData, Is.Null);
    }

    private static RawImage GetCapturedPhotoImage(ResultSceneManager manager)
    {
        var field = typeof(ResultSceneManager).GetField("_capturedPhotoImage", PrivateInstance);
        Assert.That(field, Is.Not.Null);
        return (RawImage)field.GetValue(manager);
    }

    private static IEnumerator WaitForActiveScene(string expectedSceneName)
    {
        const float timeoutSeconds = 5f;
        var timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (
            SceneManager.GetActiveScene().name != expectedSceneName
            && Time.realtimeSinceStartup < timeoutAt
        )
        {
            yield return null;
        }

        Assert.That(
            SceneManager.GetActiveScene().name,
            Is.EqualTo(expectedSceneName),
            $"Scene '{expectedSceneName}' was not loaded within {timeoutSeconds} seconds."
        );
    }

    private static IEnumerator WaitForDestroyed(UnityEngine.Object target)
    {
        const float timeoutSeconds = 1f;
        var timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (target != null && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        Assert.That(
            target == null,
            Is.True,
            $"Object '{target}' was not destroyed within {timeoutSeconds} seconds."
        );
    }

    private static void SetPrivateField(object target, string fieldName, object value)
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
}

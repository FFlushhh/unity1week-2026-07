using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ResultScene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class Stage0SceneTransitionControllerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private readonly List<Texture2D> createdTextures = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SceneManagerAPI.overrideAPI = null;
        ResultDataTransporter.CurrentData = null;

        foreach (var createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();

        foreach (var createdTexture in createdTextures)
        {
            if (createdTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(createdTexture);
            }
        }

        createdTextures.Clear();
        yield return null;
    }

    [Test]
    public void TransferWithCapturedPhotoSetsResultDataWithoutCopyingImage()
    {
        var capturedImage = CreateTexture();
        var dog = CreateSubject(SubjectId.Dog);
        var captureController = CreateCaptureController(
            new CapturedPhoto(capturedImage, new[] { dog })
        );
        var transitionController = CreateTransitionController(captureController, "ResultScene");

        var didTransfer = InvokeTryTransferCapturedPhoto(transitionController, out var resultData);

        Assert.That(didTransfer, Is.True);
        Assert.That(resultData, Is.SameAs(ResultDataTransporter.CurrentData));
        Assert.That(resultData.CapturedImage, Is.SameAs(capturedImage));
        Assert.That(resultData.BaseScore, Is.EqualTo(1000));
        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses[0].BonusName, Is.EqualTo("犬"));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(1));
        Assert.That(captureController.CapturedPhoto, Is.Null);
    }

    [Test]
    public void InvalidResultSceneDoesNotTransferCapturedPhoto()
    {
        var capturedImage = CreateTexture();
        var capturedPhoto = new CapturedPhoto(capturedImage, null);
        var captureController = CreateCaptureController(capturedPhoto);
        var transitionController = CreateTransitionController(
            captureController,
            "MissingResultScene"
        );
        LogAssert.Expect(
            LogType.Error,
            "[Stage0SceneTransitionController] Result scene 'MissingResultScene' cannot be loaded."
        );

        var didTransfer = InvokeTryTransferCapturedPhoto(transitionController, out var resultData);

        Assert.That(didTransfer, Is.False);
        Assert.That(resultData, Is.Null);
        Assert.That(ResultDataTransporter.CurrentData, Is.Null);
        Assert.That(captureController.CapturedPhoto, Is.SameAs(capturedPhoto));
    }

    [Test]
    public void MissingCaptureControllerKeepsPreviousResultData()
    {
        var previousData = new ResultData
        {
            PlayerName = "PreviousPlayer",
            LocationName = "PreviousStage",
            BaseScore = 1000,
            Bonuses = new List<BonusInputData>(),
        };
        ResultDataTransporter.CurrentData = previousData;
        var transitionController = CreateTransitionController(null, "ResultScene");
        LogAssert.Expect(
            LogType.Error,
            "[Stage0SceneTransitionController] Photo capture controller is not assigned."
        );

        var didTransfer = InvokeTryTransferCapturedPhoto(transitionController, out var resultData);

        Assert.That(didTransfer, Is.False);
        Assert.That(resultData, Is.Null);
        Assert.That(ResultDataTransporter.CurrentData, Is.SameAs(previousData));
    }

    [UnityTest]
    public IEnumerator InvalidTitleSceneReportsErrorOnlyOnceForRepeatedInput()
    {
        var transitionController = CreateTransitionController(
            null,
            "ResultScene",
            "MissingTitleScene"
        );
        LogAssert.Expect(
            LogType.Error,
            "[Stage0SceneTransitionController] Title scene 'MissingTitleScene' cannot be loaded."
        );

        transitionController.ReturnToTitle();
        transitionController.ReturnToTitle();
        yield return null;
        yield return null;

        Assert.That(
            GetPrivateField<bool>(transitionController, "hasStartedTitleTransition"),
            Is.True
        );
    }

    [UnityTest]
    public IEnumerator CompletedNotificationStartsResultPreparationOnlyOnce()
    {
        var captureController = CreateCaptureController(null);
        var transitionController = CreateTransitionController(captureController, "ResultScene");
        LogAssert.Expect(
            LogType.Error,
            "[Stage0SceneTransitionController] No captured photo is available for the Result scene."
        );

        InvokePrivateMethod(
            transitionController,
            "HandleStageStateChanged",
            Stage0Controller.Stage0State.Completed
        );
        InvokePrivateMethod(
            transitionController,
            "HandleStageStateChanged",
            Stage0Controller.Stage0State.Completed
        );
        yield return null;
        yield return null;

        Assert.That(
            GetPrivateField<bool>(transitionController, "hasStartedResultTransition"),
            Is.True
        );
        Assert.That(ResultDataTransporter.CurrentData, Is.Null);
    }

    [UnityTest]
    public IEnumerator FailedResultLoadRestoresPreviousDataAndDestroysTransferredImage()
    {
        var previousData = new ResultData
        {
            PlayerName = "PreviousPlayer",
            LocationName = "PreviousStage",
            BaseScore = 1000,
            Bonuses = new List<BonusInputData>(),
        };
        ResultDataTransporter.CurrentData = previousData;

        var capturedImage = CreateTexture();
        var captureController = CreateCaptureController(
            new CapturedPhoto(capturedImage, Array.Empty<StageSubject>())
        );
        var transitionController = CreateTransitionController(captureController, "ResultScene");
        SceneManagerAPI.overrideAPI = new ThrowingSceneManagerApi();
        LogAssert.Expect(
            LogType.Exception,
            new Regex("InvalidOperationException: Test scene load failure\\.")
        );

        InvokePrivateMethod(
            transitionController,
            "HandleStageStateChanged",
            Stage0Controller.Stage0State.Completed
        );
        yield return null;
        yield return null;

        Assert.That(ResultDataTransporter.CurrentData, Is.SameAs(previousData));
        Assert.That(captureController.CapturedPhoto, Is.Null);
        Assert.That(capturedImage == null, Is.True);
    }

    private Stage0SceneTransitionController CreateTransitionController(
        StagePhotoCaptureController captureController,
        string resultSceneName,
        string titleSceneName = "Title"
    )
    {
        var transitionObject = CreateGameObject("Stage0SceneTransitionController", active: false);
        var transitionController = transitionObject.AddComponent<Stage0SceneTransitionController>();
        SetPrivateField(transitionController, "stagePhotoCaptureController", captureController);
        SetPrivateField(transitionController, "resultSceneName", resultSceneName);
        SetPrivateField(transitionController, "titleSceneName", titleSceneName);
        return transitionController;
    }

    private StagePhotoCaptureController CreateCaptureController(CapturedPhoto capturedPhoto)
    {
        var previewObject = CreateGameObject("CapturedPhotoPreview", active: false);
        var preview = previewObject.AddComponent<RawImage>();
        preview.texture = capturedPhoto?.Image;

        var captureObject = CreateGameObject("StagePhotoCaptureController", active: false);
        var captureController = captureObject.AddComponent<StagePhotoCaptureController>();
        SetPrivateField(captureController, "capturedPhoto", capturedPhoto);
        SetPrivateField(captureController, "hasCaptured", capturedPhoto != null);
        SetPrivateField(captureController, "capturedPhotoPreview", preview);
        return captureController;
    }

    private StageSubject CreateSubject(SubjectId subjectId)
    {
        var subject = CreateGameObject("Subject").AddComponent<StageSubject>();
        SetPrivateField(subject, "subjectId", subjectId);
        return subject;
    }

    private Texture2D CreateTexture()
    {
        var texture = new Texture2D(2, 2);
        createdTextures.Add(texture);
        return texture;
    }

    private GameObject CreateGameObject(string name, bool active = true)
    {
        var gameObject = new GameObject(name);
        gameObject.SetActive(active);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static bool InvokeTryTransferCapturedPhoto(
        Stage0SceneTransitionController transitionController,
        out ResultData resultData
    )
    {
        var method = typeof(Stage0SceneTransitionController).GetMethod(
            "TryTransferCapturedPhoto",
            PrivateInstance
        );
        Assert.That(method, Is.Not.Null);

        var arguments = new object[] { null, null };
        var didTransfer = (bool)method.Invoke(transitionController, arguments);
        resultData = (ResultData)arguments[0];
        return didTransfer;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(target);
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

    private sealed class ThrowingSceneManagerApi : SceneManagerAPI
    {
        protected override AsyncOperation LoadSceneAsyncByNameOrIndex(
            string sceneName,
            int sceneBuildIndex,
            LoadSceneParameters parameters,
            bool mustCompleteNextFrame
        )
        {
            throw new InvalidOperationException("Test scene load failure.");
        }
    }
}

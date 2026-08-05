using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhotoPreviewFocusPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var stageScene = SceneManager.GetSceneByName("Game_Stage1");
        if (stageScene.IsValid() && stageScene.isLoaded)
        {
            var emptyScene = SceneManager.CreateScene(
                $"{nameof(PhotoPreviewFocusPlayModeTests)}.Empty"
            );
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
    public IEnumerator SceneWiresTheFocusPresentationToTheStageControllerAndThePhotoPreview()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var gameController = GameObject.Find("GameController");
        var stageController = gameController.GetComponent<Stage1Controller>();
        var focusPresentation = gameController.GetComponent<StagePhotoFocusPresentation>();

        Assert.That(focusPresentation, Is.Not.Null);
        Assert.That(
            GetPrivateField<StagePhotoFocusPresentation>(stageController, "photoFocusPresentation"),
            Is.SameAs(focusPresentation)
        );

        var photoPreviewObject = GameObject.Find("PhotoPreview");
        Assert.That(photoPreviewObject.transform.parent.name, Is.EqualTo("PhotoPreviewViewport"));
        var photoPreview = photoPreviewObject.GetComponent<RawImage>();
        Assert.That(
            GetPrivateField<RawImage>(focusPresentation, "photoPreview"),
            Is.SameAs(photoPreview)
        );

        var blurMaterialSource = GetPrivateField<Material>(focusPresentation, "blurMaterialSource");
        Assert.That(blurMaterialSource, Is.Not.Null);
        Assert.That(blurMaterialSource.shader.name, Is.EqualTo("Stage/PhotoPreviewBlur"));
    }

    [UnityTest]
    public IEnumerator PhotoPreviewIsBlurredRightAfterTheSceneLoads()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var photoPreview = GameObject.Find("PhotoPreview").GetComponent<RawImage>();

        Assert.That(photoPreview.material, Is.Not.EqualTo(photoPreview.defaultMaterial));
        Assert.That(photoPreview.material.GetFloat("_BlurStrength"), Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator PhotoPreviewReturnsToTheDefaultMaterialOncePlayingStarts()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var stageController = GameObject.Find("GameController").GetComponent<Stage1Controller>();
        var focusPresentation = GameObject
            .Find("GameController")
            .GetComponent<StagePhotoFocusPresentation>();
        var photoPreview = GameObject.Find("PhotoPreview").GetComponent<RawImage>();

        SetPrivateField(focusPresentation, "blurClearDuration", 0.02f);
        SetPrivateField(focusPresentation, "postBlurWaitDuration", 0.02f);

        yield return WaitUntilOrTimeout(
            () => stageController.CurrentState == Stage1Controller.Stage1State.Playing,
            "Stage did not reach Playing."
        );

        Assert.That(photoPreview.material, Is.EqualTo(photoPreview.defaultMaterial));
    }

    [Test]
    public void SceneKeepsThePhotoPreviewMaterialUnassignedBeforePlay()
    {
        var scenePath = Path.Combine(
            Application.dataPath,
            "_Project",
            "Scenes",
            "Game_Stage1.unity"
        );
        var sceneText = File.ReadAllText(scenePath);

        // PhotoPreview.renderTexture への参照はシーン内でPhotoPreviewのRawImageだけが持つため、
        // このテクスチャ参照を目印にRawImageの直列化ブロックを特定する。
        var textureReferenceIndex = sceneText.IndexOf(
            "m_Texture: {fileID: 8400000, guid: be2d6a1b702444a48ac1b644f324169f",
            StringComparison.Ordinal
        );
        Assert.That(
            textureReferenceIndex,
            Is.GreaterThan(-1),
            "PhotoPreviewのRawImageによるRenderTexture参照がシーン資産内に見つかりませんでした。"
        );

        var blockStart = sceneText.LastIndexOf(
            "--- !u!114",
            textureReferenceIndex,
            StringComparison.Ordinal
        );
        Assert.That(blockStart, Is.GreaterThan(-1));

        var block = sceneText.Substring(blockStart, textureReferenceIndex - blockStart);

        Assert.That(
            block,
            Does.Contain("m_Material: {fileID: 0}"),
            "PhotoPreviewのRawImageにマテリアルがシーン資産上で割り当てられています。"
                + "実行時にのみ割り当てる設計が壊れています。"
        );
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

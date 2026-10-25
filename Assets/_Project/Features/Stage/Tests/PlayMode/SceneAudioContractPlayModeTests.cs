using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using ResultScene.BuzzReaction;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SceneAudioContractPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        ResultDataTransporter.CurrentData = null;

        if (SoundManager.Instance != null)
        {
            UnityEngine.Object.Destroy(SoundManager.Instance.gameObject);
            yield return null;
        }

        var cleanupScene = SceneManager.CreateScene(
            $"{nameof(SceneAudioContractPlayModeTests)}.Cleanup.{Guid.NewGuid():N}"
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
    }

    [UnityTest]
    public IEnumerator TitleUsesLoopingTitleBgmAndConfiguredHowToPlaySe()
    {
        yield return LoadScene("Title");

        AssertBgmConfiguration(expectedIndex: 0, expectedLoop: true);

        var howToPlayControllers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        var configuredControllerCount = 0;

        foreach (var controller in howToPlayControllers)
        {
            if (controller.GetType().Name != "HowToPlayController")
            {
                continue;
            }

            configuredControllerCount++;
            Assert.That(GetPrivateField<bool>(controller, "_playSE"), Is.True);
            AssertSeIndexIsAssigned(
                GetPrivateField<int>(controller, "_seOpenIndex"),
                expectedIndex: 14
            );
            AssertSeIndexIsAssigned(
                GetPrivateField<int>(controller, "_seCloseIndex"),
                expectedIndex: 14
            );
            Assert.That(
                GetPrivateField<float>(controller, "_seVolumeScale"),
                Is.EqualTo(0.3f).Within(0.0001f)
            );
        }

        Assert.That(configuredControllerCount, Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator GameStage1UsesLoopingBgmAndAssignedGameplaySe()
    {
        yield return LoadScene("Game_Stage1");

        AssertBgmConfiguration(expectedIndex: 5, expectedLoop: true);

        var sceneBgmManager = UnityEngine.Object.FindAnyObjectByType<SceneSoundManager>();
        var sceneSeManager = UnityEngine.Object.FindAnyObjectByType<SceneSEManager>();
        var stageController = UnityEngine.Object.FindAnyObjectByType<Stage1Controller>();
        var capturePresentation =
            UnityEngine.Object.FindAnyObjectByType<StagePhotoCapturePresentation>();
        var randomDefocusController =
            UnityEngine.Object.FindAnyObjectByType<StageRandomDefocusController>();

        Assert.That(sceneBgmManager, Is.Not.Null);
        Assert.That(sceneSeManager, Is.Not.Null);
        Assert.That(stageController, Is.Not.Null);
        Assert.That(capturePresentation, Is.Not.Null);
        Assert.That(randomDefocusController, Is.Not.Null);
        Assert.That(GetPrivateField<int>(sceneBgmManager, "_bgmIndex"), Is.EqualTo(5));
        Assert.That(GetPrivateField<bool>(sceneBgmManager, "_isLoop"), Is.True);
        AssertSeIndexIsAssigned(GetPrivateField<int>(sceneSeManager, "_seIndex"), 17);
        Assert.That(GetPrivateField<float>(sceneSeManager, "_pitch"), Is.EqualTo(1f));
        Assert.That(
            GetPrivateField<float>(sceneSeManager, "_volumeScale"),
            Is.EqualTo(0.4f).Within(0.0001f)
        );
        Assert.That(GetPrivateField<float>(sceneSeManager, "_delay"), Is.EqualTo(0f));
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(stageController, "_gameOverSeIndex"),
            expectedIndex: 3
        );
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(capturePresentation, "shutterSeIndex"),
            expectedIndex: 5
        );
        Assert.That(
            GetPrivateField<MonoBehaviour>(randomDefocusController, "focusReleaseSoundTrigger"),
            Is.SameAs(sceneSeManager)
        );
    }

    [UnityTest]
    public IEnumerator ResultSceneUsesOneShotBgmAndAssignedResultSe()
    {
        ResultDataTransporter.CurrentData = new ResultData
        {
            PlayerName = "AudioContractTest",
            LocationName = "TestLocation",
            Bonuses = new List<BonusInputData>(),
        };

        yield return LoadScene("ResultScene");

        AssertBgmConfiguration(expectedIndex: 2, expectedLoop: false);

        var sceneBgmManager = UnityEngine.Object.FindAnyObjectByType<SceneSoundManager>();
        var resultManager = UnityEngine.Object.FindAnyObjectByType<ResultSceneManager>();
        var buzzReactionManager = UnityEngine.Object.FindAnyObjectByType<BuzzReactionManager>();

        Assert.That(sceneBgmManager, Is.Not.Null);
        Assert.That(resultManager, Is.Not.Null);
        Assert.That(buzzReactionManager, Is.Not.Null);
        Assert.That(GetPrivateField<int>(sceneBgmManager, "_bgmIndex"), Is.EqualTo(2));
        Assert.That(GetPrivateField<bool>(sceneBgmManager, "_isLoop"), Is.False);
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(resultManager, "_sePopIndex"),
            expectedIndex: 15
        );
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(resultManager, "_seSlideUpIndex"),
            expectedIndex: 16
        );
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(resultManager, "_seStampIndex"),
            expectedIndex: 16
        );
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(resultManager, "_seScoreCountIndex"),
            expectedIndex: 16
        );
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(resultManager, "_zeroScoreSeIndex"),
            expectedIndex: 3
        );
        AssertSeIndexIsAssigned(
            GetPrivateField<int>(buzzReactionManager, "_countUpSeIndex"),
            expectedIndex: 8
        );
    }

    private static IEnumerator LoadScene(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return null;
        yield return null;
    }

    private static void AssertBgmConfiguration(int expectedIndex, bool expectedLoop)
    {
        var soundManager = SoundManager.Instance;
        Assert.That(soundManager, Is.Not.Null);

        var audioDataList = GetPrivateField<AudioDataList>(soundManager, "_audioDataList");
        var bgmAudioSource = GetPrivateField<AudioSource>(soundManager, "_bgmAudioSource");
        var expectedItem = audioDataList.bgmList[expectedIndex];

        Assert.That(expectedItem.clip, Is.Not.Null);
        Assert.That(bgmAudioSource.clip, Is.SameAs(expectedItem.clip));
        Assert.That(bgmAudioSource.loop, Is.EqualTo(expectedLoop));
    }

    private static void AssertSeIndexIsAssigned(int index, int expectedIndex)
    {
        var soundManager = SoundManager.Instance;
        Assert.That(soundManager, Is.Not.Null);

        var audioDataList = GetPrivateField<AudioDataList>(soundManager, "_audioDataList");
        Assert.That(index, Is.EqualTo(expectedIndex));
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(index, Is.LessThan(audioDataList.seList.Count));
        Assert.That(audioDataList.seList[index].clip, Is.Not.Null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(target);
    }
}

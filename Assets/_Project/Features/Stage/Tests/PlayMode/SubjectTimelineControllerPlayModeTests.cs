using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SubjectTimelineControllerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly ArrayList createdObjects = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayingSpawnsDogWithConfiguredPositionScaleSpeedAndDirection()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
        var dog = spawnRoot.GetChild(0);
        Assert.That(dog.localPosition, Is.EqualTo(new Vector3(-10f, 2f, 0f)));
        Assert.That(dog.localScale, Is.EqualTo(Vector3.one * 1.5f));

        var initialPositionX = dog.position.x;
        yield return null;

        Assert.That(dog.position.x, Is.GreaterThan(initialPositionX));
        Assert.That(timeline, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator ReenteringPlayingResetsTimelineAndReplacesPreviousDog()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        var firstDog = spawnRoot.GetChild(0).gameObject;
        SetPrivateField(stageController, "currentState", Stage0Controller.Stage0State.StartMessage);
        yield return null;
        SetPrivateField(stageController, "currentState", Stage0Controller.Stage0State.Playing);
        yield return null;
        yield return null;

        Assert.That(firstDog, Is.Null);
        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
    }

    private Stage0Controller CreateStageController(Stage0Controller.Stage0State state)
    {
        var stageController = CreateGameObject("Stage0Controller").AddComponent<Stage0Controller>();
        stageController.enabled = false;
        SetPrivateField(stageController, "currentState", state);
        return stageController;
    }

    private SubjectTimelineController CreateTimeline(
        Stage0Controller stageController,
        Transform spawnRoot,
        GameObject dogPrefab,
        float spawnTimeSeconds
    )
    {
        var timeline = CreateGameObject("SubjectTimeline")
            .AddComponent<SubjectTimelineController>();
        SetPrivateField(timeline, "stageController", stageController);
        SetPrivateField(timeline, "subjectSpawnRoot", spawnRoot);
        SetPrivateField(
            timeline,
            "spawnSettings",
            CreateSpawnSettings(dogPrefab, spawnTimeSeconds)
        );
        return timeline;
    }

    private GameObject CreateDogPrefab()
    {
        var dogPrefab = CreateGameObject("Dog");
        dogPrefab.AddComponent<SubjectMover>();
        return dogPrefab;
    }

    private Array CreateSpawnSettings(GameObject dogPrefab, float spawnTimeSeconds)
    {
        var settingType = typeof(SubjectTimelineController).GetNestedType(
            "SubjectSpawnSetting",
            PrivateInstance
        );
        Assert.That(settingType, Is.Not.Null);

        var setting = Activator.CreateInstance(settingType);
        SetPrivateField(setting, "subjectPrefab", dogPrefab);
        SetPrivateField(setting, "spawnTimeSeconds", spawnTimeSeconds);
        SetPrivateField(setting, "spawnPosition", new Vector2(-10f, 2f));
        SetPrivateField(setting, "moveDirection", SubjectMoveDirection.LeftToRight);
        SetPrivateField(setting, "moveSpeed", 2f);
        SetPrivateField(setting, "scale", 1.5f);

        var settings = Array.CreateInstance(settingType, 1);
        settings.SetValue(setting, 0);
        return settings;
    }

    private GameObject CreateGameObject(string name)
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }
}

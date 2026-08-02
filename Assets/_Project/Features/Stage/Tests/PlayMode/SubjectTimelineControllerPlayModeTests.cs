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
    public IEnumerator PlayingSpawnsDogUnderConfiguredRootWithConfiguredScale()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
        var dog = spawnRoot.GetChild(0);
        Assert.That(dog.localScale, Is.EqualTo(Vector3.one * 1.5f));
        Assert.That(dog.parent, Is.EqualTo(spawnRoot));
        Assert.That(timeline, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator PlayingMovesSpawnedDogToTheRight()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
        var dog = spawnRoot.GetChild(0);
        var initialPositionX = dog.position.x;

        yield return null;

        Assert.That(dog.position.x, Is.GreaterThan(initialPositionX));
    }

    [UnityTest]
    public IEnumerator PlayingConfiguresVerticalSwayForSpawnedSubject()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            0f,
            verticalSwayAmplitude: 0.35f,
            verticalSwayFrequencyHz: 1.2f
        );

        yield return null;

        var mover = spawnRoot.GetChild(0).GetComponent<SubjectMover>();
        Assert.That(GetPrivateField<float>(mover, "verticalSwayAmplitude"), Is.EqualTo(0.35f));
        Assert.That(GetPrivateField<float>(mover, "verticalSwayFrequencyHz"), Is.EqualTo(1.2f));
    }

    [UnityTest]
    public IEnumerator PlayingPlacesConfiguredPathAnchorAtSpawnPosition()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreatePathAnchoredDogPrefab();
        CreateTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            0f,
            usePathAnchorForSpawnPosition: true
        );

        yield return null;

        var spawnedDog = spawnRoot.GetChild(0).GetComponent<StageSubject>();
        Assert.That(spawnedDog.PathAnchor.position.y, Is.EqualTo(2f));
    }

    [UnityTest]
    public IEnumerator PlayingSpawnsEveryGeneratedRandomEntryOnlyOnce()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateRandomTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            minimumSpawnCount: 3,
            maximumSpawnCount: 3,
            earliestSpawnTimeSeconds: 0f,
            latestSpawnTimeSeconds: 0f
        );

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(3));

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator ReenteringPlayingBuildsANewRandomSchedule()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        var timeline = CreateRandomTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            minimumSpawnCount: 1,
            maximumSpawnCount: 1,
            earliestSpawnTimeSeconds: 5f,
            latestSpawnTimeSeconds: 6f
        );
        SetPrivateField(timeline, "spawnRandom", new System.Random(0));

        yield return null;

        var firstSpawnTimeSeconds = GetFirstScheduledSpawnTime(timeline);
        SetPrivateField(stageController, "currentState", Stage0Controller.Stage0State.StartMessage);
        yield return null;
        SetPrivateField(stageController, "currentState", Stage0Controller.Stage0State.Playing);
        yield return null;

        var secondSpawnTimeSeconds = GetFirstScheduledSpawnTime(timeline);

        Assert.That(secondSpawnTimeSeconds, Is.Not.EqualTo(firstSpawnTimeSeconds));
        Assert.That(spawnRoot.childCount, Is.EqualTo(0));
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

        Assert.That(firstDog == null, Is.True);
        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator GameOverStopsSpawnedDog()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        var dog = spawnRoot.GetChild(0);
        stageController.EnterGameOver();
        yield return null;
        var stoppedPositionX = dog.position.x;

        yield return null;

        Assert.That(dog.position.x, Is.EqualTo(stoppedPositionX));
    }

    [UnityTest]
    public IEnumerator CapturedWaitingForTimeoutKeepsDogMovingUntilCompletedStopsIt()
    {
        var stageController = CreateStageController(Stage0Controller.Stage0State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        var dog = spawnRoot.GetChild(0);
        stageController.BeginCapturedWaitingForTimeout();
        var capturedWaitingPositionX = dog.position.x;
        yield return null;

        Assert.That(dog.position.x, Is.GreaterThan(capturedWaitingPositionX));

        SetPrivateField(stageController, "remainingTime", 0f);
        InvokePrivateMethod(stageController, "Update");
        yield return null;
        var completedPositionX = dog.position.x;

        yield return null;

        Assert.That(
            stageController.CurrentState,
            Is.EqualTo(Stage0Controller.Stage0State.Completed)
        );
        Assert.That(dog.position.x, Is.EqualTo(completedPositionX));
    }

    private Stage0Controller CreateStageController(Stage0Controller.Stage0State state)
    {
        var stageController = CreateGameObject("Stage0Controller").AddComponent<Stage0Controller>();
        stageController.enabled = false;

        var gameOverPanel = CreateGameObject("GameOverPanel");
        gameOverPanel.SetActive(false);

        var gameOverFadeObject = CreateGameObject("GameOverFade");
        var gameOverFade = gameOverFadeObject.AddComponent<CanvasGroup>();
        gameOverFade.alpha = 0f;

        var gameOverContent = CreateGameObject("GameOverContent");
        gameOverContent.SetActive(false);

        SetPrivateField(stageController, "gameOverPanel", gameOverPanel);
        SetPrivateField(stageController, "gameOverFade", gameOverFade);
        SetPrivateField(stageController, "gameOverContent", gameOverContent);
        SetPrivateField(stageController, "currentState", state);
        return stageController;
    }

    private SubjectTimelineController CreateTimeline(
        Stage0Controller stageController,
        Transform spawnRoot,
        GameObject dogPrefab,
        float spawnTimeSeconds,
        bool usePathAnchorForSpawnPosition = false,
        float verticalSwayAmplitude = 0f,
        float verticalSwayFrequencyHz = 0f
    )
    {
        var timeline = CreateGameObject("SubjectTimeline")
            .AddComponent<SubjectTimelineController>();
        SetPrivateField(timeline, "stageController", stageController);
        SetPrivateField(timeline, "subjectSpawnRoot", spawnRoot);
        SetPrivateField(
            timeline,
            "spawnSettings",
            CreateSpawnSettings(
                dogPrefab,
                spawnTimeSeconds,
                usePathAnchorForSpawnPosition,
                verticalSwayAmplitude,
                verticalSwayFrequencyHz
            )
        );
        return timeline;
    }

    private SubjectTimelineController CreateRandomTimeline(
        Stage0Controller stageController,
        Transform spawnRoot,
        GameObject subjectPrefab,
        int minimumSpawnCount,
        int maximumSpawnCount,
        float earliestSpawnTimeSeconds,
        float latestSpawnTimeSeconds
    )
    {
        var timeline = CreateGameObject("SubjectTimeline")
            .AddComponent<SubjectTimelineController>();
        SetPrivateField(timeline, "stageController", stageController);
        SetPrivateField(timeline, "subjectSpawnRoot", spawnRoot);
        SetPrivateField(
            timeline,
            "spawnSettings",
            CreateRandomSpawnSettings(
                subjectPrefab,
                minimumSpawnCount,
                maximumSpawnCount,
                earliestSpawnTimeSeconds,
                latestSpawnTimeSeconds
            )
        );
        return timeline;
    }

    private Array CreateRandomSpawnSettings(
        GameObject subjectPrefab,
        int minimumSpawnCount,
        int maximumSpawnCount,
        float earliestSpawnTimeSeconds,
        float latestSpawnTimeSeconds
    )
    {
        var settingType = typeof(SubjectTimelineController).GetNestedType(
            "SubjectSpawnSetting",
            PrivateInstance
        );
        var routeType = typeof(SubjectTimelineController).GetNestedType(
            "SubjectSpawnRoute",
            PrivateInstance
        );
        var spawnModeType = typeof(SubjectTimelineController).GetNestedType(
            "SubjectSpawnMode",
            PrivateInstance
        );
        Assert.That(settingType, Is.Not.Null);
        Assert.That(routeType, Is.Not.Null);
        Assert.That(spawnModeType, Is.Not.Null);

        var route = Activator.CreateInstance(routeType);
        SetPrivateField(route, "subjectPrefab", subjectPrefab);
        SetPrivateField(route, "spawnPosition", new Vector2(-10f, 2f));
        SetPrivateField(route, "moveDirection", SubjectMoveDirection.LeftToRight);
        SetPrivateField(route, "moveSpeed", 2f);
        SetPrivateField(route, "scale", 1f);
        SetPrivateField(route, "selectionWeight", 1f);

        var routes = Array.CreateInstance(routeType, 1);
        routes.SetValue(route, 0);

        var setting = Activator.CreateInstance(settingType);
        SetPrivateField(setting, "spawnMode", Enum.ToObject(spawnModeType, 1));
        SetPrivateField(setting, "appearanceProbability", 1f);
        SetPrivateField(setting, "minimumSpawnCount", minimumSpawnCount);
        SetPrivateField(setting, "maximumSpawnCount", maximumSpawnCount);
        SetPrivateField(setting, "earliestSpawnTimeSeconds", earliestSpawnTimeSeconds);
        SetPrivateField(setting, "latestSpawnTimeSeconds", latestSpawnTimeSeconds);
        SetPrivateField(setting, "minimumSpawnIntervalSeconds", 0f);
        SetPrivateField(setting, "randomRoutes", routes);

        var settings = Array.CreateInstance(settingType, 1);
        settings.SetValue(setting, 0);
        return settings;
    }

    private static float GetFirstScheduledSpawnTime(SubjectTimelineController timeline)
    {
        var scheduledSpawns = GetPrivateField<System.Collections.IList>(
            timeline,
            "scheduledSpawns"
        );
        Assert.That(scheduledSpawns, Has.Count.EqualTo(1));

        var spawnTimeProperty = scheduledSpawns[0]
            .GetType()
            .GetProperty("SpawnTimeSeconds", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(spawnTimeProperty, Is.Not.Null);
        return (float)spawnTimeProperty.GetValue(scheduledSpawns[0]);
    }

    private GameObject CreateDogPrefab()
    {
        var dogPrefab = CreateGameObject("Dog");
        dogPrefab.AddComponent<SubjectMover>();
        return dogPrefab;
    }

    private GameObject CreatePathAnchoredDogPrefab()
    {
        var dogPrefab = CreateDogPrefab();
        var pathAnchor = CreateGameObject("FootPoint").transform;
        pathAnchor.SetParent(dogPrefab.transform);
        pathAnchor.localPosition = new Vector3(0f, -1f, 0f);

        var stageSubject = dogPrefab.AddComponent<StageSubject>();
        SetPrivateField(stageSubject, "pathAnchor", pathAnchor);
        return dogPrefab;
    }

    private Array CreateSpawnSettings(
        GameObject dogPrefab,
        float spawnTimeSeconds,
        bool usePathAnchorForSpawnPosition = false,
        float verticalSwayAmplitude = 0f,
        float verticalSwayFrequencyHz = 0f
    )
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
        SetPrivateField(setting, "usePathAnchorForSpawnPosition", usePathAnchorForSpawnPosition);
        SetPrivateField(setting, "verticalSwayAmplitude", verticalSwayAmplitude);
        SetPrivateField(setting, "verticalSwayFrequencyHz", verticalSwayFrequencyHz);

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

    private static void InvokePrivateMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, PrivateInstance);
        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
        method.Invoke(target, null);
    }
}

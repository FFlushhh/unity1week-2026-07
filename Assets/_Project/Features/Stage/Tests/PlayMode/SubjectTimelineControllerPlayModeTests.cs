using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
    public IEnumerator PlayingMirrorsFixedSpawnPositionDirectionAndSpriteWhenOppositeSideIsSelected()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateFacingDogPrefab();
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 5f);
        SetPrivateField(timeline, "oppositeSideProbability", 1f);

        yield return null;

        Assert.That(GetFirstScheduledSpawnHorizontalMirror(timeline), Is.True);
        SetPrivateField(timeline, "elapsedTimeSeconds", 5f);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        var spawnedDog = spawnRoot.GetChild(0);
        var mover = spawnedDog.GetComponent<SubjectMover>();
        var stageSubject = spawnedDog.GetComponent<StageSubject>();
        Assert.That(spawnedDog.localPosition.x, Is.EqualTo(10f));
        Assert.That(
            GetPrivateField<SubjectMoveDirection>(mover, "moveDirection"),
            Is.EqualTo(SubjectMoveDirection.RightToLeft)
        );
        Assert.That(stageSubject.SubjectRenderer.flipX, Is.True);
    }

    [UnityTest]
    public IEnumerator PlayingMirrorsJudgementPointAndColliderWhenOppositeSideIsSelected()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var judgementPointLocalPosition = new Vector3(0.6f, 0.4f, 0f);
        var colliderPoints = new[]
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(0.5f, 1f),
        };
        var dogPrefab = CreateFacingDogPrefabWithColliderAndJudgementPoint(
            judgementPointLocalPosition,
            colliderPoints
        );
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 5f);
        SetPrivateField(timeline, "oppositeSideProbability", 1f);

        yield return null;

        SetPrivateField(timeline, "elapsedTimeSeconds", 5f);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        var spawnedDog = spawnRoot.GetChild(0);
        var stageSubject = spawnedDog.GetComponent<StageSubject>();
        var polygonCollider = spawnedDog.GetComponent<PolygonCollider2D>();

        Assert.That(stageSubject.SubjectRenderer.flipX, Is.True);
        Assert.That(
            stageSubject.JudgementPoint.localPosition.x,
            Is.EqualTo(-judgementPointLocalPosition.x).Within(0.0001f)
        );
        Assert.That(
            stageSubject.JudgementPoint.localPosition.y,
            Is.EqualTo(judgementPointLocalPosition.y).Within(0.0001f)
        );

        var mirroredPath = polygonCollider.GetPath(0);
        for (var index = 0; index < colliderPoints.Length; index++)
        {
            Assert.That(
                mirroredPath[index].x,
                Is.EqualTo(-colliderPoints[index].x).Within(0.0001f)
            );
            Assert.That(mirroredPath[index].y, Is.EqualTo(colliderPoints[index].y).Within(0.0001f));
        }
    }

    [UnityTest]
    public IEnumerator PlayingDoesNotMirrorJudgementPointOrColliderWhenSameSideIsSelected()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var judgementPointLocalPosition = new Vector3(0.6f, 0.4f, 0f);
        var colliderPoints = new[]
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(0.5f, 1f),
        };
        var dogPrefab = CreateFacingDogPrefabWithColliderAndJudgementPoint(
            judgementPointLocalPosition,
            colliderPoints
        );
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 5f);
        SetPrivateField(timeline, "oppositeSideProbability", 0f);

        yield return null;

        SetPrivateField(timeline, "elapsedTimeSeconds", 5f);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        var spawnedDog = spawnRoot.GetChild(0);
        var stageSubject = spawnedDog.GetComponent<StageSubject>();
        var polygonCollider = spawnedDog.GetComponent<PolygonCollider2D>();

        Assert.That(stageSubject.SubjectRenderer.flipX, Is.False);
        Assert.That(
            stageSubject.JudgementPoint.localPosition,
            Is.EqualTo(judgementPointLocalPosition)
        );

        var unmirroredPath = polygonCollider.GetPath(0);
        for (var index = 0; index < colliderPoints.Length; index++)
        {
            Assert.That(unmirroredPath[index], Is.EqualTo(colliderPoints[index]));
        }
    }

    [UnityTest]
    public IEnumerator RandomSpawnKeepsItsOppositeSideSelectionInTheGeneratedSchedule()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateFacingDogPrefab();
        var timeline = CreateRandomTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            minimumSpawnCount: 1,
            maximumSpawnCount: 1,
            earliestSpawnTimeSeconds: 5f,
            latestSpawnTimeSeconds: 5f
        );
        SetPrivateField(timeline, "oppositeSideProbability", 1f);

        yield return null;

        Assert.That(GetFirstScheduledSpawnHorizontalMirror(timeline), Is.True);
    }

    [UnityTest]
    public IEnumerator PlayingAssignsUniqueSortingOrdersToGeneratedSubjects()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateFacingDogPrefab();
        dogPrefab.GetComponent<StageSubject>().SubjectRenderer.sortingOrder = 10;
        var timeline = CreateRandomTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            minimumSpawnCount: 2,
            maximumSpawnCount: 2,
            earliestSpawnTimeSeconds: 0f,
            latestSpawnTimeSeconds: 0f
        );
        timeline.enabled = false;

        InvokePrivateMethod(timeline, "BuildSpawnSchedule", true);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        Assert.That(spawnRoot.childCount, Is.EqualTo(2));
        var firstOrder = spawnRoot
            .GetChild(0)
            .GetComponent<StageSubject>()
            .SubjectRenderer.sortingOrder;
        var secondOrder = spawnRoot
            .GetChild(1)
            .GetComponent<StageSubject>()
            .SubjectRenderer.sortingOrder;
        Assert.That(firstOrder, Is.EqualTo(10));
        Assert.That(secondOrder, Is.EqualTo(11));
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartMessageSpawnsUniqueInitialSubjectsInsideTheConfiguredHorizontalRange()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.StartMessage);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateSubjectPrefab("Dog", SubjectId.Dog);
        var dirtyClothesPersonPrefab = CreateSubjectPrefab(
            "DirtyClothesPerson",
            SubjectId.DirtyClothesPerson
        );
        var birdPrefab = CreateSubjectPrefab("Bird", SubjectId.Bird);
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 10f);
        SetPrivateField(
            timeline,
            "spawnSettings",
            CreateFixedSpawnSettings(dogPrefab, dirtyClothesPersonPrefab, birdPrefab)
        );
        SetPrivateField(timeline, "initialSpawnMinimumCount", 2);
        SetPrivateField(timeline, "initialSpawnMaximumCount", 2);
        SetPrivateField(timeline, "initialSpawnXRange", new Vector2(-5f, 5f));
        SetPrivateField(timeline, "spawnRandom", new System.Random(0));

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(2));
        var initialSubjectIds = new HashSet<SubjectId>();
        foreach (Transform initialSubject in spawnRoot)
        {
            Assert.That(initialSubject.localPosition.x, Is.InRange(-5f, 5f));
            Assert.That(
                initialSubjectIds.Add(initialSubject.GetComponent<StageSubject>().Id),
                Is.True
            );
        }
    }

    [UnityTest]
    public IEnumerator FirstPlayingEntryKeepsTheInitialSubjectsMoving()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.StartMessage);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateSubjectPrefab("Dog", SubjectId.Dog);
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 10f);
        SetPrivateField(timeline, "initialSpawnMinimumCount", 1);
        SetPrivateField(timeline, "initialSpawnMaximumCount", 1);
        SetPrivateField(timeline, "initialSpawnXRange", Vector2.zero);

        yield return null;

        var initialDog = spawnRoot.GetChild(0).gameObject;
        var positionBeforePlaying = initialDog.transform.position.x;
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;

        Assert.That(initialDog, Is.Not.Null);
        Assert.That(initialDog.transform.position.x, Is.GreaterThan(positionBeforePlaying));
        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator StartMessageSpawnsScheduledSubjectsAndKeepsThemWhenPlayingStarts()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.StartMessage);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        var timeline = CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);
        SetPrivateField(timeline, "initialSpawnMinimumCount", 0);
        SetPrivateField(timeline, "initialSpawnMaximumCount", 0);

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
        var scheduledDog = spawnRoot.GetChild(0).gameObject;

        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
        Assert.That(spawnRoot.GetChild(0).gameObject, Is.EqualTo(scheduledDog));
    }

    [UnityTest]
    [Ignore("Inspectorの調整を許可するため無効化")]
    public IEnumerator GameStage1StartsWithUniqueInitialSubjectsThatContinueMovingIntoPlaying()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var stageController = GameObject.Find("GameController").GetComponent<Stage1Controller>();
        var spawnRoot = GetPrivateField<Transform>(timeline, "subjectSpawnRoot");

        Assert.That(
            stageController.CurrentState,
            Is.EqualTo(Stage1Controller.Stage1State.StartMessage)
        );
        Assert.That(spawnRoot.childCount, Is.InRange(2, 3));

        var initialSubjectIds = new HashSet<SubjectId>();
        var initialPositions = new Dictionary<Transform, Vector3>();
        foreach (Transform initialSubject in spawnRoot)
        {
            var stageSubject = initialSubject.GetComponent<StageSubject>();
            Assert.That(stageSubject, Is.Not.Null);
            Assert.That(initialSubjectIds.Add(stageSubject.Id), Is.True);
            Assert.That(initialSubject.position.x, Is.InRange(-5f, 5f));
            initialPositions.Add(initialSubject, initialSubject.position);
        }

        yield return null;

        var hasMovedInitialSubject = false;
        foreach (var initialPosition in initialPositions)
        {
            if (initialPosition.Key.position != initialPosition.Value)
            {
                hasMovedInitialSubject = true;
                break;
            }
        }

        Assert.That(hasMovedInitialSubject, Is.True);

        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(initialPositions.Count));
        foreach (var initialPosition in initialPositions)
        {
            Assert.That(initialPosition.Key, Is.Not.Null);
        }
    }

    [UnityTest]
    public IEnumerator PlayingStartsAnotherRandomBatchAfterCurrentBatchIsExhausted()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        var timeline = CreateRandomTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            minimumSpawnCount: 3,
            maximumSpawnCount: 3,
            earliestSpawnTimeSeconds: 0f,
            latestSpawnTimeSeconds: 0f
        );
        timeline.enabled = false;

        InvokePrivateMethod(timeline, "BuildSpawnSchedule", true);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        Assert.That(spawnRoot.childCount, Is.EqualTo(3));

        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        Assert.That(spawnRoot.childCount, Is.EqualTo(6));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReenteringPlayingBuildsANewRandomSchedule()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.StartMessage);
        yield return null;
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;

        var secondSpawnTimeSeconds = GetFirstScheduledSpawnTime(timeline);

        Assert.That(secondSpawnTimeSeconds, Is.Not.EqualTo(firstSpawnTimeSeconds));
        Assert.That(spawnRoot.childCount, Is.EqualTo(0));
    }

    [UnityTest]
    [Ignore("Inspectorの調整を許可するため無効化")]
    public IEnumerator GameStage1InspectorSettingsMatchThePublishedRandomSpawnSpecification()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        var stageController = GameObject.Find("GameController").GetComponent<Stage1Controller>();
        var spawnSettings = GetPrivateField<Array>(timeline, "spawnSettings");

        Assert.That(timeline, Is.Not.Null);
        Assert.That(photoCamera, Is.Not.Null);
        Assert.That(stageController, Is.Not.Null);
        Assert.That(spawnSettings, Has.Length.EqualTo(6));
        Assert.That(
            GetPrivateField<float>(timeline, "oppositeSideProbability"),
            Is.EqualTo(0.5f).Within(0.01f)
        );
        Assert.That(GetPrivateField<int>(timeline, "initialSpawnMinimumCount"), Is.EqualTo(1));
        Assert.That(GetPrivateField<int>(timeline, "initialSpawnMaximumCount"), Is.EqualTo(4));
        Assert.That(
            GetPrivateField<Vector2>(timeline, "initialSpawnXRange"),
            Is.EqualTo(new Vector2(-5f, 5f))
        );

        var randomSubjectIds = new HashSet<SubjectId>();
        var foundFixedDog = false;
        var minimumSubjectCount = 1;
        var maximumSubjectCount = 1;
        foreach (var spawnSetting in spawnSettings)
        {
            if (!GetPublicProperty<bool>(spawnSetting, "IsRandom"))
            {
                var route = InvokePublicMethod(spawnSetting, "CreateFixedRoute");
                AssertRouteUsesPathAnchor(route, true);
                AssertRouteMatchesSpecification(
                    route,
                    SubjectId.Dog,
                    new Vector2(-9.5f, -4.4f),
                    SubjectMoveDirection.LeftToRight,
                    4f,
                    0f,
                    0f,
                    0.3f,
                    photoCamera,
                    GetPrivateField<float>(stageController, "playingDuration")
                );
                foundFixedDog = true;
                continue;
            }

            minimumSubjectCount += GetPrivateField<int>(spawnSetting, "minimumSpawnCount");
            maximumSubjectCount += GetPrivateField<int>(spawnSetting, "maximumSpawnCount");
            var randomRoutes = GetPrivateField<Array>(spawnSetting, "randomRoutes");
            Assert.That(randomRoutes, Is.Not.Empty);
            var subjectId = GetRouteSubjectId(randomRoutes.GetValue(0));
            Assert.That(randomSubjectIds.Add(subjectId), Is.True);

            switch (subjectId)
            {
                case SubjectId.DirtyClothesPerson:
                    AssertRandomSetting(spawnSetting, 1f, 1, 2, 0.5f, 3f, 0.25f);
                    AssertRouteUsesPathAnchor(randomRoutes.GetValue(0), true);
                    AssertRouteMatchesSpecification(
                        randomRoutes.GetValue(0),
                        subjectId,
                        new Vector2(-9.5f, -4.4f),
                        SubjectMoveDirection.LeftToRight,
                        3f,
                        0f,
                        0f,
                        3f,
                        photoCamera,
                        GetPrivateField<float>(stageController, "playingDuration")
                    );
                    break;
                case SubjectId.RabidDog:
                    AssertRandomSetting(spawnSetting, 1f, 1, 2, 0.7f, 3.8f, 0.25f);
                    AssertRouteUsesPathAnchor(randomRoutes.GetValue(0), true);
                    AssertRouteMatchesSpecification(
                        randomRoutes.GetValue(0),
                        subjectId,
                        new Vector2(9.5f, -4.4f),
                        SubjectMoveDirection.RightToLeft,
                        5.5f,
                        0f,
                        0f,
                        4.5f,
                        photoCamera,
                        GetPrivateField<float>(stageController, "playingDuration")
                    );
                    break;
                case SubjectId.PlasticBag:
                    AssertRandomSetting(spawnSetting, 1f, 2, 3, 0.4f, 4.8f, 0.25f);
                    Assert.That(randomRoutes, Has.Length.EqualTo(2));
                    AssertRouteUsesPathAnchor(randomRoutes.GetValue(0), false);
                    AssertRouteUsesPathAnchor(randomRoutes.GetValue(1), false);
                    AssertRouteMatchesSpecification(
                        randomRoutes.GetValue(0),
                        subjectId,
                        new Vector2(-9.5f, -2.5f),
                        SubjectMoveDirection.LeftToRight,
                        4f,
                        0.35f,
                        1.2f,
                        5.5f,
                        photoCamera,
                        GetPrivateField<float>(stageController, "playingDuration")
                    );
                    AssertRouteMatchesSpecification(
                        randomRoutes.GetValue(1),
                        subjectId,
                        new Vector2(9.5f, -1.6f),
                        SubjectMoveDirection.RightToLeft,
                        4.5f,
                        0.35f,
                        1.2f,
                        5.5f,
                        photoCamera,
                        GetPrivateField<float>(stageController, "playingDuration")
                    );
                    break;
                case SubjectId.Bird:
                    AssertRandomSetting(spawnSetting, 1f, 1, 2, 0.8f, 4.5f, 0.25f);
                    AssertRouteUsesPathAnchor(randomRoutes.GetValue(0), false);
                    AssertRouteMatchesSpecification(
                        randomRoutes.GetValue(0),
                        subjectId,
                        new Vector2(9.5f, 2f),
                        SubjectMoveDirection.RightToLeft,
                        4.5f,
                        0f,
                        0f,
                        5.8f,
                        photoCamera,
                        GetPrivateField<float>(stageController, "playingDuration")
                    );
                    break;
                case SubjectId.Sparrow:
                    AssertRandomSetting(spawnSetting, 1f, 2, 2, 1.2f, 5f, 0.25f);
                    AssertRouteUsesPathAnchor(randomRoutes.GetValue(0), false);
                    AssertRouteMatchesSpecification(
                        randomRoutes.GetValue(0),
                        subjectId,
                        new Vector2(-9.5f, 1f),
                        SubjectMoveDirection.LeftToRight,
                        5f,
                        0f,
                        0f,
                        6.5f,
                        photoCamera,
                        GetPrivateField<float>(stageController, "playingDuration")
                    );
                    break;
                default:
                    Assert.Fail($"Unexpected random subject: {subjectId}.");
                    break;
            }
        }

        Assert.That(foundFixedDog, Is.True);
        Assert.That(minimumSubjectCount, Is.EqualTo(8));
        Assert.That(maximumSubjectCount, Is.EqualTo(12));
        Assert.That(
            randomSubjectIds,
            Is.EquivalentTo(
                new[]
                {
                    SubjectId.DirtyClothesPerson,
                    SubjectId.RabidDog,
                    SubjectId.PlasticBag,
                    SubjectId.Bird,
                    SubjectId.Sparrow,
                }
            )
        );
    }

    [UnityTest]
    public IEnumerator GroundSubjectsPlaceFootPointsOnTheGroundLane()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var stageController = GameObject.Find("GameController").GetComponent<Stage1Controller>();
        var spawnRoot = GetPrivateField<Transform>(timeline, "subjectSpawnRoot");
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;

        SetPrivateField(timeline, "elapsedTimeSeconds", 10f);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");

        var groundedSubjectIds = new HashSet<SubjectId>();
        foreach (Transform subjectTransform in spawnRoot)
        {
            var stageSubject = subjectTransform.GetComponent<StageSubject>();
            if (
                stageSubject.Id != SubjectId.Dog
                && stageSubject.Id != SubjectId.DirtyClothesPerson
                && stageSubject.Id != SubjectId.RabidDog
            )
            {
                continue;
            }

            Assert.That(stageSubject.PathAnchor, Is.Not.Null);
            Assert.That(stageSubject.PathAnchor.position.y, Is.EqualTo(-4.4f).Within(0.001f));
            groundedSubjectIds.Add(stageSubject.Id);
        }

        Assert.That(
            groundedSubjectIds,
            Is.EquivalentTo(
                new[] { SubjectId.Dog, SubjectId.DirtyClothesPerson, SubjectId.RabidDog }
            )
        );
    }

    [UnityTest]
    [Ignore("Inspectorの調整を許可するため無効化")]
    public IEnumerator GameStage1ReenteringPlayingRebuildsAndResetsTheRandomSpawnSchedule()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);
        yield return null;

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var stageController = GameObject.Find("GameController").GetComponent<Stage1Controller>();
        var spawnRoot = GetPrivateField<Transform>(timeline, "subjectSpawnRoot");
        SetPrivateField(timeline, "spawnRandom", new System.Random(0));
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);

        yield return null;

        var firstSchedule = DescribeScheduledSpawns(timeline);
        Assert.That(firstSchedule, Is.Not.Empty);
        SetPrivateField(timeline, "elapsedTimeSeconds", 10f);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");
        var initialSubjectCount = spawnRoot.childCount - firstSchedule.Count;
        Assert.That(initialSubjectCount, Is.InRange(2, 3));

        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.StartMessage);
        yield return null;
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;
        yield return null;

        var secondSchedule = DescribeScheduledSpawns(timeline);
        Assert.That(spawnRoot.childCount, Is.EqualTo(0));
        CollectionAssert.AreNotEqual(firstSchedule, secondSchedule);
    }

    [UnityTest]
    public IEnumerator ReenteringPlayingResetsTimelineAndReplacesPreviousDog()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        CreateTimeline(stageController, spawnRoot, dogPrefab, 0f);

        yield return null;

        var firstDog = spawnRoot.GetChild(0).gameObject;
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.StartMessage);
        yield return null;
        SetPrivateField(stageController, "currentState", Stage1Controller.Stage1State.Playing);
        yield return null;
        yield return null;

        Assert.That(firstDog == null, Is.True);
        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator GameOverStopsSpawnedDog()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
    public IEnumerator GameOverPreventsNewRandomSpawnBatches()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
        var spawnRoot = CreateGameObject("SubjectSpawnRoot").transform;
        var dogPrefab = CreateDogPrefab();
        var timeline = CreateRandomTimeline(
            stageController,
            spawnRoot,
            dogPrefab,
            minimumSpawnCount: 1,
            maximumSpawnCount: 1,
            earliestSpawnTimeSeconds: 1f,
            latestSpawnTimeSeconds: 1f
        );

        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(0));
        SetPrivateField(timeline, "elapsedTimeSeconds", 1f);
        InvokePrivateMethod(timeline, "SpawnDueSubjects");
        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
        stageController.EnterGameOver();
        yield return null;
        yield return null;

        Assert.That(spawnRoot.childCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator CapturedWaitingForTimeoutKeepsDogMovingAfterCompletedUntilSceneExit()
    {
        var stageController = CreateStageController(Stage1Controller.Stage1State.Playing);
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
            Is.EqualTo(Stage1Controller.Stage1State.Completed)
        );
        Assert.That(dog.position.x, Is.GreaterThan(completedPositionX));
    }

    private static void AssertRandomSetting(
        object spawnSetting,
        float appearanceProbability,
        int minimumSpawnCount,
        int maximumSpawnCount,
        float earliestSpawnTimeSeconds,
        float latestSpawnTimeSeconds,
        float minimumSpawnIntervalSeconds
    )
    {
        Assert.That(
            GetPrivateField<float>(spawnSetting, "appearanceProbability"),
            Is.EqualTo(appearanceProbability)
        );
        Assert.That(
            GetPrivateField<int>(spawnSetting, "minimumSpawnCount"),
            Is.EqualTo(minimumSpawnCount)
        );
        Assert.That(
            GetPrivateField<int>(spawnSetting, "maximumSpawnCount"),
            Is.EqualTo(maximumSpawnCount)
        );
        Assert.That(
            GetPrivateField<float>(spawnSetting, "earliestSpawnTimeSeconds"),
            Is.EqualTo(earliestSpawnTimeSeconds)
        );
        Assert.That(
            GetPrivateField<float>(spawnSetting, "latestSpawnTimeSeconds"),
            Is.EqualTo(latestSpawnTimeSeconds)
        );
        Assert.That(
            GetPrivateField<float>(spawnSetting, "minimumSpawnIntervalSeconds"),
            Is.EqualTo(minimumSpawnIntervalSeconds)
        );
    }

    private static void AssertRouteUsesPathAnchor(object route, bool expectedUsePathAnchor)
    {
        Assert.That(
            GetPublicProperty<bool>(route, "UsePathAnchorForSpawnPosition"),
            Is.EqualTo(expectedUsePathAnchor)
        );
    }

    private static void AssertRouteMatchesSpecification(
        object route,
        SubjectId expectedSubjectId,
        Vector2 expectedSpawnPosition,
        SubjectMoveDirection expectedMoveDirection,
        float expectedMoveSpeed,
        float expectedVerticalSwayAmplitude,
        float expectedVerticalSwayFrequencyHz,
        float latestSpawnTimeSeconds,
        Camera photoCamera,
        float playingDuration
    )
    {
        var subjectPrefab = GetPublicProperty<GameObject>(route, "SubjectPrefab");
        var spawnPosition = GetPublicProperty<Vector2>(route, "SpawnPosition");
        var moveDirection = GetPublicProperty<SubjectMoveDirection>(route, "MoveDirection");
        var moveSpeed = GetPublicProperty<float>(route, "MoveSpeed");

        Assert.That(subjectPrefab, Is.Not.Null);
        Assert.That(GetRouteSubjectId(route), Is.EqualTo(expectedSubjectId));
        Assert.That(spawnPosition, Is.EqualTo(expectedSpawnPosition));
        Assert.That(moveDirection, Is.EqualTo(expectedMoveDirection));
        Assert.That(moveSpeed, Is.EqualTo(expectedMoveSpeed));
        Assert.That(
            GetPublicProperty<float>(route, "VerticalSwayAmplitude"),
            Is.EqualTo(expectedVerticalSwayAmplitude)
        );
        Assert.That(
            GetPublicProperty<float>(route, "VerticalSwayFrequencyHz"),
            Is.EqualTo(expectedVerticalSwayFrequencyHz)
        );
        Assert.That(GetPublicProperty<float>(route, "SelectionWeight"), Is.EqualTo(1f));

        var photoFrameHalfWidth = photoCamera.orthographicSize * photoCamera.aspect;
        Assert.That(Mathf.Abs(spawnPosition.x), Is.GreaterThan(photoFrameHalfWidth));
        Assert.That(
            spawnPosition.x < 0f,
            Is.EqualTo(moveDirection == SubjectMoveDirection.LeftToRight)
        );
        var timeToReachPhotoFrame = (Mathf.Abs(spawnPosition.x) - photoFrameHalfWidth) / moveSpeed;
        Assert.That(
            latestSpawnTimeSeconds + timeToReachPhotoFrame,
            Is.LessThanOrEqualTo(playingDuration)
        );
    }

    private static SubjectId GetRouteSubjectId(object route)
    {
        var subjectPrefab = GetPublicProperty<GameObject>(route, "SubjectPrefab");
        Assert.That(subjectPrefab, Is.Not.Null);
        var stageSubject = subjectPrefab.GetComponent<StageSubject>();
        Assert.That(stageSubject, Is.Not.Null);
        return stageSubject.Id;
    }

    private static List<string> DescribeScheduledSpawns(SubjectTimelineController timeline)
    {
        var scheduledSpawns = GetPrivateField<System.Collections.IList>(
            timeline,
            "scheduledSpawns"
        );
        var description = new List<string>(scheduledSpawns.Count);
        foreach (var scheduledSpawn in scheduledSpawns)
        {
            var scheduledSpawnType = scheduledSpawn.GetType();
            var spawnTimeSeconds = (float)
                scheduledSpawnType
                    .GetProperty("SpawnTimeSeconds", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(scheduledSpawn);
            var route = scheduledSpawnType
                .GetProperty("Route", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(scheduledSpawn);
            description.Add($"{GetRouteSubjectId(route)}:{spawnTimeSeconds:R}");
        }

        return description;
    }

    private static T GetPublicProperty<T>(object target, string propertyName)
    {
        var property = target
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, $"Property '{propertyName}' was not found.");
        return (T)property.GetValue(target);
    }

    private static object InvokePublicMethod(object target, string methodName)
    {
        var method = target
            .GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
        return method.Invoke(target, null);
    }

    private Stage1Controller CreateStageController(Stage1Controller.Stage1State state)
    {
        var stageController = CreateGameObject("Stage1Controller").AddComponent<Stage1Controller>();
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
        Stage1Controller stageController,
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
        SetPrivateField(timeline, "oppositeSideProbability", 0f);
        return timeline;
    }

    private SubjectTimelineController CreateRandomTimeline(
        Stage1Controller stageController,
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
        SetPrivateField(timeline, "oppositeSideProbability", 0f);
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

    private static bool GetFirstScheduledSpawnHorizontalMirror(SubjectTimelineController timeline)
    {
        var scheduledSpawns = GetPrivateField<System.Collections.IList>(
            timeline,
            "scheduledSpawns"
        );
        Assert.That(scheduledSpawns, Has.Count.EqualTo(1));

        var isHorizontallyMirroredProperty = scheduledSpawns[0]
            .GetType()
            .GetProperty("IsHorizontallyMirrored", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(isHorizontallyMirroredProperty, Is.Not.Null);
        return (bool)isHorizontallyMirroredProperty.GetValue(scheduledSpawns[0]);
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

    private GameObject CreateFacingDogPrefab()
    {
        var dogPrefab = CreateDogPrefab();
        var spriteRenderer = dogPrefab.AddComponent<SpriteRenderer>();
        var stageSubject = dogPrefab.AddComponent<StageSubject>();
        SetPrivateField(stageSubject, "subjectRenderer", spriteRenderer);
        return dogPrefab;
    }

    private GameObject CreateFacingDogPrefabWithColliderAndJudgementPoint(
        Vector3 judgementPointLocalPosition,
        Vector2[] colliderPoints
    )
    {
        var dogPrefab = CreateFacingDogPrefab();

        var judgementPoint = CreateGameObject("JudgementPoint").transform;
        judgementPoint.SetParent(dogPrefab.transform);
        judgementPoint.localPosition = judgementPointLocalPosition;
        SetPrivateField(dogPrefab.GetComponent<StageSubject>(), "judgementPoint", judgementPoint);

        var polygonCollider = dogPrefab.AddComponent<PolygonCollider2D>();
        polygonCollider.SetPath(0, colliderPoints);

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

    private GameObject CreateSubjectPrefab(string name, SubjectId subjectId)
    {
        var subjectPrefab = CreateGameObject(name);
        subjectPrefab.AddComponent<SubjectMover>();
        var stageSubject = subjectPrefab.AddComponent<StageSubject>();
        SetPrivateField(stageSubject, "subjectId", subjectId);
        return subjectPrefab;
    }

    private Array CreateFixedSpawnSettings(params GameObject[] subjectPrefabs)
    {
        var settingType = typeof(SubjectTimelineController).GetNestedType(
            "SubjectSpawnSetting",
            PrivateInstance
        );
        Assert.That(settingType, Is.Not.Null);

        var settings = Array.CreateInstance(settingType, subjectPrefabs.Length);
        for (var index = 0; index < subjectPrefabs.Length; index++)
        {
            var setting = Activator.CreateInstance(settingType);
            SetPrivateField(setting, "subjectPrefab", subjectPrefabs[index]);
            SetPrivateField(setting, "spawnTimeSeconds", 10f);
            SetPrivateField(setting, "spawnPosition", new Vector2(-10f, index));
            SetPrivateField(setting, "moveDirection", SubjectMoveDirection.LeftToRight);
            SetPrivateField(setting, "moveSpeed", 2f);
            SetPrivateField(setting, "scale", 1f);
            settings.SetValue(setting, index);
        }

        return settings;
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

    private static void InvokePrivateMethod(
        object target,
        string methodName,
        params object[] parameters
    )
    {
        var method = target.GetType().GetMethod(methodName, PrivateInstance);
        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
        method.Invoke(target, parameters);
    }
}

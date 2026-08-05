using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Game_Stage1へ固定配置した自撮り被写体の構成と、PhotoCamera内での見え方を検証します。
/// </summary>
public sealed class Stage1FixedSubjectPlacementPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int PhotoSubjectLayer = 6;
    private const int SelfieGirlSortingOrder = 100;
    private const int HighestMovingSubjectSortingOrder = 60;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var stageScene = SceneManager.GetSceneByName("Game_Stage1");
        if (stageScene.IsValid() && stageScene.isLoaded)
        {
            var emptyScene = SceneManager.CreateScene(
                $"{nameof(Stage1FixedSubjectPlacementPlayModeTests)}.Empty"
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
    public IEnumerator SelfieGirlExistsOnceUnderFixedSubjectRoot()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();

        Assert.That(selfieGirl.gameObject.layer, Is.EqualTo(PhotoSubjectLayer));
        Assert.That(selfieGirl.transform.parent, Is.Not.Null);
        Assert.That(selfieGirl.transform.parent.name, Is.EqualTo("FixedSubjectRoot"));
        Assert.That(selfieGirl.transform.parent.parent, Is.Not.Null);
        Assert.That(selfieGirl.transform.parent.parent.name, Is.EqualTo("StageRoot"));
    }

    [UnityTest]
    public IEnumerator SelfieGirlKeepsPrefabScoreAndReferences()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();

        Assert.That(selfieGirl.Score, Is.EqualTo(1000));
        Assert.That(selfieGirl.JudgementPoint, Is.Not.Null);
        Assert.That(selfieGirl.SubjectRenderer, Is.Not.Null);
        Assert.That(selfieGirl.SubjectRenderer.sprite, Is.Not.Null);
        Assert.That(selfieGirl.GetComponent<PolygonCollider2D>(), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator SelfieGirlColliderMatchesTheRenderedSpriteGeometry()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        var sprite = selfieGirl.SubjectRenderer.sprite;

        // Colliderと判断点は元画像3508x2480を100PPU換算した実寸で座標を決めている。
        // maxTextureSizeで縮小されてもpixelsPerUnitが追随して実寸は保たれるため、
        // ピクセル数ではなくワールド実寸と中心pivotを検証する。
        Assert.That(sprite.bounds.size.x, Is.EqualTo(35.08f).Within(0.1f));
        Assert.That(sprite.bounds.size.y, Is.EqualTo(24.80f).Within(0.1f));
        Assert.That(sprite.bounds.center.x, Is.EqualTo(0f).Within(0.01f));
        Assert.That(sprite.bounds.center.y, Is.EqualTo(0f).Within(0.01f));

        // 不透明部分の近似Colliderは、描画されるSpriteの範囲内へ収まる。
        // 腕が画像左端へ接しており境界が一致するため、わずかな誤差を許容する。
        const float boundsTolerance = 0.01f;
        var rendererBounds = selfieGirl.SubjectRenderer.bounds;
        var colliderBounds = selfieGirl.GetComponent<PolygonCollider2D>().bounds;
        Assert.That(
            colliderBounds.min.x,
            Is.GreaterThanOrEqualTo(rendererBounds.min.x - boundsTolerance)
        );
        Assert.That(
            colliderBounds.min.y,
            Is.GreaterThanOrEqualTo(rendererBounds.min.y - boundsTolerance)
        );
        Assert.That(
            colliderBounds.max.x,
            Is.LessThanOrEqualTo(rendererBounds.max.x + boundsTolerance)
        );
        Assert.That(
            colliderBounds.max.y,
            Is.LessThanOrEqualTo(rendererBounds.max.y + boundsTolerance)
        );
    }

    [UnityTest]
    public IEnumerator SelfieGirlDoesNotMoveAndIsNotScheduledBySubjectTimeline()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        Assert.That(selfieGirl.GetComponent<SubjectMover>(), Is.Null);
        Assert.That(selfieGirl.GetComponentInChildren<SubjectMover>(true), Is.Null);

        var timeline = Object.FindAnyObjectByType<SubjectTimelineController>();
        Assert.That(timeline, Is.Not.Null);

        foreach (var scheduledPrefab in CollectScheduledPrefabs(timeline))
        {
            if (!scheduledPrefab.TryGetComponent<StageSubject>(out var scheduledSubject))
            {
                continue;
            }

            Assert.That(
                scheduledSubject.Id,
                Is.Not.EqualTo(SubjectId.SelfieGirl),
                $"SelfieGirl must not be spawned by SubjectTimeline, but '{scheduledPrefab.name}' is scheduled."
            );
        }
    }

    [UnityTest]
    public IEnumerator SelfieGirlRendersInFrontOfEveryMovingSubject()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        Assert.That(selfieGirl.SubjectRenderer.sortingOrder, Is.EqualTo(SelfieGirlSortingOrder));
        Assert.That(
            selfieGirl.SubjectRenderer.sortingOrder,
            Is.GreaterThan(HighestMovingSubjectSortingOrder)
        );

        var timeline = Object.FindAnyObjectByType<SubjectTimelineController>();
        Assert.That(timeline, Is.Not.Null);

        foreach (var scheduledPrefab in CollectScheduledPrefabs(timeline))
        {
            if (!scheduledPrefab.TryGetComponent<StageSubject>(out var scheduledSubject))
            {
                continue;
            }

            Assert.That(scheduledSubject.SubjectRenderer, Is.Not.Null);
            Assert.That(
                scheduledSubject.SubjectRenderer.sortingOrder,
                Is.LessThan(SelfieGirlSortingOrder),
                $"'{scheduledPrefab.name}' must render behind SelfieGirl."
            );
        }
    }

    [UnityTest]
    public IEnumerator SelfieGirlIsVisibleOnlyThroughPhotoCamera()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        var mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        Assert.That(photoCamera, Is.Not.Null);
        Assert.That(mainCamera, Is.Not.Null);

        var photoSubjectMask = 1 << PhotoSubjectLayer;
        Assert.That(photoCamera.cullingMask & photoSubjectMask, Is.Not.Zero);
        Assert.That(mainCamera.cullingMask & photoSubjectMask, Is.Zero);
    }

    [UnityTest]
    public IEnumerator SelfieGirlJudgementPointStaysStrictlyInsidePhotoCamera()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        Assert.That(photoCamera, Is.Not.Null);

        var viewportPoint = photoCamera.WorldToViewportPoint(selfieGirl.JudgementPoint.position);

        Assert.That(viewportPoint.x, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.That(viewportPoint.y, Is.GreaterThan(0f).And.LessThan(1f));
    }

    [UnityTest]
    public IEnumerator SelfieGirlJudgementPointIsCoveredByItsOwnCollider()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        var collider = selfieGirl.GetComponent<PolygonCollider2D>();
        Assert.That(collider, Is.Not.Null);

        Assert.That(
            collider.OverlapPoint(selfieGirl.JudgementPoint.position),
            Is.True,
            "JudgementPoint must sit inside the opaque area approximated by the PolygonCollider2D."
        );
    }

    [UnityTest]
    public IEnumerator SelfieGirlOpaqueAreaFillsTheLowerLeftOfPhotoCamera()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        Assert.That(photoCamera, Is.Not.Null);

        var renderTexture = photoCamera.targetTexture;
        Assert.That(renderTexture, Is.Not.Null);

        var viewHalfHeight = photoCamera.orthographicSize;
        var viewHalfWidth = viewHalfHeight * ((float)renderTexture.width / renderTexture.height);
        var viewWidth = viewHalfWidth * 2f;
        var viewHeight = viewHalfHeight * 2f;
        var cameraCenter = photoCamera.transform.position;
        var viewLeft = cameraCenter.x - viewHalfWidth;
        var viewBottom = cameraCenter.y - viewHalfHeight;

        // 透明余白を含むSpriteのBoundsではなく、不透明部分を近似したColliderのBoundsを基準にする。
        var opaqueBounds = selfieGirl.GetComponent<PolygonCollider2D>().bounds;

        // プレイ確認で仮決定した配置。左下角へ寄せ、前景として画面の左半分を覆う。
        Assert.That(
            (opaqueBounds.min.x - viewLeft) / viewWidth,
            Is.EqualTo(0f).Within(0.01f),
            "Opaque area must be anchored to the left edge of the PhotoCamera view."
        );
        Assert.That(
            (opaqueBounds.min.y - viewBottom) / viewHeight,
            Is.EqualTo(0f).Within(0.01f),
            "Opaque area must be anchored to the bottom edge of the PhotoCamera view."
        );
        Assert.That(
            opaqueBounds.size.x / viewWidth,
            Is.InRange(0.48f, 0.56f),
            "Opaque width must cover about half of the PhotoCamera view."
        );
        Assert.That(
            opaqueBounds.size.y / viewHeight,
            Is.InRange(0.70f, 0.76f),
            "Opaque height must cover about three quarters of the PhotoCamera view."
        );
    }

    [UnityTest]
    public IEnumerator SelfieGirlSurvivesUntilPlayingStarts()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var selfieGirl = FindSelfieGirl();
        var stageController = Object.FindAnyObjectByType<Stage1Controller>();
        Assert.That(stageController, Is.Not.Null);

        yield return WaitForStageState(stageController, Stage1Controller.Stage1State.Playing);

        Assert.That(
            selfieGirl == null,
            Is.False,
            "SelfieGirl must not be destroyed while playing."
        );
        Assert.That(selfieGirl.transform.parent.name, Is.EqualTo("FixedSubjectRoot"));
        Assert.That(FindSelfieGirl(), Is.SameAs(selfieGirl));
    }

    private static IEnumerator WaitForStageState(
        Stage1Controller stageController,
        Stage1Controller.Stage1State expectedState
    )
    {
        const float timeoutSeconds = 10f;
        var timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (
            stageController.CurrentState != expectedState && Time.realtimeSinceStartup < timeoutAt
        )
        {
            yield return null;
        }

        Assert.That(
            stageController.CurrentState,
            Is.EqualTo(expectedState),
            $"Stage did not reach '{expectedState}' within {timeoutSeconds} seconds."
        );
    }

    private static StageSubject FindSelfieGirl()
    {
        var selfieGirls = new List<StageSubject>();
        foreach (var subject in Object.FindObjectsByType<StageSubject>(FindObjectsInactive.Exclude))
        {
            if (subject.Id == SubjectId.SelfieGirl)
            {
                selfieGirls.Add(subject);
            }
        }

        Assert.That(
            selfieGirls,
            Has.Count.EqualTo(1),
            "Game_Stage1 must contain exactly one SelfieGirl subject."
        );
        return selfieGirls[0];
    }

    /// <summary>
    /// SubjectTimelineControllerのSpawn設定は入れ子のprivate型のため、リフレクションでPrefabだけを集める。
    /// </summary>
    private static List<GameObject> CollectScheduledPrefabs(SubjectTimelineController timeline)
    {
        var prefabs = new List<GameObject>();

        var spawnSettingsField = typeof(SubjectTimelineController).GetField(
            "spawnSettings",
            PrivateInstance
        );
        Assert.That(spawnSettingsField, Is.Not.Null, "Field 'spawnSettings' was not found.");

        if (spawnSettingsField.GetValue(timeline) is not IEnumerable spawnSettings)
        {
            return prefabs;
        }

        foreach (var spawnSetting in spawnSettings)
        {
            if (spawnSetting == null)
            {
                continue;
            }

            AddPrefabIfPresent(prefabs, spawnSetting, "subjectPrefab");

            var randomRoutesField = spawnSetting
                .GetType()
                .GetField("randomRoutes", PrivateInstance);
            if (randomRoutesField?.GetValue(spawnSetting) is not IEnumerable randomRoutes)
            {
                continue;
            }

            foreach (var route in randomRoutes)
            {
                if (route != null)
                {
                    AddPrefabIfPresent(prefabs, route, "subjectPrefab");
                }
            }
        }

        return prefabs;
    }

    private static void AddPrefabIfPresent(
        ICollection<GameObject> prefabs,
        object source,
        string fieldName
    )
    {
        var field = source.GetType().GetField(fieldName, PrivateInstance);

        // 未設定の参照はUnityのfake-nullとして返るため、`is`だけでなくUnityの==で除外する。
        if (field?.GetValue(source) is GameObject prefab && prefab != null)
        {
            prefabs.Add(prefab);
        }
    }
}

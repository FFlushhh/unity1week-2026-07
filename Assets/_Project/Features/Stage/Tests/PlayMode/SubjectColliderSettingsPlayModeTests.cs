#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 被写体Prefabの当たり判定設定と、不透明・透明領域の代表点を検証します。
/// 頂点列そのものではなく「どこが遮蔽物か」という挙動を対象にします。
/// Prefabアセットを直接読むため、Editorでのみ実行します。
/// </summary>
public sealed class SubjectColliderSettingsPlayModeTests
{
    private const int PhotoSubjectLayer = 6;
    private const string PrefabDirectory = "Assets/_Project/Features/Stage/Prefabs/Subjects";

    private readonly List<GameObject> createdObjects = new();

    /// <summary>
    /// Prefabごとの期待設定。得点と描画順はStageとResultの契約に一致させる。
    /// </summary>
    private static readonly (
        string PrefabName,
        SubjectId Id,
        int Score,
        int SortingOrder
    )[] SubjectSettings =
    {
        ("Dog", SubjectId.Dog, 500, 10),
        ("DirtyClothesPerson", SubjectId.DirtyClothesPerson, -600, 20),
        ("RabidDog", SubjectId.RabidDog, -800, 30),
        ("PlasticBag", SubjectId.PlasticBag, -100, 40),
        ("Bird", SubjectId.Bird, 800, 50),
        ("Sparrow", SubjectId.Sparrow, 5, 60),
        ("SelfieGirl", SubjectId.SelfieGirl, 1000, 100),
    };

    /// <summary>
    /// Inspectorで確定した代表点のローカル座標。Colliderを作り直しても
    /// 「不透明部分は遮蔽し、透明部分は遮蔽しない」挙動が保たれることを確認する。
    /// </summary>
    private static readonly (
        string PrefabName,
        string PointName,
        float X,
        float Y,
        bool ExpectedInside
    )[] RepresentativePoints =
    {
        ("Dog", "face", 0.58f, 0.6f, true),
        ("Dog", "body", 0f, -0.2f, true),
        ("Dog", "topLeftCorner", -1.95f, 1.95f, false),
        ("Dog", "bottomRightCorner", 1.95f, -1.95f, false),
        ("RabidDog", "face", 1.1f, -0.15f, true),
        ("RabidDog", "body", -0.2f, 0f, true),
        ("RabidDog", "topLeftCorner", -1.95f, 1.95f, false),
        ("RabidDog", "topRightCorner", 1.95f, 1.95f, false),
        ("Bird", "body", -0.05f, -0.5f, true),
        ("Bird", "transparentAroundWing", 1.3f, 1.4f, false),
        ("Bird", "bottomLeftCorner", -1.65f, -1.95f, false),
        ("Bird", "topRightCorner", 1.65f, 1.95f, false),
        ("Sparrow", "body", 0.1f, -0.67f, true),
        ("Sparrow", "decorativeStarRight", 1.95f, -0.56f, false),
        ("Sparrow", "decorativeStarLeft", -1.55f, -1.22f, false),
        ("Sparrow", "topLeftCorner", -2.2f, 1.99f, false),
        ("PlasticBag", "bagBody", -0.17f, -0.76f, true),
        ("PlasticBag", "leftHandle", -2.35f, 2.75f, true),
        ("PlasticBag", "rightHandle", 1.85f, 2.75f, true),
        ("PlasticBag", "holeBetweenHandles", 0.01f, 2.75f, false),
        ("PlasticBag", "topLeftCorner", -3.6f, 3.69f, false),
        ("DirtyClothesPerson", "face", -0.01f, 7.26f, true),
        ("DirtyClothesPerson", "body", -0.01f, 0.96f, true),
        ("DirtyClothesPerson", "gapBetweenLeftArmAndBody", -2.91f, -2.97f, false),
        ("DirtyClothesPerson", "gapBetweenRightArmAndBody", 3.19f, -2.97f, false),
        ("DirtyClothesPerson", "gapBetweenLegs", -0.11f, -9.04f, false),
        ("DirtyClothesPerson", "topLeftCorner", -8.26f, 12.91f, false),
        ("SelfieGirl", "face", -5.16f, 2.14f, true),
        ("SelfieGirl", "body", -7f, -5f, true),
        ("SelfieGirl", "transparentRightSide", 12f, 0f, false),
        ("SelfieGirl", "transparentBottomRight", 10f, -8f, false),
        ("SelfieGirl", "topLeftCorner", -17f, 12.2f, false),
    };

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

    [Test]
    public void EverySubjectPrefabUsesPolygonColliderOnPhotoSubjectLayer()
    {
        foreach (var setting in SubjectSettings)
        {
            var prefab = LoadPrefab(setting.PrefabName);
            var label = setting.PrefabName;

            Assert.That(prefab.layer, Is.EqualTo(PhotoSubjectLayer), $"{label}: layer");
            Assert.That(
                prefab.GetComponent<PolygonCollider2D>(),
                Is.Not.Null,
                $"{label}: PolygonCollider2D"
            );
            Assert.That(
                prefab.GetComponent<BoxCollider2D>(),
                Is.Null,
                $"{label}: BoxCollider2D must be removed"
            );

            var subject = prefab.GetComponent<StageSubject>();
            Assert.That(subject, Is.Not.Null, $"{label}: StageSubject");
            Assert.That(subject.Id, Is.EqualTo(setting.Id), $"{label}: id");
            Assert.That(subject.Score, Is.EqualTo(setting.Score), $"{label}: score");
            Assert.That(subject.JudgementPoint, Is.Not.Null, $"{label}: judgement point");
            Assert.That(
                subject.JudgementPoint.parent,
                Is.EqualTo(prefab.transform),
                $"{label}: judgement point parent"
            );
            Assert.That(subject.SubjectRenderer, Is.Not.Null, $"{label}: renderer reference");
            Assert.That(subject.SubjectRenderer.sprite, Is.Not.Null, $"{label}: sprite");
            Assert.That(
                subject.SubjectRenderer.sortingOrder,
                Is.EqualTo(setting.SortingOrder),
                $"{label}: sorting order"
            );
        }
    }

    [Test]
    public void SubjectSortingOrdersAreUniqueSoOcclusionIsDeterministic()
    {
        var sortingOrders = new List<int>();
        foreach (var setting in SubjectSettings)
        {
            sortingOrders.Add(
                LoadPrefab(setting.PrefabName)
                    .GetComponent<StageSubject>()
                    .SubjectRenderer.sortingOrder
            );
        }

        Assert.That(sortingOrders, Is.Unique);
    }

    [UnityTest]
    public IEnumerator EverySubjectKeepsItsJudgementPointInsideItsOwnCollider()
    {
        foreach (var setting in SubjectSettings)
        {
            var instance = InstantiatePrefab(setting.PrefabName);
            Physics2D.SyncTransforms();

            var subject = instance.GetComponent<StageSubject>();
            var collider = instance.GetComponent<PolygonCollider2D>();
            Assert.That(
                collider.OverlapPoint(subject.JudgementPoint.position),
                Is.True,
                $"{setting.PrefabName}: the judgement point must sit on the opaque area."
            );
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator RepresentativePointsMatchTheOpaqueAndTransparentAreas()
    {
        foreach (var testCase in RepresentativePoints)
        {
            var instance = InstantiatePrefab(testCase.PrefabName);
            Physics2D.SyncTransforms();

            var collider = instance.GetComponent<PolygonCollider2D>();
            var worldPoint = instance.transform.TransformPoint(
                new Vector3(testCase.X, testCase.Y, 0f)
            );

            Assert.That(
                collider.OverlapPoint(worldPoint),
                Is.EqualTo(testCase.ExpectedInside),
                $"{testCase.PrefabName}.{testCase.PointName} local({testCase.X}, {testCase.Y}) "
                    + $"must be {(testCase.ExpectedInside ? "inside" : "outside")} the collider."
            );
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator SelfieGirlOccludesMovingSubjectsButIsNeverOccludedByThem()
    {
        var judge = CreateJudge(out var photoCamera);
        var selfieGirl = InstantiatePrefab("SelfieGirl");
        var selfieSubject = selfieGirl.GetComponent<StageSubject>();

        // 自撮りの顔（不透明部分）へ他の被写体の判断点を重ねる。
        var occludedWorldPoint = selfieSubject.JudgementPoint.position;
        selfieGirl.transform.position += (Vector3)(
            (Vector2)photoCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f))
            - (Vector2)occludedWorldPoint
        );
        Physics2D.SyncTransforms();

        foreach (var setting in SubjectSettings)
        {
            if (setting.Id == SubjectId.SelfieGirl)
            {
                continue;
            }

            var movingSubject = InstantiatePrefab(setting.PrefabName);
            var subject = movingSubject.GetComponent<StageSubject>();
            movingSubject.transform.position += (Vector3)(
                (Vector2)selfieSubject.JudgementPoint.position
                - (Vector2)subject.JudgementPoint.position
            );
            Physics2D.SyncTransforms();

            Assert.That(
                judge.IsCapturable(subject),
                Is.False,
                $"{setting.PrefabName} must be occluded by SelfieGirl drawn in front of it."
            );
            Assert.That(
                judge.IsCapturable(selfieSubject),
                Is.True,
                $"SelfieGirl must stay capturable even while overlapping {setting.PrefabName}."
            );

            Object.DestroyImmediate(movingSubject);
            createdObjects.Remove(movingSubject);
            Physics2D.SyncTransforms();
        }

        yield return null;
    }

    private PhotoFrameSubjectJudge CreateJudge(out Camera photoCamera)
    {
        var cameraObject = CreateGameObject("PhotoCamera");
        photoCamera = cameraObject.AddComponent<Camera>();
        photoCamera.orthographic = true;
        photoCamera.orthographicSize = 5f;
        photoCamera.transform.position = new Vector3(0f, 0f, -10f);

        var canvasObject = CreateGameObject("PhotoPreviewCanvas", typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var photoFrameObject = CreateGameObject("PhotoFrame", typeof(RectTransform));
        photoFrameObject.transform.SetParent(canvasObject.transform, false);

        var judge = CreateGameObject("PhotoFrameSubjectJudge")
            .AddComponent<PhotoFrameSubjectJudge>();
        SetPrivateField(judge, "photoCamera", photoCamera);
        SetPrivateField(judge, "photoFrame", photoFrameObject.GetComponent<RectTransform>());
        SetPrivateField(judge, "photoSubjectLayerMask", (LayerMask)(1 << PhotoSubjectLayer));
        return judge;
    }

    private GameObject CreateGameObject(string name, params System.Type[] components)
    {
        var gameObject = new GameObject(name, components);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target
            .GetType()
            .GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }

    private static GameObject LoadPrefab(string prefabName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{PrefabDirectory}/{prefabName}.prefab"
        );
        Assert.That(prefab, Is.Not.Null, $"Prefab '{prefabName}' was not found.");
        return prefab;
    }

    private GameObject InstantiatePrefab(string prefabName)
    {
        var instance = Object.Instantiate(LoadPrefab(prefabName));
        createdObjects.Add(instance);
        return instance;
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhotoFrameSubjectVisibilityPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private Camera photoCamera;
    private PhotoFrameSubjectJudge judge;
    private Sprite whiteSprite;
    private Texture2D whiteTexture;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        whiteSprite = Sprite.Create(
            whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f)
        );

        photoCamera = CreateGameObject("PhotoCamera").AddComponent<Camera>();
        photoCamera.orthographic = true;
        photoCamera.orthographicSize = 5f;
        photoCamera.transform.position = new Vector3(0f, 0f, -10f);

        var canvasObject = CreateGameObject("PhotoPreviewCanvas", typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var photoFrameObject = CreateGameObject("PhotoFrame", typeof(RectTransform));
        photoFrameObject.transform.SetParent(canvasObject.transform, false);

        judge = CreateGameObject("PhotoFrameSubjectJudge").AddComponent<PhotoFrameSubjectJudge>();
        SetPrivateField(judge, "photoCamera", photoCamera);
        SetPrivateField(judge, "photoFrame", photoFrameObject.GetComponent<RectTransform>());
        SetPrivateField(judge, "photoSubjectLayerMask", (LayerMask)(1 << PhotoSubjectLayer));
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.Destroy(createdObject);
            }
        }

        createdObjects.Clear();
        Object.Destroy(whiteSprite);
        Object.Destroy(whiteTexture);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CapturesSubjectInsideFrameWhenNoFrontColliderCoversJudgementPoint()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator KeepsSubjectCapturableWhenOnlyBehindColliderCoversJudgementPoint()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        CreateSubject("BehindSubject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator ExcludesSubjectWhenFrontColliderCoversJudgementPoint()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        CreateSubject("FrontSubject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.False);
    }

    [UnityTest]
    public IEnumerator KeepsSubjectCapturableWhenFrontColliderDoesNotCoverJudgementPoint()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        CreateSubject(
            "FrontSubject",
            new Vector2(0.7f, 0.5f),
            sortingOrder: 10,
            colliderSize: 0.1f
        );
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator ExcludesSubjectWhenJudgementPointIsOnFrameBorder()
    {
        var subject = CreateSubject("Subject", new Vector2(0f, 0.5f), sortingOrder: 0);
        yield return null;

        Assert.That(judge.IsCapturable(subject), Is.False);
    }

    [UnityTest]
    public IEnumerator CapturesSubjectWhoseSpriteCrossesFrameWhenJudgementPointIsInsideAndVisible()
    {
        var subject = CreateSubject("Subject", new Vector2(0.05f, 0.5f), sortingOrder: 0);
        subject.transform.localScale = Vector3.one * 3f;
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator HighPriorityCharacterOccludesSubjectButIsCapturableItself()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        var character = CreateSubject("Character", new Vector2(0.5f, 0.5f), sortingOrder: 100);
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.False);
        Assert.That(judge.IsCapturable(character), Is.True);
    }

    [UnityTest]
    public IEnumerator SameSortingPriorityDoesNotOccludeSubject()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        CreateSubject("SamePrioritySubject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator OwnColliderDoesNotOccludeItsOwnJudgementPoint()
    {
        // 被写体自身のColliderは判断点を必ず覆うため、自己遮蔽しないことを明示的に確認する。
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        yield return null;
        Physics2D.SyncTransforms();

        var ownCollider = subject.GetComponent<Collider2D>();
        Assert.That(ownCollider.OverlapPoint(subject.JudgementPoint.position), Is.True);
        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator DisabledRendererInFrontDoesNotOccludeSubject()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        var frontSubject = CreateSubject("FrontSubject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        frontSubject.SubjectRenderer.enabled = false;
        yield return null;
        Physics2D.SyncTransforms();

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    [UnityTest]
    public IEnumerator InactiveSubjectInFrontDoesNotOccludeSubject()
    {
        var subject = CreateSubject("Subject", new Vector2(0.5f, 0.5f), sortingOrder: 0);
        var frontSubject = CreateSubject("FrontSubject", new Vector2(0.5f, 0.5f), sortingOrder: 10);
        yield return null;
        Physics2D.SyncTransforms();
        frontSubject.SubjectRenderer.gameObject.SetActive(false);
        yield return null;

        Assert.That(judge.IsCapturable(subject), Is.True);
    }

    private const int PhotoSubjectLayer = 6;

    private StageSubject CreateSubject(
        string name,
        Vector2 viewportPosition,
        int sortingOrder,
        float colliderSize = 1f
    )
    {
        var subjectObject = CreateGameObject(name);
        subjectObject.layer = PhotoSubjectLayer;
        subjectObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(viewportPosition.x, viewportPosition.y, 10f)
        );
        var spriteRenderer = subjectObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = whiteSprite;
        spriteRenderer.sortingOrder = sortingOrder;
        var collider = subjectObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one * colliderSize;

        var judgementPointObject = CreateGameObject($"{name}JudgementPoint");
        judgementPointObject.transform.SetParent(subjectObject.transform, false);
        var subject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(subject, "judgementPoint", judgementPointObject.transform);
        SetPrivateField(subject, "subjectRenderer", spriteRenderer);
        return subject;
    }

    private GameObject CreateGameObject(string name, params System.Type[] components)
    {
        var gameObject = new GameObject(name, components);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }
}

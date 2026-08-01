using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhotoFrameSubjectJudgePlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject cameraObject;
    private GameObject canvasObject;
    private GameObject subjectObject;
    private GameObject judgeObject;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(cameraObject);
        Object.Destroy(canvasObject);
        Object.Destroy(subjectObject);
        Object.Destroy(judgeObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClassifiesInsideOutsideAndBorderPoints()
    {
        var judge = CreateJudge();
        Assert.That(GetPrivateField<RectTransform>(judge, "photoFrame"), Is.Not.Null);
        var camera = GetPrivateField<Camera>(judge, "photoCamera");
        var stageSubject = CreateSubject();

        yield return null;

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(0.5f, 0.5f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.True);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(-0.01f, 0.5f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(1.01f, 0.5f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(0.5f, -0.01f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(0.5f, 1.01f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(0f, 0.5f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(1f, 0.5f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(0.5f, 0f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        SetJudgementPointViewportPosition(
            stageSubject.JudgementPoint,
            camera,
            new Vector2(0.5f, 1f)
        );
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);

        stageSubject.JudgementPoint.position = new Vector3(0f, 0f, -11f);
        Assert.That(judge.IsInsidePhotoFrame(stageSubject), Is.False);
    }

    [Test]
    public void MissingReferencesAreTreatedAsOutside()
    {
        judgeObject = new GameObject("PhotoFrameSubjectJudge");
        var judge = judgeObject.AddComponent<PhotoFrameSubjectJudge>();

        Assert.That(judge.IsInsidePhotoFrame((StageSubject)null), Is.False);

        subjectObject = new GameObject("Subject");
        var subject = subjectObject.AddComponent<StageSubject>();
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.False);
    }

    private PhotoFrameSubjectJudge CreateJudge()
    {
        cameraObject = new GameObject("ScreenCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        canvasObject = new GameObject("Canvas", typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var frameObject = new GameObject("PhotoFrame", typeof(RectTransform));
        frameObject.transform.SetParent(canvasObject.transform, false);
        var frame = frameObject.GetComponent<RectTransform>();
        frame.anchorMin = new Vector2(0.2f, 0.2f);
        frame.anchorMax = new Vector2(0.8f, 0.8f);
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;

        judgeObject = new GameObject("PhotoFrameSubjectJudge");
        var judge = judgeObject.AddComponent<PhotoFrameSubjectJudge>();
        SetPrivateField(judge, "photoCamera", camera);
        SetPrivateField(judge, "photoFrame", frame);
        return judge;
    }

    private StageSubject CreateSubject()
    {
        subjectObject = new GameObject("Subject");
        var judgementPointObject = new GameObject("JudgementPoint");
        judgementPointObject.transform.SetParent(subjectObject.transform);
        var subject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(subject, "judgementPoint", judgementPointObject.transform);
        return subject;
    }

    private static void SetJudgementPointViewportPosition(
        Transform judgementPoint,
        Camera camera,
        Vector2 viewportPosition
    )
    {
        judgementPoint.position = camera.ViewportToWorldPoint(
            new Vector3(viewportPosition.x, viewportPosition.y, 10f)
        );
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

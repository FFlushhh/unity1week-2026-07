using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhotoPreviewPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var stageScene = SceneManager.GetSceneByName("Game_Stage0");
        if (stageScene.IsValid() && stageScene.isLoaded)
        {
            var emptyScene = SceneManager.CreateScene($"{nameof(PhotoPreviewPlayModeTests)}.Empty");
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
    public IEnumerator PhotoPreviewKeepsRenderTextureAspectRatio()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var photoPreview = GameObject.Find("PhotoPreview");
        Assert.That(photoPreview, Is.Not.Null);

        var aspectRatioFitter = photoPreview.GetComponent<AspectRatioFitter>();
        Assert.That(aspectRatioFitter, Is.Not.Null);
        Assert.That(
            aspectRatioFitter.aspectMode,
            Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent)
        );
        Assert.That(aspectRatioFitter.aspectRatio, Is.EqualTo(16f / 9f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator PhotoPreviewIsClippedInsideTheFixedPhotoFrame()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var viewport = GameObject.Find("PhotoPreviewViewport");
        var photoPreview = GameObject.Find("PhotoPreview");
        Assert.That(viewport, Is.Not.Null);
        Assert.That(photoPreview, Is.Not.Null);
        Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(photoPreview.transform.parent, Is.EqualTo(viewport.transform));

        var viewportRect = viewport.GetComponent<RectTransform>();
        Assert.That(viewportRect.anchorMin, Is.EqualTo(new Vector2(0.2f, 0.2f)));
        Assert.That(viewportRect.anchorMax, Is.EqualTo(new Vector2(0.8f, 0.8f)));
    }

    [UnityTest]
    public IEnumerator SubjectsAreRenderedOnlyByThePhotoCameraAndSpawnNearItsFrame()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var photoSubjectLayer = LayerMask.NameToLayer("PhotoSubject");
        Assert.That(photoSubjectLayer, Is.GreaterThanOrEqualTo(0));

        var mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        Assert.That(mainCamera.cullingMask & (1 << photoSubjectLayer), Is.EqualTo(0));
        Assert.That(photoCamera.cullingMask & (1 << photoSubjectLayer), Is.Not.EqualTo(0));

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var spawnSettings = GetPrivateField<Array>(timeline, "spawnSettings");
        var halfWidth = photoCamera.orthographicSize * photoCamera.aspect;

        foreach (var spawnSetting in spawnSettings)
        {
            var settingType = spawnSetting.GetType();
            var spawnPosition = (Vector2)
                settingType
                    .GetProperty("SpawnPosition", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);
            var subjectPrefab = (GameObject)
                settingType
                    .GetProperty("SubjectPrefab", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);

            Assert.That(subjectPrefab.layer, Is.EqualTo(photoSubjectLayer));
            Assert.That(Mathf.Abs(spawnPosition.x), Is.EqualTo(9.5f));
            Assert.That(Mathf.Abs(spawnPosition.x), Is.GreaterThan(halfWidth));
            Assert.That(Mathf.Abs(spawnPosition.y), Is.LessThan(photoCamera.orthographicSize));
        }
    }

    [UnityTest]
    public IEnumerator SubjectsHaveCenteredJudgementPointsAndTheSceneJudgeUsesThePhotoFrame()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var judgeObject = GameObject.Find("PhotoFrameSubjectJudge");
        Assert.That(judgeObject, Is.Not.Null);
        var judge = judgeObject.GetComponent<PhotoFrameSubjectJudge>();
        Assert.That(judge, Is.Not.Null);
        var photoCamera = GetPrivateField<Camera>(judge, "photoCamera");
        var photoFrame = GetPrivateField<RectTransform>(judge, "photoFrame");
        Assert.That(photoCamera, Is.EqualTo(GameObject.Find("PhotoCamera").GetComponent<Camera>()));
        Assert.That(
            photoFrame,
            Is.EqualTo(GameObject.Find("PhotoFrame").GetComponent<RectTransform>())
        );

        var timeline = GameObject.Find("SubjectTimeline").GetComponent<SubjectTimelineController>();
        var spawnSettings = GetPrivateField<Array>(timeline, "spawnSettings");
        foreach (var spawnSetting in spawnSettings)
        {
            var subjectPrefab = (GameObject)
                spawnSetting
                    .GetType()
                    .GetProperty("SubjectPrefab", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(spawnSetting);
            var stageSubject = subjectPrefab.GetComponent<StageSubject>();

            Assert.That(stageSubject, Is.Not.Null);
            Assert.That(stageSubject.JudgementPoint, Is.Not.Null);
            Assert.That(stageSubject.JudgementPoint.name, Is.EqualTo("JudgementPoint"));
            Assert.That(stageSubject.JudgementPoint.parent, Is.EqualTo(subjectPrefab.transform));
            Assert.That(stageSubject.JudgementPoint.localPosition, Is.EqualTo(Vector3.zero));
        }
    }

    [UnityTest]
    public IEnumerator SceneJudgeClassifiesThePhotoCameraViewportAndItsBorders()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage0", LoadSceneMode.Single);

        var judge = GameObject
            .Find("PhotoFrameSubjectJudge")
            .GetComponent<PhotoFrameSubjectJudge>();
        var photoCamera = GameObject.Find("PhotoCamera").GetComponent<Camera>();
        var subjectObject = new GameObject("JudgeTestSubject");
        var judgementPointObject = new GameObject("JudgementPoint");
        judgementPointObject.transform.SetParent(subjectObject.transform);
        var subject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(subject, "judgementPoint", judgementPointObject.transform);

        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, 10f)
        );
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.True);

        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0f, 0.5f, 10f)
        );
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.False);

        judgementPointObject.transform.position = photoCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 1f, 10f)
        );
        Assert.That(judge.IsInsidePhotoFrame(subject), Is.False);

        UnityEngine.Object.Destroy(subjectObject);
        yield return null;
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

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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(target);
    }
}

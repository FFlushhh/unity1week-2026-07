using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhotoPreviewPlayModeTests
{
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
}

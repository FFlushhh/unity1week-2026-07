using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class GameOverPresentationPlayModeTests
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var stageScene = SceneManager.GetSceneByName("Game_Stage1");
        if (stageScene.IsValid() && stageScene.isLoaded)
        {
            var emptyScene = SceneManager.CreateScene(
                $"{nameof(GameOverPresentationPlayModeTests)}.Empty"
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
    public IEnumerator GameOverPanelUsesAnOpaqueBlackFadeAndSeparateContent()
    {
        yield return SceneManager.LoadSceneAsync("Game_Stage1", LoadSceneMode.Single);

        var photoPreviewCanvas = GameObject.Find("PhotoPreviewCanvas");
        var gameOverPanel = photoPreviewCanvas.transform.Find("GameOverPanel");
        var overlay = gameOverPanel.GetComponent<Image>();
        var fade = gameOverPanel.GetComponent<CanvasGroup>();
        var content = gameOverPanel.Find("GameOverContent");

        Assert.That(overlay.color, Is.EqualTo(Color.black));
        Assert.That(fade, Is.Not.Null);
        Assert.That(content, Is.Not.Null);
        Assert.That(content.Find("GameOverText"), Is.Not.Null);
        Assert.That(content.Find("ReturnToTitleButton"), Is.Not.Null);
    }
}

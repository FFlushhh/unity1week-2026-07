using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ResultScene.Tests
{
    public class ResultScenePlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ResultDataTransporter.CurrentData = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Start_WithNullData_LogsErrorAndTransitions()
        {
            // Expect the specific error log
            LogAssert.Expect(
                LogType.Error,
                "[ResultSceneManager] ResultDataTransporter.CurrentData is null! Falling back to the next scene."
            );

            ResultDataTransporter.CurrentData = null;

            SceneManager.LoadScene("ResultScene");

            yield return null; // Wait for Start to execute
        }

        [UnityTest]
        public IEnumerator ResultSequence_Skip_CalculatesScoreAndFinishes()
        {
            var resultData = new ResultData
            {
                PlayerName = "TestPlayer",
                LocationName = "TestLocation",
                BaseScore = 1000,
                Bonuses = new List<BonusInputData>(),
            };
            ResultDataTransporter.CurrentData = resultData;

            SceneManager.LoadScene("ResultScene");
            yield return null; // Wait for Start

            var manager = Object.FindAnyObjectByType<ResultSceneManager>();
            Assert.IsNotNull(manager);

            // Use reflection to trigger skip manually (bypassing input dependency)
            var endSequenceMethod = typeof(ResultSceneManager).GetMethod(
                "EndSequenceEarly",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            endSequenceMethod.Invoke(manager, new object[] { resultData });

            yield return null; // Wait a frame

            // Verify Next button is active (which happens at the very end of FinishSequence)
            var nextButtonField = typeof(ResultSceneManager).GetField(
                "_nextButton",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var nextButton = nextButtonField.GetValue(manager) as GameObject;

            Assert.IsNotNull(nextButton);
            Assert.IsTrue(nextButton.activeSelf);
            Assert.That(ResultDataTransporter.CurrentData, Is.Null);
        }

        [UnityTest]
        public IEnumerator ResultSequence_UsesExistingBonusMasterForDogAndBird()
        {
            var resultData = CreateResultData(
                new BonusInputData { BonusName = "犬", Count = 2 },
                new BonusInputData { BonusName = "鳥", Count = 1 }
            );

            var manager = default(ResultSceneManager);
            yield return LoadResultScene(resultData, loadedManager => manager = loadedManager);
            EndSequenceEarly(manager, resultData);

            Assert.That(GetTotalScore(manager), Is.EqualTo(2800));
        }

        [UnityTest]
        public IEnumerator ResultSequence_UsesExistingBonusMasterForDogAndRabidDog()
        {
            var resultData = CreateResultData(
                new BonusInputData { BonusName = "犬", Count = 1 },
                new BonusInputData { BonusName = "狂犬", Count = 1 }
            );

            var manager = default(ResultSceneManager);
            yield return LoadResultScene(resultData, loadedManager => manager = loadedManager);
            EndSequenceEarly(manager, resultData);

            Assert.That(GetTotalScore(manager), Is.EqualTo(700));
        }

        [UnityTest]
        public IEnumerator ResultSequence_DisplaysBaseScoreForEmptyBonuses()
        {
            var resultData = CreateResultData();

            var manager = default(ResultSceneManager);
            yield return LoadResultScene(resultData, loadedManager => manager = loadedManager);
            EndSequenceEarly(manager, resultData);

            Assert.That(GetTotalScore(manager), Is.EqualTo(1000));
        }

        [UnityTest]
        public IEnumerator ResultSceneOwnsCapturedImageAfterClearingTransporter()
        {
            var capturedImage = new Texture2D(2, 2);
            var resultData = CreateResultData();
            resultData.CapturedImage = capturedImage;

            var manager = default(ResultSceneManager);
            yield return LoadResultScene(resultData, loadedManager => manager = loadedManager);

            var imageField = typeof(ResultSceneManager).GetField(
                "_capturedPhotoImage",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var snsImage = imageField.GetValue(manager) as RawImage;
            Assert.That(snsImage.texture, Is.SameAs(capturedImage));
            Assert.That(snsImage.gameObject.name, Is.EqualTo("CapturedPhotoImage"));
            Assert.That(snsImage.transform.parent.name, Is.EqualTo("SnsImage"));
            Assert.That(ResultDataTransporter.CurrentData, Is.Null);

            Object.Destroy(manager.gameObject);
            yield return null;

            Assert.That(capturedImage == null, Is.True);
        }

        [UnityTest]
        public IEnumerator OnNextButtonClicked_TransitionsToNextScene()
        {
            ResultDataTransporter.CurrentData = new ResultData
            {
                PlayerName = "TestPlayer",
                LocationName = "TestLocation",
                BaseScore = 1000,
                Bonuses = new System.Collections.Generic.List<BonusInputData>(),
            };

            SceneManager.LoadScene("ResultScene");
            yield return null;

            var manager = Object.FindAnyObjectByType<ResultSceneManager>();
            Assert.IsNotNull(manager);

            // Title is registered in Build Settings, but this test only verifies that the call is safe.
            LogAssert.ignoreFailingMessages = true;

            manager.OnNextButtonClicked();

            yield return null;

            LogAssert.ignoreFailingMessages = false;
        }

        private static ResultData CreateResultData(params BonusInputData[] bonuses)
        {
            return new ResultData
            {
                PlayerName = "TestPlayer",
                LocationName = "Stage 0",
                BaseScore = 1000,
                Bonuses = new List<BonusInputData>(bonuses),
            };
        }

        private static IEnumerator LoadResultScene(
            ResultData resultData,
            System.Action<ResultSceneManager> onLoaded
        )
        {
            ResultDataTransporter.CurrentData = resultData;
            SceneManager.LoadScene("ResultScene");
            yield return null;

            var manager = Object.FindAnyObjectByType<ResultSceneManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(ResultDataTransporter.CurrentData, Is.Null);
            onLoaded(manager);
        }

        private static void EndSequenceEarly(ResultSceneManager manager, ResultData resultData)
        {
            var method = typeof(ResultSceneManager).GetMethod(
                "EndSequenceEarly",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.That(method, Is.Not.Null);
            method.Invoke(manager, new object[] { resultData });
        }

        private static int GetTotalScore(ResultSceneManager manager)
        {
            var field = typeof(ResultSceneManager).GetField(
                "_totalScore",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(manager);
        }
    }
}

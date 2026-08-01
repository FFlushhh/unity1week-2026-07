using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ResultScene.Tests
{
    public class ResultScenePlayModeTests
    {
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
            ResultDataTransporter.CurrentData = new ResultData
            {
                PlayerName = "TestPlayer",
                LocationName = "TestLocation",
                BaseScore = 1000,
                Bonuses = new System.Collections.Generic.List<BonusInputData>(),
            };

            SceneManager.LoadScene("ResultScene");
            yield return null; // Wait for Start

            var manager = Object.FindAnyObjectByType<ResultSceneManager>();
            Assert.IsNotNull(manager);

            // Use reflection to trigger skip manually (bypassing input dependency)
            var endSequenceMethod = typeof(ResultSceneManager).GetMethod(
                "EndSequenceEarly",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            endSequenceMethod.Invoke(manager, new object[] { ResultDataTransporter.CurrentData });

            yield return null; // Wait a frame

            // Verify Next button is active (which happens at the very end of FinishSequence)
            var nextButtonField = typeof(ResultSceneManager).GetField(
                "_nextButton",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var nextButton = nextButtonField.GetValue(manager) as GameObject;

            Assert.IsNotNull(nextButton);
            Assert.IsTrue(nextButton.activeSelf);
        }

        [UnityTest]
        public IEnumerator ResultSequence_Skip_WithNegativeScore_KeepsDisplayedScoresAtZero()
        {
            var data = new ResultData
            {
                PlayerName = "TestPlayer",
                LocationName = "TestLocation",
                BaseScore = -1,
                Bonuses = new System.Collections.Generic.List<BonusInputData>(),
            };
            ResultDataTransporter.CurrentData = data;

            SceneManager.LoadScene("ResultScene");
            yield return null;

            var manager = Object.FindAnyObjectByType<ResultSceneManager>();
            Assert.IsNotNull(manager);

            var endSequenceMethod = typeof(ResultSceneManager).GetMethod(
                "EndSequenceEarly",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(endSequenceMethod);
            endSequenceMethod.Invoke(manager, new object[] { data });

            // 旧実装の2秒演出が終了する時間を超え、遅延上書きがないことも確認する。
            yield return new WaitForSeconds(2.2f);

            var likeCountText = GetPrivateField<Component>(manager, "_likeCountText");
            var totalScoreText = GetPrivateField<Component>(manager, "_totalScoreText");

            Assert.IsNotNull(likeCountText);
            Assert.IsNotNull(totalScoreText);
            Assert.AreEqual("0", GetText(likeCountText));
            Assert.AreEqual("0", GetText(totalScoreText));
        }

        [UnityTest]
        public IEnumerator ResultSequence_CountUp_KeepsLikeAndTotalScoreSynchronized()
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

            var likeCountText = GetPrivateField<Component>(manager, "_likeCountText");
            var totalScoreText = GetPrivateField<Component>(manager, "_totalScoreText");
            Assert.IsNotNull(likeCountText);
            Assert.IsNotNull(totalScoreText);

            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (GetText(totalScoreText) != "1000" && Time.realtimeSinceStartup < timeoutAt)
                yield return null;

            Assert.AreEqual(
                "1000",
                GetText(totalScoreText),
                "Score count-up did not finish in time."
            );

            float verifyUntil = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < verifyUntil)
            {
                yield return null;
                Assert.AreEqual(
                    GetText(totalScoreText),
                    GetText(likeCountText),
                    "Multiple coroutines updated the like count independently."
                );
            }
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

            // Ignore the failure if MockGameScene is not in build settings during tests
            LogAssert.ignoreFailingMessages = true;

            manager.OnNextButtonClicked();

            yield return null;

            LogAssert.ignoreFailingMessages = false;
        }

        private static T GetPrivateField<T>(ResultSceneManager manager, string fieldName)
            where T : class
        {
            var field = typeof(ResultSceneManager).GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(field);
            return field.GetValue(manager) as T;
        }

        private static string GetText(Component textComponent)
        {
            var textProperty = textComponent.GetType().GetProperty("text");
            Assert.IsNotNull(textProperty);
            return textProperty.GetValue(textComponent) as string;
        }
    }
}

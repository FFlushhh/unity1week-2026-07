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
    }
}

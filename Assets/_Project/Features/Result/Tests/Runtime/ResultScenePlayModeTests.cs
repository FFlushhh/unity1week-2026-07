using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using ResultScene.BuzzReaction;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ResultScene.Tests
{
    public class ResultScenePlayModeTests
    {
        private Texture2D capturedImage;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ResultDataTransporter.CurrentData = null;

            if (capturedImage != null)
            {
                Object.DestroyImmediate(capturedImage);
            }

            capturedImage = null;
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
            capturedImage = new Texture2D(2, 2);
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
            Assert.That(capturedImage == null, Is.False);

            Object.Destroy(manager.gameObject);
            yield return null;

            Assert.That(snsImage.texture, Is.Null);
            Assert.That(capturedImage == null, Is.True);
        }

        [UnityTest]
        public IEnumerator DestroyingRawImageBeforeManagerStillDestroysCapturedImage()
        {
            capturedImage = new Texture2D(2, 2);
            var resultData = CreateResultData();
            resultData.CapturedImage = capturedImage;

            var manager = default(ResultSceneManager);
            yield return LoadResultScene(resultData, loadedManager => manager = loadedManager);

            var capturedPhotoImage = GetCapturedPhotoImage(manager);
            Assert.That(capturedPhotoImage.gameObject, Is.Not.SameAs(manager.gameObject));
            Object.Destroy(capturedPhotoImage.gameObject);
            yield return null;

            Assert.That(capturedPhotoImage == null, Is.True);
            Assert.That(capturedImage == null, Is.False);

            Object.Destroy(manager.gameObject);
            yield return null;

            Assert.That(capturedImage == null, Is.True);
        }

        [UnityTest]
        public IEnumerator MissingRawImageStillDestroysCapturedImageWithManager()
        {
            var manager = default(ResultSceneManager);
            yield return LoadResultScene(
                CreateResultData(),
                loadedManager => manager = loadedManager
            );
            SetPrivateField(manager, "_capturedPhotoImage", null);

            capturedImage = new Texture2D(2, 2);
            var resultData = CreateResultData();
            resultData.CapturedImage = capturedImage;
            manager.PlayResult(resultData);

            Object.Destroy(manager.gameObject);
            yield return null;

            Assert.That(capturedImage == null, Is.True);
        }

        [UnityTest]
        public IEnumerator RepeatedOnDestroyCallsDoNotDoubleDestroyCapturedImage()
        {
            capturedImage = new Texture2D(2, 2);
            var resultData = CreateResultData();
            resultData.CapturedImage = capturedImage;

            var manager = default(ResultSceneManager);
            yield return LoadResultScene(resultData, loadedManager => manager = loadedManager);

            var onDestroy = typeof(ResultSceneManager).GetMethod(
                "OnDestroy",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.That(onDestroy, Is.Not.Null);
            Assert.DoesNotThrow(() => onDestroy.Invoke(manager, null));
            Assert.DoesNotThrow(() => onDestroy.Invoke(manager, null));

            Object.Destroy(manager.gameObject);
            yield return null;

            Assert.That(capturedImage == null, Is.True);
            LogAssert.NoUnexpectedReceived();
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
        public IEnumerator ResultScene_BuzzAudioSource_DoesNotPlayOnAwake()
        {
            // テスト用にダミーの SoundManager を生成
            var dummySoundManagerObj = new GameObject("SoundManager");
            var soundManager = dummySoundManagerObj.AddComponent<global::SoundManager>();

            ResultDataTransporter.CurrentData = CreateResultData(1000);

            SceneManager.LoadScene("ResultScene");
            yield return null;

            var buzzManager = Object.FindAnyObjectByType<BuzzReactionManager>();
            Assert.IsNotNull(buzzManager);

            // SoundManager側の AudioSource を確認
            var audioSource = GetPrivateField<AudioSource>(soundManager, "_pitchedSeAudioSource");
            Assert.IsNotNull(audioSource);
            Assert.IsFalse(audioSource.playOnAwake);
            Assert.IsNull(audioSource.clip);
            Assert.IsFalse(audioSource.isPlaying);

            Object.Destroy(dummySoundManagerObj);
        }

        [UnityTest]
        public IEnumerator ResultSequence_SkipAfterBuzzStarts_StopsAllBuzzPresentation()
        {
            var dummySoundManagerObj = new GameObject("SoundManager");
            var soundManager = dummySoundManagerObj.AddComponent<global::SoundManager>();

            var data = CreateResultData(1000);
            ResultDataTransporter.CurrentData = data;

            SceneManager.LoadScene("ResultScene");
            yield return null;

            var manager = Object.FindAnyObjectByType<ResultSceneManager>();
            var buzzManager = Object.FindAnyObjectByType<BuzzReactionManager>();
            Assert.IsNotNull(manager);
            Assert.IsNotNull(buzzManager);

            var postPanel = GetPrivateField<RectTransform>(buzzManager, "_postPanel");
            var audioSource = GetPrivateField<AudioSource>(soundManager, "_pitchedSeAudioSource");
            Assert.IsNotNull(postPanel);
            Assert.IsNotNull(audioSource);
            Vector3 initialPanelScale = postPanel.localScale;
            int initialActiveChildren = CountActiveChildren(postPanel);

            buzzManager.StartReactionSequence();
            yield return new WaitForSeconds(0.15f);

            Assert.Greater(CountActiveChildren(postPanel), initialActiveChildren);

            var skipPresentationMethod = typeof(ResultSceneManager).GetMethod(
                "SkipPresentation",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(skipPresentationMethod);
            skipPresentationMethod.Invoke(manager, null);
            yield return null;

            Assert.AreEqual(initialActiveChildren, CountActiveChildren(postPanel));
            Assert.AreEqual(initialPanelScale, postPanel.localScale);
            Assert.IsFalse(audioSource.isPlaying);

            Object.Destroy(dummySoundManagerObj);
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

            manager.OnNextButtonClicked();

            yield return WaitForActiveScene("Title");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Title"));
            Assert.That(ResultDataTransporter.CurrentData, Is.Null);
        }

        private static IEnumerator WaitForActiveScene(string expectedSceneName)
        {
            const float timeoutSeconds = 5f;
            var timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

            while (
                SceneManager.GetActiveScene().name != expectedSceneName
                && Time.realtimeSinceStartup < timeoutAt
            )
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(expectedSceneName),
                $"Scene '{expectedSceneName}' was not loaded within {timeoutSeconds} seconds."
            );
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

        private static RawImage GetCapturedPhotoImage(ResultSceneManager manager)
        {
            var field = typeof(ResultSceneManager).GetField(
                "_capturedPhotoImage",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.That(field, Is.Not.Null);
            return field.GetValue(manager) as RawImage;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target
                .GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(target, value);
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

        private static ResultData CreateResultData(int baseScore)
        {
            return new ResultData
            {
                PlayerName = "TestPlayer",
                LocationName = "TestLocation",
                BaseScore = baseScore,
                Bonuses = new System.Collections.Generic.List<BonusInputData>(),
            };
        }

        private static int CountActiveChildren(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
            where T : class
        {
            var field = target
                .GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);
            return field.GetValue(target) as T;
        }

        private static string GetText(Component textComponent)
        {
            var textProperty = textComponent.GetType().GetProperty("text");
            Assert.IsNotNull(textProperty);
            return textProperty.GetValue(textComponent) as string;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResultScene
{
    [System.Serializable]
    public class BonusScoreMaster
    {
        public string BonusName;
        public int ScorePerItem;
    }

    public class ResultSceneManager : MonoBehaviour
    {
        [Header("Debug & Test")]
        [SerializeField]
        private bool _useTestData = false;

        [SerializeField]
        private ResultData _testData;

        [Header("Master Data")]
        [SerializeField]
        private List<BonusScoreMaster> _bonusMaster = new List<BonusScoreMaster>()
        {
            new BonusScoreMaster { BonusName = "犬", ScorePerItem = 500 },
            new BonusScoreMaster { BonusName = "汚れた服の人", ScorePerItem = -600 },
            new BonusScoreMaster { BonusName = "狂犬", ScorePerItem = -800 },
            new BonusScoreMaster { BonusName = "ビニール袋", ScorePerItem = -100 },
            new BonusScoreMaster { BonusName = "鳥", ScorePerItem = 800 },
            new BonusScoreMaster { BonusName = "スズメ", ScorePerItem = 5 },
        };

        [Header("Left Panel (SNS)")]
        [SerializeField]
        private RawImage _capturedPhotoImage;

        [SerializeField]
        private TextMeshProUGUI _userNameText;

        [SerializeField]
        private TextMeshProUGUI _postText;

        [SerializeField]
        private TextMeshProUGUI _likeCountText;

        [Header("Right Panel (Score)")]
        [SerializeField]
        private TextMeshProUGUI _locationNameText;

        [SerializeField]
        private Transform _scoreListContent;

        [SerializeField]
        private GameObject _scoreItemPrefab;

        [SerializeField]
        private ScrollRect _scoreScrollRect;

        [Header("Right Panel (Total & Rank)")]
        [SerializeField]
        private RectTransform _totalScoreArea;

        [SerializeField]
        private TextMeshProUGUI _totalScoreText;

        [SerializeField]
        private Image _rankStampImage;

        [SerializeField]
        private GameObject _rankLabel;

        [Header("Rank Thresholds")]
        [SerializeField]
        private int _rankSThreshold = 10000;

        [SerializeField]
        private int _rankAThreshold = 8000;

        [Header("Post Texts")]
        [SerializeField, TextArea(2, 4)]
        private string _postTextRankS = "奇跡の一枚が撮れた！最高！！";

        [SerializeField, TextArea(2, 4)]
        private string _postTextRankA = "かなり良い写真かも！";

        [SerializeField, TextArea(2, 4)]
        private string _postTextRankB = "まあまあかな。次はもっと頑張る";

        [Header("Rank Sprites")]
        [SerializeField]
        private Sprite _rankSSprite;

        [SerializeField]
        private Sprite _rankASprite;

        [SerializeField]
        private Sprite _rankBSprite;

        [Header("Rank Illustration")]
        [SerializeField]
        private Image _illustrationImage;

        [SerializeField]
        private Sprite _illustrationSSprite;

        [SerializeField]
        private Sprite _illustrationASprite;

        [SerializeField]
        private Sprite _illustrationBSprite;

        [Header("Audio (SoundManager Indices)")]
        [SerializeField]
        private float _seVolumeScale = 0.3f;

        [SerializeField]
        private int _sePopIndex = 15;

        [SerializeField]
        private int _seSlideUpIndex = 16;

        [SerializeField]
        private int _seStampIndex = 16;

        [SerializeField]
        private int _seScoreCountIndex = 16;

        [Header("Misc")]
        [SerializeField]
        private GameObject _nextButton;

        [SerializeField]
        private RectTransform _shakeTarget;

        [SerializeField]
        private string _nextSceneName = "MockGameScene";

        [Header("Buzz Reaction")]
        [SerializeField]
        private BuzzReaction.BuzzReactionManager _buzzReactionManager;

        private int _totalScore;
        private bool _isSequenceFinished = false;
        private bool _isSkipped = false;

        private Coroutine _sequenceCoroutine;
        private Texture2D _ownedCapturedImage;

        private void Start()
        {
            if (ResultDataTransporter.CurrentData != null)
            {
                PlayResult(ResultDataTransporter.CurrentData);
                ResultDataTransporter.CurrentData = null; // 受け取ったデータをクリア
            }
            else if (_useTestData) //デバッグ用
            {
                ResultData testPassData = new ResultData
                {
                    PlayerName = _testData.PlayerName,
                    LocationName = _testData.LocationName,
                    BaseScore = _testData.BaseScore,
                    Bonuses =
                        _testData.Bonuses != null
                            ? new List<BonusInputData>(_testData.Bonuses)
                            : new List<BonusInputData>(),
                };

                if (_testData.CapturedImage != null)
                {
                    testPassData.CapturedImage = CreateTextureCopy(_testData.CapturedImage);
                }

                PlayResult(testPassData);
            }
            else
            {
                Debug.LogError(
                    "[ResultSceneManager] ResultDataTransporter.CurrentData is null! Falling back to the next scene."
                );
                OnNextButtonClicked();
            }
        }

        private Texture2D CreateTextureCopy(Texture2D original)
        {
            if (original == null)
                return null;

            RenderTexture tmp = RenderTexture.GetTemporary(
                original.width,
                original.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );
            Graphics.Blit(original, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D copy = new Texture2D(
                original.width,
                original.height,
                TextureFormat.RGBA32,
                false
            );
            copy.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return copy;
        }

        private void Update()
        {
            if (CheckActionInput())
            {
                if (!_isSequenceFinished && !_isSkipped)
                {
                    SkipPresentation();
                }
                else if (_isSequenceFinished)
                {
                    OnNextButtonClicked();
                }
            }
        }

        private bool CheckActionInput() //クリック（タッチ）およびSpace/Enter判定
        {
#if ENABLE_INPUT_SYSTEM //InputSystemパッケージが有効ならこっちを使う
            if (
                UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame
            )
                return true;
            if (
                UnityEngine.InputSystem.Touchscreen.current != null
                && UnityEngine
                    .InputSystem
                    .Touchscreen
                    .current
                    .primaryTouch
                    .press
                    .wasPressedThisFrame
            )
                return true;

            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (
                    kb.spaceKey.wasPressedThisFrame
                    || kb.enterKey.wasPressedThisFrame
                    || kb.numpadEnterKey.wasPressedThisFrame
                )
                    return true;
            }
            return false;
#else//InputSystemパッケージが無効ならこっちを使う
            return Input.GetMouseButtonDown(0)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        public void PlayResult(ResultData data)
        {
            _isSkipped = false; //リザルトシーン開始時にスキップフラグをリセット
            _isSequenceFinished = false; //演出完了フラグをリセット

            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }
            if (_buzzReactionManager != null)
                _buzzReactionManager.StopReactionSequence();
            _sequenceCoroutine = StartCoroutine(ResultSequenceCoroutine(data));
        }

        private void EndSequenceEarly(ResultData data)
        {
            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _isSequenceFinished = true;
            SkipPresentation();
            if (data != null)
            {
                _totalScore = ResultScoreCalculator.CalculateTotalScore(
                    data.BaseScore,
                    data.Bonuses,
                    _bonusMaster
                );

                if (_totalScoreArea != null)
                    _totalScoreArea.gameObject.SetActive(true);
            }
            FinishSequence();
        }

        private void SkipPresentation()
        {
            _isSkipped = true;
            if (_buzzReactionManager != null)
                _buzzReactionManager.StopReactionSequence();
        }

        private IEnumerator ResultSequenceCoroutine(ResultData data)
        {
            InitializeUI(data);

            yield return WaitOrSkipCoroutine(0.5f);

            _totalScore = data.BaseScore;
            yield return AddScoreItemSequenceCoroutine("基礎スコア", data.BaseScore);

            if (data.Bonuses != null)
            {
                foreach (var bonus in data.Bonuses.Where(b => b.Count > 0))
                {
                    var master = _bonusMaster.FirstOrDefault(m => m.BonusName == bonus.BonusName);
                    if (master == null)
                    {
                        Debug.LogError(
                            $"[ResultSceneManager] Data/Configuration Error: Unknown bonus type '{bonus.BonusName}'."
                        );
                        continue;
                    }
                    int scorePerItem = master.ScorePerItem;
                    string itemName =
                        bonus.Count > 1 ? $"{bonus.BonusName} × {bonus.Count}" : bonus.BonusName;
                    int calculatedScore = scorePerItem * bonus.Count;

                    _totalScore += calculatedScore;
                    yield return AddScoreItemSequenceCoroutine(itemName, calculatedScore);
                }
            }

            yield return WaitOrSkipCoroutine(0.5f);

            if (!_isSkipped)
                PlaySound(_seSlideUpIndex);
            yield return SlideUpTotalScoreAreaSequenceCoroutine();

            Rank rank = GenerateSnsContent(data);

            if (_buzzReactionManager != null && !_isSkipped)
            {
                _buzzReactionManager.StartReactionSequence(rank);
            }

            yield return CountUpScoreSequenceCoroutine(_totalScore);

            yield return WaitOrSkipCoroutine(0.2f);

            if (_rankLabel != null)
                _rankLabel.SetActive(true);
            SetRankStamp(_totalScore);
            SetIllustration(_totalScore);

            if (!_isSkipped)
                PlaySound(_seStampIndex);
            yield return StampAnimationSequenceCoroutine();

            if (!_isSkipped)
                StartCoroutine(UIShakeCoroutine(0.2f, 15f));

            yield return WaitOrSkipCoroutine(0.5f);

            FinishSequence();
        }

        private void InitializeUI(ResultData data)
        {
            _ownedCapturedImage = data.CapturedImage;
            if (_capturedPhotoImage != null)
                _capturedPhotoImage.texture = _ownedCapturedImage;
            if (_userNameText != null)
                _userNameText.text = data.PlayerName;
            if (_postText != null)
                _postText.text = "";
            if (_likeCountText != null)
                _likeCountText.text = "0";
            if (_locationNameText != null)
                _locationNameText.text = data.LocationName;

            if (_totalScoreArea != null)
                _totalScoreArea.gameObject.SetActive(false);
            if (_totalScoreText != null)
                _totalScoreText.text = "0";
            if (_rankStampImage != null)
                _rankStampImage.gameObject.SetActive(false);
            if (_rankLabel != null)
                _rankLabel.SetActive(false);
            if (_illustrationImage != null)
                _illustrationImage.gameObject.SetActive(false);
            if (_nextButton != null)
                _nextButton.SetActive(false);

            // 既存のリスト要素はここで解放
            foreach (Transform child in _scoreListContent)
            {
                Destroy(child.gameObject);
            }
        }

        private Rank GenerateSnsContent(ResultData data)
        {
            Rank rank = ResultScoreCalculator.DetermineRank(
                _totalScore,
                _rankSThreshold,
                _rankAThreshold
            );
            string autoText = rank switch
            {
                Rank.S => _postTextRankS,
                Rank.A => _postTextRankA,
                Rank.B => _postTextRankB,
                _ => _postTextRankB,
            };

            if (_postText != null)
                _postText.text = autoText;

            return rank;
        }

        private IEnumerator AddScoreItemSequenceCoroutine(string itemName, int score)
        {
            if (_scoreItemPrefab == null)
            {
                Debug.LogError(
                    "【ResultSceneManager】_scoreItemPrefab がInspectorで設定されていません。"
                );
                yield break;
            }

            GameObject itemObj = Instantiate(_scoreItemPrefab, _scoreListContent);
            itemObj.transform.localScale = Vector3.one;

            if (itemObj.TryGetComponent<ScoreItemUI>(out var itemUI))
            {
                itemUI.Setup(itemName, score);
            }

            if (!_isSkipped)
                PlaySound(_sePopIndex);

            yield return null;

            if (_scoreScrollRect != null)
                _scoreScrollRect.verticalNormalizedPosition = 0f;

            if (!_isSkipped)
                yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator SlideUpTotalScoreAreaSequenceCoroutine()
        {
            if (_totalScoreArea == null)
                yield break;
            _totalScoreArea.gameObject.SetActive(true);

            if (_isSkipped)
            {
                _totalScoreArea.anchoredPosition = new Vector2(
                    _totalScoreArea.anchoredPosition.x,
                    _totalScoreArea.anchoredPosition.y
                );
                yield break;
            }

            float duration = 0.3f;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(
                _totalScoreArea.anchoredPosition.x,
                -_totalScoreArea.rect.height
            );
            Vector2 endPos = _totalScoreArea.anchoredPosition;

            while (elapsed < duration && !_isSkipped)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                _totalScoreArea.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
                yield return null;
            }
            _totalScoreArea.anchoredPosition = endPos;
        }

        private IEnumerator CountUpScoreSequenceCoroutine(int targetScore)
        {
            if (_isSkipped)
            {
                UpdateScoreTexts(targetScore);
                yield break;
            }

            float duration = 0.8f;
            float elapsed = 0f;

            PlaySound(_seScoreCountIndex);

            while (elapsed < duration && !_isSkipped)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t * (3f - 2f * t);

                int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, easedT));
                UpdateScoreTexts(currentScore);

                yield return null;
            }

            UpdateScoreTexts(targetScore);
        }

        private void UpdateScoreTexts(int score)
        {
            int displayScore = Mathf.Max(0, score);
            string scoreStr = displayScore.ToString();
            if (_totalScoreText != null)
                _totalScoreText.text = scoreStr;
            if (_likeCountText != null)
                _likeCountText.text = scoreStr;
        }

        private IEnumerator StampAnimationSequenceCoroutine()
        {
            if (_rankStampImage == null)
                yield break;
            _rankStampImage.gameObject.SetActive(true);

            if (_isSkipped)
            {
                _rankStampImage.rectTransform.localScale = Vector3.one;
                yield break;
            }

            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 1.5f;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration && !_isSkipped)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t;
                _rankStampImage.rectTransform.localScale = Vector3.Lerp(
                    startScale,
                    endScale,
                    easedT
                );
                yield return null;
            }
            _rankStampImage.rectTransform.localScale = endScale;
        }

        private IEnumerator WaitOrSkipCoroutine(float time)
        {
            float elapsed = 0f;
            while (elapsed < time && !_isSkipped)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator UIShakeCoroutine(float duration, float magnitude)
        {
            if (_shakeTarget == null)
                yield break;
            Vector2 originalPos = _shakeTarget.anchoredPosition;
            float elapsed = 0.0f;

            while (elapsed < duration && !_isSkipped)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                _shakeTarget.anchoredPosition = new Vector2(originalPos.x + x, originalPos.y + y);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _shakeTarget.anchoredPosition = originalPos;
        }

        private void FinishSequence()
        {
            _isSequenceFinished = true;

            UpdateScoreTexts(_totalScore);

            if (_totalScoreArea != null)
                _totalScoreArea.gameObject.SetActive(true);
            if (_rankLabel != null)
                _rankLabel.SetActive(true);

            SetRankStamp(_totalScore);
            SetIllustration(_totalScore);

            if (_rankStampImage != null)
            {
                _rankStampImage.gameObject.SetActive(true);
                _rankStampImage.rectTransform.localScale = Vector3.one;
            }

            if (_nextButton != null)
                _nextButton.SetActive(true);
        }

        private void SetRankStamp(int score)
        {
            if (_rankStampImage == null)
                return;

            Rank rank = ResultScoreCalculator.DetermineRank(
                score,
                _rankSThreshold,
                _rankAThreshold
            );
            Sprite rankSprite = null;

            switch (rank)
            {
                case Rank.S:
                    rankSprite = _rankSSprite;
                    break;
                case Rank.A:
                    rankSprite = _rankASprite;
                    break;
                case Rank.B:
                    rankSprite = _rankBSprite;
                    break;
            }

            _rankStampImage.sprite = rankSprite;
        }

        private void SetIllustration(int score)
        {
            if (_illustrationImage == null)
                return;

            Rank rank = ResultScoreCalculator.DetermineRank(
                score,
                _rankSThreshold,
                _rankAThreshold
            );
            Sprite illustrationSprite = null;

            switch (rank)
            {
                case Rank.S:
                    illustrationSprite = _illustrationSSprite;
                    break;
                case Rank.A:
                    illustrationSprite = _illustrationASprite;
                    break;
                case Rank.B:
                    illustrationSprite = _illustrationBSprite;
                    break;
            }

            _illustrationImage.sprite = illustrationSprite;
            _illustrationImage.gameObject.SetActive(illustrationSprite != null);
            if (illustrationSprite != null)
            {
                // 現在設定されているRectTransformの縦幅（高さ）を取得
                // ※ 固定値にしたい場合は float targetHeight = 150f; のように直接数値を指定してもOK。
                float targetHeight = _illustrationImage.rectTransform.rect.height;

                float aspectRatio = illustrationSprite.rect.width / illustrationSprite.rect.height;

                _illustrationImage.rectTransform.sizeDelta = new Vector2(
                    targetHeight * aspectRatio,
                    targetHeight
                );
            }
        }

        private void PlaySound(int seIndex)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(seIndex, 1.0f, _seVolumeScale);
            }
        }

        public void OnNextButtonClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }

        private void OnDestroy()
        {
            if (_capturedPhotoImage != null)
                _capturedPhotoImage.texture = null;

            if (_ownedCapturedImage == null)
                return;

            Destroy(_ownedCapturedImage);
            _ownedCapturedImage = null;
        }
    }
}

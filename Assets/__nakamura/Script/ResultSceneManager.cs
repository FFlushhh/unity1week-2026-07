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
        private RawImage _snsImage;

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

        [SerializeField]
        private int _rankBThreshold = 5000;

        [Header("Rank Sprites")]
        [SerializeField]
        private Sprite _rankSSprite;

        [SerializeField]
        private Sprite _rankASprite;

        [SerializeField]
        private Sprite _rankBSprite;

        [SerializeField]
        private Sprite _rankCSprite;

        [Header("Audio")]
        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _sePop;

        [SerializeField]
        private AudioClip _seSlideUp;

        [SerializeField]
        private AudioClip _seStamp;

        [SerializeField]
        private AudioClip _seScoreCount;

        [Header("Misc")]
        [SerializeField]
        private GameObject _nextButton;

        [SerializeField]
        private RectTransform _shakeTarget;

        [SerializeField]
        private string _nextSceneName = "MockGameScene";

        private int _totalScore;
        private bool _isSequenceFinished = false;
        private bool _isSkipped = false;

        private Coroutine _sequenceCoroutine;

        private void Start()
        {
            if (ResultDataTransporter.CurrentData != null)
            {
                PlayResult(ResultDataTransporter.CurrentData);
                ResultDataTransporter.CurrentData = null; // 受け取ったデータをクリア
            }
            else if (_useTestData) //デバッグ用
            {
                PlayResult(_testData);
            }
            else
            {
                Debug.LogError(
                    "[ResultSceneManager] ResultDataTransporter.CurrentData is null! Falling back to the next scene."
                );
                OnNextButtonClicked();
            }
        }

        private void Update()
        {
            if (CheckActionInput())
            {
                if (!_isSequenceFinished && !_isSkipped)
                {
                    _isSkipped = true;
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
            _sequenceCoroutine = StartCoroutine(ResultSequenceCoroutine(data));
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
                PlaySound(_seSlideUp);
            yield return SlideUpTotalScoreAreaSequenceCoroutine();

            GenerateSnsContent(data);

            yield return CountUpScoreSequenceCoroutine(_totalScore);

            yield return WaitOrSkipCoroutine(0.2f);

            if (_rankLabel != null)
                _rankLabel.SetActive(true);
            SetRankStamp(_totalScore);

            if (!_isSkipped)
                PlaySound(_seStamp);
            yield return StampAnimationSequenceCoroutine();

            if (!_isSkipped)
                StartCoroutine(UIShakeCoroutine(0.2f, 15f));

            yield return WaitOrSkipCoroutine(0.5f);

            FinishSequence();
        }

        private void InitializeUI(ResultData data)
        {
            if (_snsImage != null && data.CapturedImage != null)
                _snsImage.texture = data.CapturedImage;
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
            if (_nextButton != null)
                _nextButton.SetActive(false);

            // 既存のリスト要素はここで解放
            foreach (Transform child in _scoreListContent)
            {
                Destroy(child.gameObject);
            }
        }

        private void GenerateSnsContent(ResultData data)
        {
            string autoText = _totalScore switch
            {
                var s when s >= _rankSThreshold => "奇跡の一枚が撮れた！最高！！",
                var s when s >= _rankAThreshold => "かなり良い写真かも！",
                var s when s >= _rankBThreshold => "まあまあかな。次はもっと頑張る",
                _ => "うーん、ちょっとタイミングが悪かったかも…",
            };

            if (_postText != null)
                _postText.text = autoText;
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
                PlaySound(_sePop);

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

            if (_seScoreCount != null)
                PlaySound(_seScoreCount);

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
            Sprite targetSprite = _rankCSprite;
            if (score >= _rankSThreshold)
                targetSprite = _rankSSprite;
            else if (score >= _rankAThreshold)
                targetSprite = _rankASprite;
            else if (score >= _rankBThreshold)
                targetSprite = _rankBSprite;
            _rankStampImage.sprite = targetSprite;
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        public void OnNextButtonClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }
    }
}

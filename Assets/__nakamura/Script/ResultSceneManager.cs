using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using R3;

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
        [SerializeField] private bool _useTestData = false;
        [SerializeField] private ResultData _testData;

        [Header("Master Data")]
        [SerializeField] private List<BonusScoreMaster> _bonusMaster = new List<BonusScoreMaster>();

        [Header("Left Panel (SNS)")]
        [SerializeField] private Image _snsImage;
        [SerializeField] private TextMeshProUGUI _userNameText;
        [SerializeField] private TextMeshProUGUI _postText;
        [SerializeField] private TextMeshProUGUI _likeCountText;

        [Header("Right Panel (Score)")]
        [SerializeField] private TextMeshProUGUI _locationNameText;
        [SerializeField] private Transform _scoreListContent;
        [SerializeField] private AssetReferenceGameObject _scoreItemPrefabRef;
        [SerializeField] private ScrollRect _scoreScrollRect;

        [Header("Right Panel (Total & Rank)")]
        [SerializeField] private RectTransform _totalScoreArea;
        [SerializeField] private TextMeshProUGUI _totalScoreText;
        [SerializeField] private Image _rankStampImage;
        [SerializeField] private GameObject _rankLabel;

        [Header("Rank Thresholds")]
        [SerializeField] private int _rankSThreshold = 10000;
        [SerializeField] private int _rankAThreshold = 8000;
        [SerializeField] private int _rankBThreshold = 5000;

        [Header("Rank Sprites")]
        [SerializeField] private Sprite _rankSSprite;
        [SerializeField] private Sprite _rankASprite;
        [SerializeField] private Sprite _rankBSprite;
        [SerializeField] private Sprite _rankCSprite;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _sePop;
        [SerializeField] private AudioClip _seSlideUp;
        [SerializeField] private AudioClip _seStamp;
        [SerializeField] private AudioClip _seScoreCount;

        [Header("Misc")]
        [SerializeField] private GameObject _nextButton;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private string _nextSceneName = "MockGameScene";

        private int _totalScore;
        private bool _isSequenceFinished = false;

        // 【R3】スキップ状態のリアクティブ管理
        private readonly ReactiveProperty<bool> _isSkipped = new(false);

        // 【Addressables】生成したインスタンスの管理リスト（破棄用）
        private readonly List<GameObject> _spawnedInstances = new();

        private void Start()
        {
            // 【R3】入力イベントのストリーム化
            Observable.EveryUpdate()
                .Where(_ => !_isSkipped.Value && !_isSequenceFinished)
                .Where(_ => CheckPointerDown())
                .Subscribe(_ => _isSkipped.Value = true)
                .RegisterTo(destroyCancellationToken);

            if (_useTestData)//デバッグ用
            {
                PlayResult(_testData);
            }
            else if (ResultDataTransporter.CurrentData != null)
            {
                PlayResult(ResultDataTransporter.CurrentData);
            }
        }

        private bool CheckPointerDown()//クリック（タッチ）判定
        {
#if ENABLE_INPUT_SYSTEM //InputSystemパッケージが有効ならこっちを使う
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
#else//InputSystemパッケージが無効ならこっちを使う
            return Input.GetMouseButtonDown(0);
#endif
        }

        public void PlayResult(ResultData data)
        {
            _isSkipped.Value = false; //リザルトシーン開始時にスキップフラグをリセット
            _isSequenceFinished = false; //演出完了フラグをリセット

            // 【UniTask】
            ResultSequenceAsync(data, destroyCancellationToken).Forget();
        }

        // 【UniTask】コルーチンを排除してasync/await化
        private async UniTask ResultSequenceAsync(ResultData data, CancellationToken token)
        {
            try
            {
                InitializeUI(data);

                await WaitOrSkipAsync(0.5f, token);

                _totalScore = data.BaseScore;
                await AddScoreItemSequenceAsync("基礎スコア", data.BaseScore, token);

                if (data.Bonuses != null)
                {
                    foreach (var bonus in data.Bonuses.Where(b => b.Count > 0))
                    {
                        int scorePerItem = _bonusMaster.FirstOrDefault(m => m.BonusName == bonus.BonusName)?.ScorePerItem ?? 0;
                        string itemName = bonus.Count > 1 ? $"{bonus.BonusName} × {bonus.Count}" : bonus.BonusName;
                        int calculatedScore = scorePerItem * bonus.Count;

                        _totalScore += calculatedScore;
                        await AddScoreItemSequenceAsync(itemName, calculatedScore, token);
                    }
                }

                await WaitOrSkipAsync(0.5f, token);

                if (!_isSkipped.Value) PlaySound(_seSlideUp);
                await SlideUpTotalScoreAreaSequenceAsync(token);

                GenerateSnsContent(data);

                await CountUpScoreSequenceAsync(_totalScore, token);

                await WaitOrSkipAsync(0.2f, token);

                if (_rankLabel != null) _rankLabel.SetActive(true);
                SetRankStamp(_totalScore);

                if (!_isSkipped.Value) PlaySound(_seStamp);
                await StampAnimationSequenceAsync(token);

                if (!_isSkipped.Value) CameraShakeAsync(0.2f, 0.3f, token).Forget();

                await WaitOrSkipAsync(0.5f, token);

                FinishSequence();
            }
            catch (System.OperationCanceledException)
            {
                // オブジェクト破棄時のキャンセル処理
                //Debug.Log("リザルト演出がキャンセル（破棄）されました。");
            }
        }

        private void InitializeUI(ResultData data)
        {
            if (_snsImage != null && data.CapturedImage != null) _snsImage.sprite = data.CapturedImage;
            if (_userNameText != null) _userNameText.text = data.PlayerName;
            if (_postText != null) _postText.text = "";
            if (_likeCountText != null) _likeCountText.text = "0";
            if (_locationNameText != null) _locationNameText.text = data.LocationName;

            if (_totalScoreArea != null) _totalScoreArea.gameObject.SetActive(false);
            if (_totalScoreText != null) _totalScoreText.text = "0";
            if (_rankStampImage != null) _rankStampImage.gameObject.SetActive(false);
            if (_rankLabel != null) _rankLabel.SetActive(false);
            if (_nextButton != null) _nextButton.SetActive(false);

            // 既存のリスト要素はここで解放はせず、OnDestroyで一括解放する
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
                _ => "うーん、ちょっとタイミングが悪かったかも…"
            };

            if (_postText != null) _postText.text = autoText;
        }

        private async UniTask AddScoreItemSequenceAsync(string itemName, int score, CancellationToken token)
        {
            if (!_scoreItemPrefabRef.RuntimeKeyIsValid())
            {
                Debug.LogError("【ResultSceneManager】_scoreItemPrefabRef がInspectorで設定されていません。");
                return;
            }

            // 【Addressables】非同期でのインスタンス化とリスト管理
            GameObject itemObj = await _scoreItemPrefabRef.InstantiateAsync(_scoreListContent).WithCancellation(token);
            _spawnedInstances.Add(itemObj); // メモリ解放用に保持

            itemObj.transform.localScale = Vector3.one;

            if (itemObj.TryGetComponent<ScoreItemUI>(out var itemUI))
            {
                itemUI.Setup(itemName, score);
            }

            if (!_isSkipped.Value) PlaySound(_sePop);

            await UniTask.Yield(PlayerLoopTiming.Update, token);

            if (_scoreScrollRect != null) _scoreScrollRect.verticalNormalizedPosition = 0f;

            if (!_isSkipped.Value) await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: token);
        }

        private async UniTask SlideUpTotalScoreAreaSequenceAsync(CancellationToken token)
        {
            if (_totalScoreArea == null) return;
            _totalScoreArea.gameObject.SetActive(true);

            if (_isSkipped.Value)
            {
                _totalScoreArea.anchoredPosition = new Vector2(_totalScoreArea.anchoredPosition.x, _totalScoreArea.anchoredPosition.y);
                return;
            }

            float duration = 0.3f;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(_totalScoreArea.anchoredPosition.x, -_totalScoreArea.rect.height);
            Vector2 endPos = _totalScoreArea.anchoredPosition;

            while (elapsed < duration && !_isSkipped.Value)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                _totalScoreArea.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _totalScoreArea.anchoredPosition = endPos;
        }

        private async UniTask CountUpScoreSequenceAsync(int targetScore, CancellationToken token)
        {
            if (_isSkipped.Value)
            {
                UpdateScoreTexts(targetScore);
                return;
            }

            float duration = 0.8f;
            float elapsed = 0f;

            if (_seScoreCount != null) PlaySound(_seScoreCount);

            while (elapsed < duration && !_isSkipped.Value)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t * (3f - 2f * t);

                int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, easedT));
                UpdateScoreTexts(currentScore);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            UpdateScoreTexts(targetScore);
        }

        private void UpdateScoreTexts(int score)
        {
            string scoreStr = score.ToString();
            if (_totalScoreText != null) _totalScoreText.text = scoreStr;
            if (_likeCountText != null) _likeCountText.text = scoreStr;
        }

        private async UniTask StampAnimationSequenceAsync(CancellationToken token)
        {
            if (_rankStampImage == null) return;
            _rankStampImage.gameObject.SetActive(true);

            if (_isSkipped.Value)
            {
                _rankStampImage.rectTransform.localScale = Vector3.one;
                return;
            }

            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 1.5f;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration && !_isSkipped.Value)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t;
                _rankStampImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _rankStampImage.rectTransform.localScale = endScale;
        }

        private async UniTask WaitOrSkipAsync(float time, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < time && !_isSkipped.Value)
            {
                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private async UniTask CameraShakeAsync(float duration, float magnitude, CancellationToken token)
        {
            if (_mainCamera == null) return;
            Vector3 originalPos = _mainCamera.transform.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration && !_isSkipped.Value)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                _mainCamera.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _mainCamera.transform.localPosition = originalPos;
        }

        private void FinishSequence()
        {
            _isSequenceFinished = true;

            UpdateScoreTexts(_totalScore);

            if (_totalScoreArea != null) _totalScoreArea.gameObject.SetActive(true);
            if (_rankLabel != null) _rankLabel.SetActive(true);

            SetRankStamp(_totalScore);
            if (_rankStampImage != null)
            {
                _rankStampImage.gameObject.SetActive(true);
                _rankStampImage.rectTransform.localScale = Vector3.one;
            }

            if (_nextButton != null) _nextButton.SetActive(true);
        }

        private void SetRankStamp(int score)
        {
            if (_rankStampImage == null) return;
            Sprite targetSprite = _rankCSprite;
            if (score >= _rankSThreshold) targetSprite = _rankSSprite;
            else if (score >= _rankAThreshold) targetSprite = _rankASprite;
            else if (score >= _rankBThreshold) targetSprite = _rankBSprite;
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

        private void OnDestroy()
        {
            // 【Addressables】メモリリーク防止のために、インスタンス化したオブジェクトを全て解放
            foreach (var instance in _spawnedInstances)
            {
                if (instance != null)
                {
                    Addressables.ReleaseInstance(instance);
                }
            }
            _spawnedInstances.Clear();
        }
    }
}

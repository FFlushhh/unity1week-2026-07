using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResultScene
{
    [System.Serializable]
    public class BonusScoreMaster
    {
        [Tooltip("ボーナス名")]
        public string BonusName;

        [Tooltip("ボーナス1個あたりのスコア")]
        public int ScorePerItem;
    }

    [System.Serializable]
    public struct BaseScoreMapping
    {
        [Tooltip("画像ファイル等の名前")]
        public string ImageName;

        [Tooltip("設定する基礎スコア")]
        public int Score;
    }

    public class ResultSceneManager : MonoBehaviour
    {
        private const string RandomDefocusScoreItemName = "ピンボケ";

        [Header("Debug & Test")]
        [SerializeField, Tooltip("テスト用のデータを使用するかどうか")]
        private bool _useTestData = false;

        [SerializeField, Tooltip("テスト実行時に使用されるリザルトデータ")]
        private ResultData _testData;

        [Header("Master Data")]
        [SerializeField, Tooltip("ボーナス名とスコアのマスターデータ")]
        private List<BonusScoreMaster> _bonusMaster = new List<BonusScoreMaster>()
        {
            new BonusScoreMaster { BonusName = "犬", ScorePerItem = 500 },
            new BonusScoreMaster { BonusName = "汚れた服の人", ScorePerItem = -600 },
            new BonusScoreMaster { BonusName = "狂犬", ScorePerItem = -800 },
            new BonusScoreMaster { BonusName = "ビニール袋", ScorePerItem = -100 },
            new BonusScoreMaster { BonusName = "ハト", ScorePerItem = 800 },
            new BonusScoreMaster { BonusName = "青鳥", ScorePerItem = 5 },
            new BonusScoreMaster { BonusName = "自撮り", ScorePerItem = 1000 },
        };

        [Header("Left Panel (SNS)")]
        [SerializeField, Tooltip("リザルト画面に表示する撮影した写真")]
        private RawImage _capturedPhotoImage;

        [SerializeField, Tooltip("プレイヤー名を表示するテキストUI")]
        private TextMeshProUGUI _userNameText;

        [SerializeField, Tooltip("SNS投稿風のテキストUI")]
        private TextMeshProUGUI _postText;

        [SerializeField, Tooltip("いいね数を表示するテキストUI")]
        private TextMeshProUGUI _likeCountText;

        [Header("Right Panel (Score)")]
        [SerializeField, Tooltip("撮影場所を表示するテキストUI")]
        private TextMeshProUGUI _locationNameText;

        [SerializeField, Tooltip("スコア明細を並べる親要素（コンテンツ）")]
        private Transform _scoreListContent;

        [SerializeField, Tooltip("スコア明細1行分のプレハブ")]
        private GameObject _scoreItemPrefab;

        [SerializeField, Tooltip("スコアリストのスクロールビュー")]
        private ScrollRect _scoreScrollRect;

        [Header("Base Score Settings")]
        [SerializeField, Tooltip("基礎スコアを表示する親要素")]
        private Transform _baseScoreContainer;

        [SerializeField, Tooltip("マスターデータにない場合のデフォルト基礎スコア")]
        private int _defaultBaseScore = 1000;

        [SerializeField, Tooltip("画像名ごとの基礎スコアマスターデータ")]
        private List<BaseScoreMapping> _baseScoreMaster = new List<BaseScoreMapping>();

        [Header("Right Panel (Total & Rank)")]
        [SerializeField, Tooltip("合計スコアを表示するエリア")]
        private RectTransform _totalScoreArea;

        [SerializeField, Tooltip("合計スコアを表示するテキストUI")]
        private TextMeshProUGUI _totalScoreText;

        [SerializeField, Tooltip("ランクスタンプの画像UI")]
        private Image _rankStampImage;

        [SerializeField, Tooltip("ランク表示のラベルオブジェクト")]
        private GameObject _rankLabel;

        [Header("Rank Thresholds")]
        [SerializeField, Tooltip("Sランクになるスコアのしきい値")]
        private int _rankSThreshold = 10000;

        [SerializeField, Tooltip("Aランクになるスコアのしきい値")]
        private int _rankAThreshold = 8000;

        [Header("Post Texts")]
        [SerializeField, TextArea(2, 4), Tooltip("Sランク時の投稿テキスト")]
        private string _postTextRankS = "奇跡の一枚が撮れた！最高！！";

        [SerializeField, TextArea(2, 4), Tooltip("Aランク時の投稿テキスト")]
        private string _postTextRankA = "かなり良い写真かも！";

        [SerializeField, TextArea(2, 4), Tooltip("Bランク時の投稿テキスト")]
        private string _postTextRankB = "まあまあかな。次はもっと頑張る";

        [Header("Rank Sprites")]
        [SerializeField, Tooltip("Sランクのスタンプ用スプライト")]
        private Sprite _rankSSprite;

        [SerializeField, Tooltip("Aランクのスタンプ用スプライト")]
        private Sprite _rankASprite;

        [SerializeField, Tooltip("Bランクのスタンプ用スプライト")]
        private Sprite _rankBSprite;

        [Header("Rank Illustration")]
        [SerializeField, Tooltip("ランクごとのイラストを表示する画像UI")]
        private Image _illustrationImage;

        [SerializeField, Tooltip("Sランクのイラストスプライト")]
        private Sprite _illustrationSSprite;

        [SerializeField, Tooltip("Aランクのイラストスプライト")]
        private Sprite _illustrationASprite;

        [SerializeField, Tooltip("Bランクのイラストスプライト")]
        private Sprite _illustrationBSprite;

        [Header("Audio (SoundManager Indices)")]
        [SerializeField, Tooltip("SEの音量スケール（0〜1）")]
        private float _seVolumeScale = 0.3f;

        [SerializeField, Tooltip("スコア項目追加時のSEインデックス")]
        private int _sePopIndex = 15;

        [SerializeField, Tooltip("スコアエリアスライド時のSEインデックス")]
        private int _seSlideUpIndex = 16;

        [SerializeField, Tooltip("スタンプ押下時のSEインデックス")]
        private int _seStampIndex = 16;

        [SerializeField, Tooltip("スコアカウントアップ中のSEインデックス")]
        private int _seScoreCountIndex = 16;

        [SerializeField, Tooltip("画面揺れ演出の対象となるUI")]
        private RectTransform _shakeTarget;

        [SerializeField, Tooltip("次に遷移するシーンの名前")]
        private string _nextSceneName = "MockGameScene";

        [Header("Buzz Reaction")]
        [SerializeField, Tooltip("バズリアクション（いいね演出）を管理するコンポーネント")]
        private BuzzReaction.BuzzReactionManager _buzzReactionManager;

        [Header("Unityroom Scoreboard")]
        [
            SerializeField,
            Tooltip("unityroomへのスコア送信を担当するコンポーネント。未設定なら送信しない")
        ]
        private UnityroomScoreSubmitter _scoreSubmitter;

        [Header("Hold to Skip UI")]
        [SerializeField, Tooltip("長押しスキップ案内のテキストUI")]
        private TMPro.TextMeshProUGUI _holdKeyText;

        [SerializeField, Tooltip("長押しスキップ待機時のテキスト色")]
        private Color _holdTextStartColor = new Color(1f, 1f, 1f, 0.4f); // 待機時の色

        [SerializeField, Tooltip("長押しスキップ完了時のテキスト色")]
        private Color _holdTextEndColor = new Color(1f, 0.9f, 0.2f, 1f); // ゲージMAX時の色（黄色っぽく発光するイメージ）

        [Header("Animation & Timing Parameters")]
        [SerializeField, Tooltip("基礎スコア表示前の待機時間")]
        private float _waitBeforeBaseScore = 0.5f;

        [SerializeField, Tooltip("ボーナススコア表示後の待機時間")]
        private float _waitBeforeTotalScore = 0.5f;

        [SerializeField, Tooltip("スコア追加ごとの待機時間")]
        private float _waitBetweenScoreItems = 0.2f;

        [SerializeField, Tooltip("合計スコア表示エリアのスライド時間")]
        private float _totalScoreSlideDuration = 0.3f;

        [SerializeField, Tooltip("合計スコアのカウントアップ時間")]
        private float _scoreCountUpDuration = 0.8f;

        [SerializeField, Tooltip("スタンプ表示前の待機時間")]
        private float _waitBeforeStamp = 0.2f;

        [SerializeField, Tooltip("スタンプのアニメーション時間")]
        private float _stampAnimationDuration = 0.2f;

        [SerializeField, Tooltip("スタンプアニメーションの開始スケール")]
        private float _stampStartScale = 1.5f;

        [SerializeField, Tooltip("画面揺れ（Shake）の持続時間")]
        private float _shakeDuration = 0.2f;

        [SerializeField, Tooltip("画面揺れ（Shake）の大きさ")]
        private float _shakeMagnitude = 15f;

        [SerializeField, Tooltip("リザルト演出終了後の待機時間")]
        private float _waitAfterResult = 0.5f;

        [Header("Hold to Skip UI Parameters")]
        [SerializeField, Tooltip("長押し完了までの必要時間")]
        private float _holdToSkipRequiredTime = 0.5f;

        [SerializeField, Tooltip("長押し中の最大スケール")]
        private float _holdToSkipMaxScale = 1.15f;

        [SerializeField, Tooltip("キーボード操作時のスクロール速度")]
        private float _keyboardScrollSpeed = 1.5f;

        private int _totalScore;
        private bool _isSequenceFinished = false;
        private bool _isSkipped = false;
        private float _transitionKeyHoldTime = 0f;

        private Coroutine _sequenceCoroutine;
        private Texture2D _ownedCapturedImage;

        [Header("Transition Presentation")]
        [SerializeField, Tooltip("シーン遷移演出用のアニメーター")]
        private Animator _transitionAnimator;

        [SerializeField, Tooltip("シーン遷移アニメーションのトリガー名")]
        private string _transitionTriggerName = "Change";

        [SerializeField, Tooltip("シーン遷移時のフェード時間")]
        private float _fadeDuration = 0.6f;

        private bool _isTransitioning = false; // 重複遷移防止フラグ

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
            copy.name = original.name; // 名前も引き継ぐ

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
                // 画面遷移はUIボタン（NextButton）のクリックのみで行うため、ここでの自動遷移は削除
            }

            if (_isSequenceFinished)
            {
                if (CheckLongPressTransitionInput())
                {
                    _transitionKeyHoldTime += Time.deltaTime;

                    if (_holdKeyText != null)
                    {
                        // 長押し時間に応じて色を変化させ、さらに少しだけ文字を拡大させる（直感的なチャージ感の演出）
                        float progress = Mathf.Clamp01(
                            _transitionKeyHoldTime / _holdToSkipRequiredTime
                        );
                        _holdKeyText.color = Color.Lerp(
                            _holdTextStartColor,
                            _holdTextEndColor,
                            progress
                        );
                        _holdKeyText.transform.localScale = Vector3.Lerp(
                            Vector3.one,
                            Vector3.one * _holdToSkipMaxScale,
                            progress
                        );
                    }

                    if (_transitionKeyHoldTime >= _holdToSkipRequiredTime && !_isTransitioning)
                    {
                        _isTransitioning = true;
                        StartCoroutine(TransitionWithAnimationCoroutine());
                        _transitionKeyHoldTime = 0f; // 重複実行防止
                    }
                }
                else
                {
                    _transitionKeyHoldTime = 0f;

                    if (_holdKeyText != null)
                    {
                        // キーを離したら元の色・サイズに即座に戻す
                        _holdKeyText.color = _holdTextStartColor;
                        _holdKeyText.transform.localScale = Vector3.one;
                    }
                }
            }

            if (_scoreScrollRect != null)
            {
                HandleKeyboardScroll();
            }
        }

        private void HandleKeyboardScroll()
        {
            float scrollDir = 0f;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed)
                    scrollDir += 1f;
                if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
                    scrollDir -= 1f;
            }
#else
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow))
                scrollDir += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow))
                scrollDir -= 1f;
#endif

            if (scrollDir != 0f)
            {
                // スクロール速度（適宜調整）
                float scrollSpeed = _keyboardScrollSpeed;
                _scoreScrollRect.verticalNormalizedPosition +=
                    scrollDir * Time.deltaTime * scrollSpeed;
                _scoreScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                    _scoreScrollRect.verticalNormalizedPosition
                );
            }
        }

        private bool CheckLongPressTransitionInput()
        {
#if ENABLE_INPUT_SYSTEM
            bool isKeyboardPressed = false;
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                isKeyboardPressed =
                    UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed
                    || UnityEngine.InputSystem.Keyboard.current.enterKey.isPressed
                    || UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.isPressed;
            }

            bool isPointerPressed = false;
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                isPointerPressed = UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
            }
            if (!isPointerPressed && UnityEngine.InputSystem.Touchscreen.current != null)
            {
                isPointerPressed = UnityEngine
                    .InputSystem
                    .Touchscreen
                    .current
                    .primaryTouch
                    .press
                    .isPressed;
            }

            return isKeyboardPressed || isPointerPressed;
#else
            bool isKeyboardPressed =
                UnityEngine.Input.GetKey(KeyCode.Space)
                || UnityEngine.Input.GetKey(KeyCode.Return)
                || UnityEngine.Input.GetKey(KeyCode.KeypadEnter);

            bool isPointerPressed =
                UnityEngine.Input.GetMouseButton(0)
                || (
                    UnityEngine.Input.touchCount > 0
                    && (
                        UnityEngine.Input.GetTouch(0).phase == TouchPhase.Moved
                        || UnityEngine.Input.GetTouch(0).phase == TouchPhase.Stationary
                    )
                );

            return isKeyboardPressed || isPointerPressed;
#endif
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
                    GetActualBaseScore(data),
                    data.Bonuses,
                    _bonusMaster,
                    data.IsScoreForcedToZero
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

            if (data.IsScoreForcedToZero)
            {
                _totalScore = 0;
                yield return WaitOrSkipCoroutine(_waitBeforeBaseScore);
                yield return AddScoreItemSequenceCoroutine(RandomDefocusScoreItemName, 0);
            }
            else
            {
                yield return WaitOrSkipCoroutine(_waitBeforeBaseScore);

                // 1. 基礎スコアの追加
                string baseScoreName = GetBaseScoreName(data);
                int actualBaseScore = GetActualBaseScore(data);

                _totalScore = actualBaseScore;
                yield return AddScoreItemSequenceCoroutine(
                    baseScoreName,
                    actualBaseScore,
                    _baseScoreContainer
                );

                if (data.Bonuses != null)
                {
                    foreach (var bonus in data.Bonuses.Where(b => b.Count > 0))
                    {
                        var master = _bonusMaster.FirstOrDefault(m =>
                            m.BonusName == bonus.BonusName
                        );
                        if (master == null)
                        {
                            Debug.LogError(
                                $"[ResultSceneManager] Data/Configuration Error: Unknown bonus type '{bonus.BonusName}'."
                            );
                            continue;
                        }
                        int scorePerItem = master.ScorePerItem;
                        string itemName =
                            bonus.Count > 1
                                ? $"{bonus.BonusName} × {bonus.Count}"
                                : bonus.BonusName;
                        int calculatedScore = scorePerItem * bonus.Count;

                        _totalScore += calculatedScore;
                        yield return AddScoreItemSequenceCoroutine(itemName, calculatedScore);
                    }
                }
            }

            yield return WaitOrSkipCoroutine(_waitBeforeTotalScore);

            if (!_isSkipped)
                PlaySound(_seSlideUpIndex);
            yield return SlideUpTotalScoreAreaSequenceCoroutine();

            Rank rank = GenerateSnsContent(data);

            if (_buzzReactionManager != null && !_isSkipped)
            {
                _buzzReactionManager.StartReactionSequence(rank);
            }

            yield return CountUpScoreSequenceCoroutine(_totalScore);

            yield return WaitOrSkipCoroutine(_waitBeforeStamp);

            if (_rankLabel != null)
                _rankLabel.SetActive(true);
            SetRankStamp(_totalScore);
            SetIllustration(_totalScore);

            if (!_isSkipped)
                PlaySound(_seStampIndex);
            yield return StampAnimationSequenceCoroutine();

            if (!_isSkipped)
                StartCoroutine(UIShakeCoroutine(_shakeDuration, _shakeMagnitude));

            yield return WaitOrSkipCoroutine(_waitAfterResult);

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
            if (_holdKeyText != null)
                _holdKeyText.gameObject.SetActive(false);
            if (_baseScoreContainer != null)
                _baseScoreContainer.gameObject.SetActive(!data.IsScoreForcedToZero);
            if (_scoreListContent != null)
                _scoreListContent.gameObject.SetActive(true);

            if (_scoreScrollRect != null)
            {
                // エラーの原因となるスクロールバーの参照をプログラムから強制解除（これをしないとドラッグすらバグるため残します）
                _scoreScrollRect.verticalScrollbar = null;
            }

            if (_scoreListContent != null)
            {
                // Contentの高さが自動調整されないとスクロールできないため、ContentSizeFitterを強制適用
                if (
                    !_scoreListContent.TryGetComponent<UnityEngine.UI.ContentSizeFitter>(
                        out var fitter
                    )
                )
                {
                    fitter =
                        _scoreListContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                }
                fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            }

            // 既存のリスト要素はここで解放
            foreach (Transform child in _scoreListContent)
            {
                Destroy(child.gameObject);
            }
            if (_baseScoreContainer != null)
            {
                foreach (Transform child in _baseScoreContainer)
                {
                    Destroy(child.gameObject);
                }
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

        private string GetBaseScoreName(ResultData data)
        {
            if (data.CapturedImage != null && !string.IsNullOrEmpty(data.CapturedImage.name))
            {
                return data.CapturedImage.name;
            }
            if (!string.IsNullOrEmpty(data.LocationName))
            {
                return data.LocationName;
            }
            return "基礎スコア";
        }

        private int GetActualBaseScore(ResultData data)
        {
            string baseScoreName = GetBaseScoreName(data);
            int actualBaseScore = _defaultBaseScore;
            var mapping = _baseScoreMaster.FirstOrDefault(x => x.ImageName == baseScoreName);
            if (mapping.ImageName != null)
            {
                actualBaseScore = mapping.Score;
            }
            return actualBaseScore;
        }

        private IEnumerator AddScoreItemSequenceCoroutine(
            string itemName,
            int score,
            Transform customParent = null
        )
        {
            Transform parent = customParent != null ? customParent : _scoreListContent;
            if (_scoreItemPrefab == null)
            {
                Debug.LogError(
                    "【ResultSceneManager】_scoreItemPrefab がInspectorで設定されていません。"
                );
                yield break;
            }

            GameObject itemObj = Instantiate(_scoreItemPrefab, parent);
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
                yield return new WaitForSeconds(_waitBetweenScoreItems);
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

            float duration = _totalScoreSlideDuration;
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

            float duration = _scoreCountUpDuration;
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

            float duration = _stampAnimationDuration;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * _stampStartScale;
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

            if (_holdKeyText != null)
            {
                _holdKeyText.gameObject.SetActive(true);
                _holdKeyText.color = _holdTextStartColor;
                _holdKeyText.transform.localScale = Vector3.one;
            }

            // 通常フロー・スキップフローのどちらでもここを通るため、送信箇所はここ1箇所のみでよい。
            // 送信の成否はゲーム進行に影響させないためFire-and-forgetで呼ぶ。
            if (_scoreSubmitter != null)
            {
                _scoreSubmitter.SendScoreAsync(_totalScore, destroyCancellationToken).Forget();
            }
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

        /// <summary>
        /// アニメーションを再生し、指定時間（0.6秒）待ってから次のシーンへ遷移します
        /// </summary>
        private IEnumerator TransitionWithAnimationCoroutine()
        {
            // 1. アニメーションのトリガーを実行
            if (_transitionAnimator != null && !string.IsNullOrEmpty(_transitionTriggerName))
            {
                _transitionAnimator.SetTrigger(_transitionTriggerName);
            }

            // 2. 0.6秒間待機
            if (_fadeDuration > 0f)
            {
                yield return new WaitForSeconds(_fadeDuration);
            }

            // 3. シーン遷移実行
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }

        /// <summary>
        /// UIボタン（NextButton等）から直接呼ばれた場合にも対応
        /// </summary>
        public void OnNextButtonClicked()
        {
            if (_isTransitioning)
                return;
            _isTransitioning = true;
            StartCoroutine(TransitionWithAnimationCoroutine());
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

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResultScene.BuzzReaction
{
    public class BuzzReactionManager : MonoBehaviour
    {
        [Header("Target UI Elements")]
        [SerializeField]
        private RectTransform _postPanel;

        [SerializeField]
        private TextMeshProUGUI _likeCountText;

        [SerializeField]
        private RectTransform _heartSpawnPoint;

        [SerializeField]
        private RectTransform _danmakuContainer;

        [Header("Audio")]
        [SerializeField]
        private AudioSource _audioSource;

        [SerializeField]
        private AudioClip _countUpClip;

        [Header("Animation Curves")]
        [Tooltip("パネルのバウンド用カーブ。1.0で開始・終了するように設定します。")]
        [SerializeField]
        private AnimationCurve _panelBounceCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 1.1f),
            new Keyframe(1f, 1f)
        );

        [Tooltip("いいね数を0から最終スコアまで補間するためのカーブ。")]
        [SerializeField]
        private AnimationCurve _countUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Timing Parameters")]
        [SerializeField]
        private float _bounceDuration = 0.3f;

        [SerializeField]
        private float _countUpDuration = 2.0f;

        [Header("Prefabs & Resources")]
        [SerializeField]
        private GameObject _heartParticlePrefab;

        [SerializeField]
        private GameObject _danmakuCommentPrefab;

        [SerializeField]
        private string[] _danmakuTexts =
        {
            "すごい！",
            "神！",
            "草",
            "最高じゃん",
            "バズってるｗ",
            "すこ",
        };

        [Header("Heart Particle Settings")]
        [SerializeField]
        private float _heartDuration = 2f;

        [SerializeField]
        private float _heartGravity = 1500f;

        [SerializeField]
        private Vector2 _heartMinVelocity = new Vector2(-200f, 400f);

        [SerializeField]
        private Vector2 _heartMaxVelocity = new Vector2(400f, 900f);

        [SerializeField]
        private float _heartSpawnInterval = 0.05f;

        [Header("Danmaku Settings")]
        [SerializeField]
        private float _danmakuSpeed = 800f;

        [SerializeField]
        private float _danmakuMinY = -300f;

        [SerializeField]
        private float _danmakuMaxY = 300f;

        [SerializeField]
        private float _danmakuSpawnInterval = 0.1f;

        private UIObjectPool _heartPool;
        private UIObjectPool _danmakuPool;
        private Vector3 _initialPanelScale = Vector3.one;
        private Coroutine _sequenceCoroutine;

        private void Awake()
        {
            if (_heartParticlePrefab != null && _heartSpawnPoint != null)
            {
                // anchoredPosition を基準点からの相対座標にするため、スポーンポイントの子要素としてパーティクルを生成
                _heartPool = new UIObjectPool(_heartParticlePrefab, _heartSpawnPoint);
            }
            if (_danmakuCommentPrefab != null && _danmakuContainer != null)
            {
                _danmakuPool = new UIObjectPool(_danmakuCommentPrefab, _danmakuContainer);
            }
            if (_postPanel != null)
            {
                _initialPanelScale = _postPanel.localScale;
            }
        }

        public void StartReactionSequence(int finalLikeCount)
        {
            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);
            int displayCount = Mathf.Max(0, finalLikeCount);
            _sequenceCoroutine = StartCoroutine(ReactionSequenceRoutine(displayCount));
        }

        public void SkipReactionSequence(int finalLikeCount)
        {
            StopAllCoroutines(); // 実行中のカウントアップやバウンド、新規生成をすべて停止

            if (_postPanel != null)
                _postPanel.localScale = _initialPanelScale;
            if (_likeCountText != null)
                _likeCountText.text = Mathf.Max(0, finalLikeCount).ToString();
            if (_audioSource != null)
                _audioSource.pitch = 1.0f;
        }

        private IEnumerator ReactionSequenceRoutine(int finalLikeCount)
        {
            // カウントアップ演出を開始
            StartCoroutine(CountUpRoutine(finalLikeCount));

            // ハートと弾幕の生成を開始
            Coroutine heartSpawning = null;
            if (_heartPool != null)
                heartSpawning = StartCoroutine(SpawnHeartsRoutine());

            Coroutine danmakuSpawning = null;
            if (_danmakuPool != null)
                danmakuSpawning = StartCoroutine(SpawnDanmakuRoutine());

            // カウント中は定期的にパネルをバウンドさせる
            float elapsed = 0f;
            while (elapsed < _countUpDuration)
            {
                yield return StartCoroutine(PanelBounceRoutine());
                elapsed += _bounceDuration;
                // 見た目に応じて、次のバウンドまでに少し待機を入れることも可能
                // yield return new WaitForSeconds(0.1f);
                // elapsed += 0.1f;
            }

            // カウントアップ終了時にパーティクルの新規生成を停止
            if (heartSpawning != null)
                StopCoroutine(heartSpawning);
            if (danmakuSpawning != null)
                StopCoroutine(danmakuSpawning);
        }

        private IEnumerator PanelBounceRoutine()
        {
            if (_postPanel == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < _bounceDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _bounceDuration);
                float curveValue = _panelBounceCurve.Evaluate(normalizedTime);

                _postPanel.localScale = _initialPanelScale * curveValue;
                yield return null;
            }

            _postPanel.localScale = _initialPanelScale;
        }

        private IEnumerator CountUpRoutine(int finalLikeCount)
        {
            if (_likeCountText == null)
                yield break;

            float elapsed = 0f;
            float initialPitch = 1.0f;
            float targetPitch = 2.0f;
            float nextSoundTime = 0f;
            float soundInterval = 0.08f; // 効果音を鳴らす間隔

            while (elapsed < _countUpDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _countUpDuration);

                // カスタムカーブを使用して値を補間する
                float curveValue = _countUpCurve.Evaluate(normalizedTime);
                int currentCount = Mathf.RoundToInt(Mathf.Lerp(0, finalLikeCount, curveValue));

                _likeCountText.text = currentCount.ToString();

                // ピッチを上げながらオーディオを再生
                if (_audioSource != null && _countUpClip != null && elapsed >= nextSoundTime)
                {
                    _audioSource.pitch = Mathf.Lerp(initialPitch, targetPitch, normalizedTime);
                    _audioSource.PlayOneShot(_countUpClip);
                    nextSoundTime = elapsed + soundInterval;
                }

                yield return null;
            }

            _likeCountText.text = finalLikeCount.ToString();

            // 念のためピッチをリセット
            if (_audioSource != null)
            {
                _audioSource.pitch = 1.0f;
            }
        }

        private IEnumerator SpawnHeartsRoutine()
        {
            if (_heartSpawnPoint == null)
                yield break;

            while (true)
            {
                GameObject obj = _heartPool.Get();
                if (obj.TryGetComponent(out HeartParticle heart))
                {
                    // _heartSpawnPoint の子要素なので Vector2.zero を開始位置とする
                    Vector2 startPos = Vector2.zero;
                    float vx = Random.Range(_heartMinVelocity.x, _heartMaxVelocity.x);
                    float vy = Random.Range(_heartMinVelocity.y, _heartMaxVelocity.y);

                    heart.Initialize(
                        _heartPool,
                        startPos,
                        new Vector2(vx, vy),
                        _heartGravity,
                        _heartDuration
                    );
                }
                yield return new WaitForSeconds(_heartSpawnInterval);
            }
        }

        private IEnumerator SpawnDanmakuRoutine()
        {
            if (_danmakuContainer == null || _danmakuTexts.Length == 0)
                yield break;

            float containerWidth = _danmakuContainer.rect.width;
            // レイアウトがすぐに更新されない場合やコンテナ幅が大きい場合に備え、
            // rect.width が 0 の場合は標準的な 1920x1080 キャンバス用の安全な距離を使用する。
            if (containerWidth <= 0)
                containerWidth = 1920f;

            float startX = containerWidth * 0.5f + 200f; // 右端のすぐ外側から開始
            float endX = -containerWidth * 0.5f - 400f; // 左端の外側を終点とする

            while (true)
            {
                GameObject obj = _danmakuPool.Get();
                if (obj.TryGetComponent(out DanmakuComment danmaku))
                {
                    string randomText = _danmakuTexts[Random.Range(0, _danmakuTexts.Length)];
                    float randomY = Random.Range(_danmakuMinY, _danmakuMaxY);

                    danmaku.Initialize(
                        _danmakuPool,
                        randomText,
                        new Vector2(startX, randomY),
                        _danmakuSpeed,
                        endX
                    );
                }
                yield return new WaitForSeconds(_danmakuSpawnInterval);
            }
        }
    }
}

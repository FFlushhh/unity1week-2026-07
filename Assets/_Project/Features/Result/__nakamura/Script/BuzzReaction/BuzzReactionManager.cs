using System.Collections;
using UnityEngine;

namespace ResultScene.BuzzReaction
{
    public class BuzzReactionManager : MonoBehaviour
    {
        [Header("Target UI Elements")]
        [SerializeField]
        private RectTransform _postPanel;

        [SerializeField]
        private RectTransform _heartSpawnPoint;

        [SerializeField]
        private RectTransform _danmakuContainer;

        [Header("Audio (SoundManager Index)")]
        [SerializeField]
        private float _seVolumeScale = 0.3f;

        [SerializeField]
        private int _countUpSeIndex = 16;

        [Header("Animation Curves")]
        [Tooltip("パネルのバウンド用カーブ。1.0で開始・終了するように設定します。")]
        [SerializeField]
        private AnimationCurve _panelBounceCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 1.1f),
            new Keyframe(1f, 1f)
        );

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
        private string[] _danmakuTextsRankS =
        {
            "神！",
            "伝説の始まり",
            "これは伸びる",
            "最高じゃん",
            "バズってるｗ",
            "すこ",
            "助かる",
            "天才",
        };

        [SerializeField]
        private string[] _danmakuTextsRankA =
        {
            "すごい！",
            "いいね",
            "草",
            "最高じゃん",
            "すこ",
            "わかる",
            "草生える",
        };

        [SerializeField]
        private string[] _danmakuTextsRankB =
        {
            "ふむ",
            "なるほど",
            "草",
            "いいね",
            "まあまあ",
            "おつ",
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

        public void StartReactionSequence(Rank rank = Rank.B)
        {
            StopReactionSequence();
            StartCoroutine(ReactionSequenceRoutine(rank));
        }

        public void StopReactionSequence()
        {
            StopAllCoroutines();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopPitchedSE();
            }

            if (_postPanel != null)
                _postPanel.localScale = _initialPanelScale;

            _heartPool?.ReturnAllToPool();
            _danmakuPool?.ReturnAllToPool();
        }

        private IEnumerator ReactionSequenceRoutine(Rank rank)
        {
            float heartInterval = _heartSpawnInterval;
            float danmakuInterval = _danmakuSpawnInterval;
            float audioInterval = 0.08f; // 基準となる効果音の間隔

            switch (rank)
            {
                case Rank.S:
                    heartInterval *= 0.5f; // Sランクは2倍の頻度（多く）
                    danmakuInterval *= 0.5f;
                    audioInterval *= 0.5f;
                    break;
                case Rank.A:
                    heartInterval *= 1.0f; // Aランクを基準量とする
                    danmakuInterval *= 1.0f;
                    audioInterval *= 1.0f;
                    break;
                case Rank.B:
                    heartInterval *= 2.0f; // Bランクは半分の頻度
                    danmakuInterval *= 2.0f;
                    audioInterval *= 2.0f;
                    break;
            }

            string[] currentDanmakuTexts = rank switch
            {
                Rank.S => _danmakuTextsRankS,
                Rank.A => _danmakuTextsRankA,
                Rank.B => _danmakuTextsRankB,
                _ => _danmakuTextsRankB,
            };

            // いいね数はResultSceneManagerだけが更新し、ここでは効果音と装飾演出のみ再生する。
            StartCoroutine(CountUpAudioRoutine(audioInterval));

            // 2. ハートと弾幕の生成を開始
            Coroutine heartSpawning = null;
            if (_heartPool != null)
                heartSpawning = StartCoroutine(SpawnHeartsRoutine(heartInterval));

            Coroutine danmakuSpawning = null;
            if (_danmakuPool != null)
                danmakuSpawning = StartCoroutine(
                    SpawnDanmakuRoutine(danmakuInterval, currentDanmakuTexts)
                );

            // 3. カウント中は定期的にパネルをバウンドさせる
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

        private IEnumerator CountUpAudioRoutine(float soundInterval)
        {
            float elapsed = 0f;
            float initialPitch = 1.0f;
            float targetPitch = 2.0f;
            float nextSoundTime = 0f;

            while (elapsed < _countUpDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _countUpDuration);

                // ピッチを上げながらオーディオを再生
                if (elapsed >= nextSoundTime)
                {
                    float currentPitch = Mathf.Lerp(initialPitch, targetPitch, normalizedTime);
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlaySE(_countUpSeIndex, currentPitch, _seVolumeScale);
                    }
                    nextSoundTime = elapsed + soundInterval;
                }

                yield return null;
            }
        }

        private IEnumerator SpawnHeartsRoutine(float interval)
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
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator SpawnDanmakuRoutine(float interval, string[] texts)
        {
            if (_danmakuContainer == null || texts == null || texts.Length == 0)
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
                    string randomText = texts[Random.Range(0, texts.Length)];
                    float randomY = Random.Range(_danmakuMinY, _danmakuMaxY);

                    danmaku.Initialize(
                        _danmakuPool,
                        randomText,
                        new Vector2(startX, randomY),
                        _danmakuSpeed,
                        endX
                    );
                }
                yield return new WaitForSeconds(interval);
            }
        }
    }
}

using System.Collections;
using UnityEngine;

namespace ResultScene.BuzzReaction
{
    public class BuzzReactionManager : MonoBehaviour
    {
        [Header("--- 調整用パラメータ ---")]
        [Header("Reaction Multipliers - Rank S")]
        [SerializeField, Tooltip("Sランク時のハート発生量倍率")]
        private float _rankSHeartMultiplier = 2.0f;

        [SerializeField, Tooltip("Sランク時の弾幕発生量倍率")]
        private float _rankSDanmakuMultiplier = 2.0f;

        [SerializeField, Tooltip("Sランク時の音発生量倍率")]
        private float _rankSAudioMultiplier = 2.0f;

        [Header("Reaction Multipliers - Rank A")]
        [SerializeField, Tooltip("Aランク時のハート発生量倍率")]
        private float _rankAHeartMultiplier = 1.0f;

        [SerializeField, Tooltip("Aランク時の弾幕発生量倍率")]
        private float _rankADanmakuMultiplier = 1.0f;

        [SerializeField, Tooltip("Aランク時の音発生量倍率")]
        private float _rankAAudioMultiplier = 1.0f;

        [Header("Reaction Multipliers - Rank B")]
        [SerializeField, Tooltip("Bランク時のハート発生量倍率")]
        private float _rankBHeartMultiplier = 0.5f;

        [SerializeField, Tooltip("Bランク時の弾幕発生量倍率")]
        private float _rankBDanmakuMultiplier = 0.5f;

        [SerializeField, Tooltip("Bランク時の音発生量倍率")]
        private float _rankBAudioMultiplier = 0.5f;

        [Header("Heart Particle Settings")]
        [SerializeField, Tooltip("ハートパーティクルの生成間隔")]
        private float _heartSpawnInterval = 0.05f;

        [SerializeField, Tooltip("ハートパーティクルの初期速度の最小値")]
        private Vector2 _heartMinVelocity = new Vector2(-200f, 400f);

        [SerializeField, Tooltip("ハートパーティクルの初期速度の最大値")]
        private Vector2 _heartMaxVelocity = new Vector2(400f, 900f);

        [SerializeField, Tooltip("ハートパーティクルにかかる重力")]
        private float _heartGravity = 1500f;

        [SerializeField, Tooltip("ハートパーティクルの生存時間")]
        private float _heartDuration = 2f;

        [Header("Danmaku Settings")]
        [SerializeField, Tooltip("弾幕コメントの生成間隔")]
        private float _danmakuSpawnInterval = 0.1f;

        [SerializeField, Tooltip("弾幕コメント開始位置のマージン")]
        private float _danmakuStartXMargin = 200f;

        [SerializeField, Tooltip("弾幕コメント終了位置のマージン")]
        private float _danmakuEndXMargin = 400f;

        [SerializeField, Tooltip("弾幕コメントのY座標の最小値")]
        private float _danmakuMinY = -300f;

        [SerializeField, Tooltip("弾幕コメントのY座標の最大値")]
        private float _danmakuMaxY = 300f;

        [SerializeField, Tooltip("弾幕コメントの移動速度")]
        private float _danmakuSpeed = 800f;

        [Header("Danmaku Texts")]
        [SerializeField, Tooltip("Sランク時に流れる弾幕コメントのリスト")]
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

        [SerializeField, Tooltip("Aランク時に流れる弾幕コメントのリスト")]
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

        [SerializeField, Tooltip("Bランク時に流れる弾幕コメントのリスト")]
        private string[] _danmakuTextsRankB =
        {
            "ふむ",
            "なるほど",
            "草",
            "いいね",
            "まあまあ",
            "おつ",
        };

        [Header("Audio Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("SEの音量スケール")]
        private float _seVolumeScale = 0.3f;

        [SerializeField, Tooltip("基準となる効果音の再生間隔")]
        private float _baseAudioInterval = 0.08f;

        [SerializeField, Tooltip("カウントアップ開始時のピッチ")]
        private float _countUpInitialPitch = 1.0f;

        [SerializeField, Tooltip("カウントアップ終了時のピッチ")]
        private float _countUpTargetPitch = 2.0f;

        [Header("Animation & Timing")]
        [SerializeField, Tooltip("いいね数のカウントアップにかける時間")]
        private float _countUpDuration = 2.0f;

        [SerializeField, Tooltip("パネルがバウンドする演出の時間")]
        private float _bounceDuration = 0.3f;

        [Tooltip("パネルのバウンド用カーブ。1.0で開始・終了するように設定します。")]
        [SerializeField]
        private AnimationCurve _panelBounceCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 1.1f),
            new Keyframe(1f, 1f)
        );

        [Header("--- 参照用データ（基本変更不要） ---")]
        [Header("Target UI Elements")]
        [SerializeField, Tooltip("バウンド演出を行う対象のパネルUI")]
        private RectTransform _postPanel;

        [SerializeField, Tooltip("ハートパーティクルの生成基準点")]
        private RectTransform _heartSpawnPoint;

        [SerializeField, Tooltip("弾幕コメントを配置するコンテナUI")]
        private RectTransform _danmakuContainer;

        [Header("Prefabs & Resources")]
        [SerializeField, Tooltip("生成するハートパーティクルのプレハブ")]
        private GameObject _heartParticlePrefab;

        [SerializeField, Tooltip("生成する弾幕コメントのプレハブ")]
        private GameObject _danmakuCommentPrefab;

        [SerializeField, Tooltip("カウントアップ時のSEインデックス")]
        private int _countUpSeIndex = 16;

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

        public void StartReactionSequence(Rank rank = Rank.B, bool playLikeEffects = true)
        {
            StopReactionSequence();
            StartCoroutine(ReactionSequenceRoutine(rank, playLikeEffects));
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

        private IEnumerator ReactionSequenceRoutine(Rank rank, bool playLikeEffects)
        {
            float heartMultiplier = rank switch
            {
                Rank.S => _rankSHeartMultiplier,
                Rank.A => _rankAHeartMultiplier,
                Rank.B => _rankBHeartMultiplier,
                _ => _rankBHeartMultiplier,
            };

            float danmakuMultiplier = rank switch
            {
                Rank.S => _rankSDanmakuMultiplier,
                Rank.A => _rankADanmakuMultiplier,
                Rank.B => _rankBDanmakuMultiplier,
                _ => _rankBDanmakuMultiplier,
            };

            float audioMultiplier = rank switch
            {
                Rank.S => _rankSAudioMultiplier,
                Rank.A => _rankAAudioMultiplier,
                Rank.B => _rankBAudioMultiplier,
                _ => _rankBAudioMultiplier,
            };

            float danmakuInterval = _danmakuSpawnInterval / danmakuMultiplier;

            string[] currentDanmakuTexts = rank switch
            {
                Rank.S => _danmakuTextsRankS,
                Rank.A => _danmakuTextsRankA,
                Rank.B => _danmakuTextsRankB,
                _ => _danmakuTextsRankB,
            };

            // いいね数はResultSceneManagerだけが更新し、ここでは効果音と装飾演出のみ再生する。
            // ハートと弾幕の生成を開始
            Coroutine heartSpawning = null;
            if (playLikeEffects)
            {
                float heartInterval = _heartSpawnInterval / heartMultiplier;
                float audioInterval = _baseAudioInterval / audioMultiplier;
                StartCoroutine(CountUpAudioRoutine(audioInterval));

                if (_heartPool != null)
                    heartSpawning = StartCoroutine(SpawnHeartsRoutine(heartInterval));
            }

            Coroutine danmakuSpawning = null;
            if (_danmakuPool != null)
                danmakuSpawning = StartCoroutine(
                    SpawnDanmakuRoutine(danmakuInterval, currentDanmakuTexts)
                );

            // カウント中は定期的にパネルをバウンドさせる
            if (playLikeEffects)
            {
                float elapsed = 0f;
                while (elapsed < _countUpDuration)
                {
                    yield return StartCoroutine(PanelBounceRoutine());
                    elapsed += _bounceDuration;
                    // 見た目に応じて、次のバウンドまでに少し待機を入れることも可能
                    // yield return new WaitForSeconds(0.1f);
                    // elapsed += 0.1f;
                }
            }
            else
            {
                // 0いいねでもコメントだけは通常と同じ表示時間だけ流す。
                yield return new WaitForSeconds(_countUpDuration);
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
            float initialPitch = _countUpInitialPitch;
            float targetPitch = _countUpTargetPitch;
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

            float startX = containerWidth * 0.5f + _danmakuStartXMargin; // 右端のすぐ外側から開始
            float endX = -containerWidth * 0.5f - _danmakuEndXMargin; // 左端の外側を終点とする

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

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("フェード用Sprite")]
    [SerializeField]
    private SpriteRenderer _fadeSprite; // 暗転用のSpriteRenderer

    [SerializeField]
    private float _fadeDuration = 1.0f; // 暗転にかかる時間（秒）

    [Header("遷移先")]
    [SerializeField]
    private string _nextSceneName = "GameScene"; // 遷移したいシーン名

    private bool _isFading = false;

    #region テスト検証・設定用プロパティ (Test Runner用)
    /// <summary>
    /// 現在フェード中（または処理中）かどうかを取得
    /// </summary>
    public bool IsFading => _isFading;

    /// <summary>
    /// テスト等からフェード時間を動的に変更/検証するためのプロパティ
    /// </summary>
    public float FadeDuration
    {
        get => _fadeDuration;
        set => _fadeDuration = value;
    }

    /// <summary>
    /// テスト等から遷移先シーン名を動的に変更/検証するためのプロパティ
    /// </summary>
    public string NextSceneName
    {
        get => _nextSceneName;
        set => _nextSceneName = value;
    }

    /// <summary>
    /// シーン遷移処理が最後まで完了したかどうかのフラグ（テスト用）
    /// </summary>
    public bool IsTransitionCompleted { get; private set; } = false;
    #endregion

    private void Start()
    {
        // 開始時はフェード用Spriteを完全に透明（アルファ値 = 0）にしておく
        if (_fadeSprite != null)
        {
            Color color = _fadeSprite.color;
            color.a = 0f;
            _fadeSprite.color = color;
        }
    }

    // ボタンの OnClick() などから呼び出すメソッド
    public void OnStartButtonClicked()
    {
        // 連続押し防止（Repeat Activation の抑制）
        if (_isFading)
            return;

        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        _isFading = true;

        // 【テスト条件対応】Absent Fade References: 参照が欠損していても例外を出さずに遷移する
        if (_fadeSprite == null)
        {
            Debug.LogWarning(
                "暗転用の SpriteRenderer が割り当てられていません。フェードをスキップしてシーン移動します。"
            );

            TriggerSceneLoad();
            yield break;
        }

        // 【テスト条件対応】Zero-duration Fades: 時間が0以下の場合はループせずに即時真っ黒にする
        if (_fadeDuration <= 0f)
        {
            Color c = _fadeSprite.color;
            c.a = 1f;
            _fadeSprite.color = c;
        }
        else
        {
            float time = 0f;
            Color color = _fadeSprite.color;

            // 時間経過で Alpha（透明度）を 0 から 1 へ（徐々に暗転）
            while (time < _fadeDuration)
            {
                time += Time.deltaTime;
                color.a = Mathf.Clamp01(time / _fadeDuration);
                _fadeSprite.color = color;
                yield return null; // 1フレーム待機
            }
        }

        // シーン切り替えを実行
        TriggerSceneLoad();
    }

    private void TriggerSceneLoad()
    {
        IsTransitionCompleted = true;

        // Build Settingsに登録されていないシーン名でのロードによるクラッシュを防ぐガード（必要に応じて）
        if (!string.IsNullOrEmpty(_nextSceneName))
        {
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}

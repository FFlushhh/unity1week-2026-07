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
        // 連続押し防止
        if (_isFading)
            return;

        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        _isFading = true;

        if (_fadeSprite == null)
        {
            Debug.LogError("暗転用の SpriteRenderer が割り当てられていません！");
            SceneManager.LoadScene(_nextSceneName);
            yield break;
        }

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

        // 完全に真っ黒（1.0）になったらシーン切り替え
        SceneManager.LoadScene(_nextSceneName);
    }
}

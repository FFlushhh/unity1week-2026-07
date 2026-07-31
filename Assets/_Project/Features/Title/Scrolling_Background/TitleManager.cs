using System.Collections;
using UnityEngine;
// 新しいInput Systemを使用するための名前空間を追加
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("フェード用Sprite")]
    [SerializeField]
    private SpriteRenderer _fadeSprite; // 暗転用のSpriteRenderer[cite: 1]

    [SerializeField]
    private float _fadeDuration = 1.0f; // 暗転にかかる時間（秒）[cite: 1]

    [Header("遷移先")]
    [SerializeField]
    private string _nextSceneName = "GameScene"; // 遷移したいシーン名[cite: 1]

    private bool _isFading = false; //[cite: 1]
    #region テスト検証・設定用プロパティ (Test Runner用)
    public bool IsFading => _isFading; //[cite: 1]
    public float FadeDuration
    {
        get => _fadeDuration;
        set => _fadeDuration = value;
    } //[cite: 1]
    public string NextSceneName
    {
        get => _nextSceneName;
        set => _nextSceneName = value;
    } //[cite: 1]
    public bool IsTransitionCompleted { get; private set; } = false; //[cite: 1]
    #endregion

    private void Start()
    {
        if (_fadeSprite != null) //[cite: 1]
        {
            Color color = _fadeSprite.color; //[cite: 1]
            color.a = 0f; //[cite: 1]
            _fadeSprite.color = color; //[cite: 1]
        }
        SoundManager.Instance.PlayBGM(0);
    }

    private void Update()
    {
        // 新しいInput Systemでのキーボード入力監視
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // Spaceキー、またはEnterキー（Return/テンキーのEnter）が「このフレームで押されたか」を判定
            if (
                keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
            )
            {
                StartGameProcess();
            }
        }
    }

    public void OnStartButtonClicked()
    {
        StartGameProcess();
    }

    private void StartGameProcess()
    {
        if (_isFading)
            return;

        StartCoroutine(FadeAndLoadScene());
        SoundManager.Instance.PlaySE(12);
    }

    private IEnumerator FadeAndLoadScene()
    {
        _isFading = true; //[cite: 1]

        if (_fadeSprite == null) //[cite: 1]
        {
            Debug.LogWarning(
                "暗転用の SpriteRenderer が割り当てられていません。フェードをスキップしてシーン移動します。"
            ); //[cite: 1]
            TriggerSceneLoad(); //[cite: 1]
            yield break; //[cite: 1]
        }

        if (_fadeDuration <= 0f) //[cite: 1]
        {
            Color c = _fadeSprite.color; //[cite: 1]
            c.a = 1f; //[cite: 1]
            _fadeSprite.color = c; //[cite: 1]
        }
        else
        {
            float time = 0f; //[cite: 1]
            Color color = _fadeSprite.color; //[cite: 1]

            while (time < _fadeDuration) //[cite: 1]
            {
                time += Time.deltaTime; //[cite: 1]
                color.a = Mathf.Clamp01(time / _fadeDuration); //[cite: 1]
                _fadeSprite.color = color; //[cite: 1]
                yield return null; //[cite: 1]
            }
        }

        TriggerSceneLoad(); //[cite: 1]
    }

    private void TriggerSceneLoad()
    {
        IsTransitionCompleted = true; //[cite: 1]

        if (!string.IsNullOrEmpty(_nextSceneName)) //[cite: 1]
        {
            SceneManager.LoadScene(_nextSceneName); //[cite: 1]
        }
    }
}

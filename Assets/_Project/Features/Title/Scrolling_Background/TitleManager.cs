using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("アニメーション設定")]
    [SerializeField]
    private Animator _transitionAnimator; // 遷移用アニメーションを持つAnimator

    [SerializeField]
    private string _transitionTriggerName = "Change"; // AnimatorのTrigger名

    [SerializeField]
    private float _fadeDuration = 0.6f; // アニメーションを待つ時間（秒）

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
    }
    public string NextSceneName
    {
        get => _nextSceneName;
        set => _nextSceneName = value;
    } //[cite: 1]
    public bool IsTransitionCompleted { get; private set; } = false; //[cite: 1]
    #endregion

    private void Start()
    {
        // 永続化された SoundManager を安全に参照してBGM再生
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(0);
        }
    }

    private void Update()
    {
        // 新しいInput Systemでのキーボード入力監視
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
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

        StartCoroutine(AnimateAndLoadScene());

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(14);
            SoundManager.Instance.StopBGM();
        }
    }

    private IEnumerator AnimateAndLoadScene()
    {
        _isFading = true; //[cite: 1]

        // 1. アニメーションのトリガーを実行
        if (_transitionAnimator != null && !string.IsNullOrEmpty(_transitionTriggerName))
        {
            _transitionAnimator.SetTrigger(_transitionTriggerName);
        }
        else
        {
            Debug.LogWarning(
                "[TitleManager] Transition Animator が割り当てられていないか、Trigger名が空です。"
            );
        }

        // 2. 指定時間（0.6秒）だけアニメーション再生を待つ
        if (_fadeDuration > 0f)
        {
            yield return new WaitForSeconds(_fadeDuration);
        }

        // 3. シーン切り替えを実行
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

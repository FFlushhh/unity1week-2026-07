using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Game_Stage0の進行状態と、状態ごとのUI表示を管理します。
/// </summary>
public sealed class Stage0Controller : MonoBehaviour
{
    public enum Stage0State
    {
        StartMessage,
        Playing,
        CapturedWaitingForTimeout,
        Completed,
        GameOver,
    }

    [Header("State Settings")]
    [SerializeField]
    private StagePhotoFocusPresentation photoFocusPresentation;

    [SerializeField, Min(0f)]
    private float playingDuration = 10f;

    [Header("State UI")]
    [SerializeField]
    private GameObject startMessage;

    [SerializeField]
    private GameObject timer;

    [SerializeField]
    private GameObject photoFrame;

    [SerializeField]
    private GameObject shutterButton;

    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField, Min(0f)]
    private float gameOverFadeDuration = 0.5f;

    [SerializeField]
    private CanvasGroup gameOverFade;

    [SerializeField]
    private GameObject gameOverContent;

    [SerializeField]
    private TMP_Text timerText;

    // ★ ゲームオーバー時に再生する SE の設定
    [Header("SE Settings")]
    [SerializeField]
    private int _gameOverSeIndex = 0; // ゲームオーバー時のSE番号 (SoundManagerのリスト順)

    [SerializeField]
    private float _gameOverSePitch = 1.0f; // SEのピッチ

    [SerializeField]
    private float _gameOverSeVolume = 1.0f; // SEの音量

    private Stage0State currentState;
    private float remainingTime;
    private bool hasInitialized;

    public Stage0State CurrentState => currentState;

    public float RemainingTime => remainingTime;

    public event Action<Stage0State> StateChanged;

    private void Start()
    {
        RunStartMessageAsync(destroyCancellationToken).Forget();
    }

    private async UniTask RunStartMessageAsync(CancellationToken cancellationToken)
    {
        TransitionTo(Stage0State.StartMessage);

        if (photoFocusPresentation == null)
        {
            Debug.LogError("[Stage0Controller] Photo focus presentation is not assigned.", this);
        }
        else
        {
            try
            {
                await photoFocusPresentation.PlayAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        if (currentState == Stage0State.StartMessage)
        {
            TransitionTo(Stage0State.Playing);
        }
    }

    private void Update()
    {
        if (
            currentState == Stage0State.Playing
            || currentState == Stage0State.CapturedWaitingForTimeout
        )
        {
            UpdateTimer();
        }
    }

    public void BeginCapturedWaitingForTimeout()
    {
        if (currentState != Stage0State.Playing)
        {
            Debug.LogWarning(
                $"[Stage0Controller] Capture request was ignored in {currentState}.",
                this
            );
            return;
        }

        TransitionTo(Stage0State.CapturedWaitingForTimeout);
    }

    public void EnterGameOver()
    {
        if (currentState == Stage0State.GameOver || currentState == Stage0State.Completed)
        {
            return;
        }

        TransitionTo(Stage0State.GameOver);
    }

    private void TransitionTo(Stage0State nextState)
    {
        if (hasInitialized && currentState == nextState)
        {
            return;
        }

        var previousState = currentState;
        currentState = nextState;
        StateChanged?.Invoke(currentState);

        if (nextState == Stage0State.Playing)
        {
            remainingTime = playingDuration;
            UpdateTimerText();
        }

        ApplyUiFor(nextState);
        if (hasInitialized)
        {
            Debug.Log($"[Stage0Controller] {previousState} -> {nextState}", this);
        }
        else
        {
            Debug.Log($"[Stage0Controller] Initial state: {nextState}", this);
            hasInitialized = true;
        }
    }

    private void ApplyUiFor(Stage0State state)
    {
        SetActive(startMessage, state == Stage0State.StartMessage);

        if (state != Stage0State.StartMessage && photoFocusPresentation != null)
        {
            photoFocusPresentation.ResetPresentation();
        }

        if (state == Stage0State.GameOver)
        {
            BeginGameOverPresentation();
            return;
        }

        SetActive(timer, state != Stage0State.StartMessage);
        SetActive(photoFrame, true);
        SetActive(shutterButton, state == Stage0State.Playing);
        ResetGameOverPresentation();
    }

    private void BeginGameOverPresentation()
    {
        if (gameOverPanel == null || gameOverFade == null || gameOverContent == null)
        {
            Debug.LogError("[Stage0Controller] Game over UI is not assigned.", this);
            SetActive(gameOverPanel, true);
            PlayGameOverSE(); // UI未設定時のフォールバック再生
            return;
        }

        SetActive(gameOverPanel, true);
        SetActive(gameOverContent, false);
        gameOverFade.alpha = 0f;
        RunGameOverPresentationAsync(destroyCancellationToken).Forget();
    }

    private async UniTask RunGameOverPresentationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var elapsedTime = 0f;
            while (elapsedTime < gameOverFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                gameOverFade.alpha = Mathf.Clamp01(elapsedTime / gameOverFadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        gameOverFade.alpha = 1f;
        SetActive(timer, false);
        SetActive(photoFrame, false);
        SetActive(shutterButton, false);
        SetActive(gameOverContent, true);

        // ★ 暗転（フェード）が完了してゲームオーバー画面が表示された瞬間にSEを鳴らす！
        PlayGameOverSE();
    }

    private void ResetGameOverPresentation()
    {
        if (gameOverFade != null)
        {
            gameOverFade.alpha = 1f;
        }

        SetActive(gameOverContent, true);
        SetActive(gameOverPanel, false);
    }

    private void UpdateTimer()
    {
        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimerText();

        if (remainingTime > 0f)
        {
            return;
        }

        if (currentState == Stage0State.Playing)
        {
            EnterGameOver();
            return;
        }

        if (currentState == Stage0State.CapturedWaitingForTimeout)
        {
            TransitionTo(Stage0State.Completed);
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = remainingTime.ToString("0.0");
        }
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    // ★ 安全に SoundManager から SE を呼び出す処理
    private void PlayGameOverSE()
    {
        var soundManager = GetValidSoundManagerInstance();
        if (soundManager != null)
        {
            var method = soundManager
                .GetType()
                .GetMethod("PlaySE", new[] { typeof(int), typeof(float), typeof(float) });
            if (method != null)
            {
                method.Invoke(
                    soundManager,
                    new object[] { _gameOverSeIndex, _gameOverSePitch, _gameOverSeVolume }
                );
            }
            else
            {
                soundManager.SendMessage(
                    "PlaySE",
                    _gameOverSeIndex,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }

    // ★ 本物の SoundManager を検索して取得する安全メソッド
    private Component GetValidSoundManagerInstance()
    {
        GameObject soundObj = GameObject.Find("Sound_Manager");
        if (soundObj == null)
            return null;

        var comp = soundObj.GetComponent("SoundManager");
        if (comp == null)
            return null;

        var instanceProp = comp.GetType()
            .GetProperty(
                "Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );
        if (instanceProp != null)
        {
            var activeInstance = instanceProp.GetValue(null) as Component;
            if (activeInstance != null)
            {
                return activeInstance;
            }
        }

        return comp;
    }
}

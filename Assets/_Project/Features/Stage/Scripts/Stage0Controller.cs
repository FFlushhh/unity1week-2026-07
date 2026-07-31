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
        GameOver,
    }

    [Header("State Settings")]
    [SerializeField, Min(0f)]
    private float startMessageDuration = 2f;

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

    [SerializeField]
    private TMP_Text timerText;

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

        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(startMessageDuration),
                cancellationToken: cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            return;
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
            || (currentState == Stage0State.CapturedWaitingForTimeout && remainingTime > 0f)
        )
        {
            UpdateTimer();
        }
    }

    /// <summary>
    /// 撮影成功後、残り時間が終了するまで待つ状態へ移行します。
    /// シャッター入力の実装を追加する後続ステップから呼び出します。
    /// </summary>
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

    /// <summary>
    /// 制限時間終了時にGame Over状態へ移行します。
    /// カウントダウンを実装する後続ステップから呼び出します。
    /// </summary>
    public void EnterGameOver()
    {
        if (currentState == Stage0State.GameOver)
        {
            return;
        }

        TransitionTo(Stage0State.GameOver);
    }

    private void TransitionTo(Stage0State nextState)
    {
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
        var isGameplayVisible = state != Stage0State.GameOver;

        SetActive(startMessage, state == Stage0State.StartMessage);
        SetActive(timer, isGameplayVisible);
        SetActive(photoFrame, isGameplayVisible);
        SetActive(shutterButton, state == Stage0State.Playing);
        SetActive(gameOverPanel, state == Stage0State.GameOver);
    }

    private void UpdateTimer()
    {
        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimerText();

        if (remainingTime <= 0f && currentState == Stage0State.Playing)
        {
            EnterGameOver();
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
}

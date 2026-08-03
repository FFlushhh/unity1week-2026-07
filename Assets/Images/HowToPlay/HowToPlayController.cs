using UnityEngine;

public class HowToPlayController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private GameObject _howToPlayPanel; // 遊び方画面の親オブジェクト

    [Header("Audio (SoundManager Settings)")]
    [SerializeField]
    private bool _playSE = true;

    [SerializeField]
    private int _seOpenIndex = 15; // 開くときのSEインデックス（例: Pop音など）

    [SerializeField]
    private int _seCloseIndex = 16; // 閉じるときのSEインデックス（例: キャンセル音など）

    [SerializeField]
    private float _seVolumeScale = 0.3f; // SEの音量スケール

    private void Start()
    {
        // 起動時は確実に閉じておく
        if (_howToPlayPanel != null)
        {
            _howToPlayPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 「遊び方」ボタンから呼び出す処理
    /// </summary>
    public void OpenHowToPlay()
    {
        if (_howToPlayPanel != null)
        {
            _howToPlayPanel.SetActive(true);
            PlaySound(_seOpenIndex);
        }
    }

    /// <summary>
    /// 「閉じる」ボタンから呼び出す処理
    /// </summary>
    public void CloseHowToPlay()
    {
        if (_howToPlayPanel != null)
        {
            _howToPlayPanel.SetActive(false);
            PlaySound(_seCloseIndex);
        }
    }

    /// <summary>
    /// SoundManager経由でSEを再生します
    /// </summary>
    private void PlaySound(int seIndex)
    {
        if (!_playSE)
            return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(seIndex, 1.0f, _seVolumeScale);
        }
    }
}

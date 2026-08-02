using UnityEngine;

public class SceneSoundManager : MonoBehaviour
{
    [Header("再生するBGMの番号 (SoundManagerのリスト順)")]
    [SerializeField]
    private int _bgmIndex = 0;

    [Header("BGMをループ再生するかどうか")]
    [SerializeField]
    private bool _isLoop = true;

    [Header("ヒエラルキー上のSoundManagerのオブジェクト名")]
    [SerializeField]
    private string _soundManagerObjectName = "Sound_Manager";

    private void Start()
    {
        GameObject soundObj = GameObject.Find(_soundManagerObjectName);
        if (soundObj != null)
        {
            if (_isLoop)
            {
                // ループありで再生
                soundObj.SendMessage("PlayBGM", _bgmIndex, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // ループなし（単発）で再生
                soundObj.SendMessage(
                    "PlayBGMOnce",
                    _bgmIndex,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[SceneSoundManager] '{_soundManagerObjectName}' がヒエラルキーに見つかりません。"
            );
        }
    }

    private void OnDestroy()
    {
        GameObject soundObj = GameObject.Find(_soundManagerObjectName);
        if (soundObj != null)
        {
            soundObj.SendMessage("StopBGM", SendMessageOptions.DontRequireReceiver);
        }
    }
}

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
            // SoundManager 側の PlayBGM または SendMessage でループフラグを渡す
            if (_isLoop)
            {
                soundObj.SendMessage("PlayBGM", _bgmIndex, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // ループなしの場合は、Direct/Instance呼び出し または 専用メソッド
                SoundManager soundMgr = soundObj.GetComponent<SoundManager>();
                if (soundMgr != null)
                {
                    soundMgr.PlayBGMExtended(_bgmIndex, false);
                }
                else
                {
                    soundObj.SendMessage(
                        "PlayBGM",
                        _bgmIndex,
                        SendMessageOptions.DontRequireReceiver
                    );
                }
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

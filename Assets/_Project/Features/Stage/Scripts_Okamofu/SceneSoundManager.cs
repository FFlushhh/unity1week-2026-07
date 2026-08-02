using UnityEngine;

public class SceneSoundManager : MonoBehaviour
{
    [Header("再生するBGMの番号 (SoundManagerのリスト順)")]
    [SerializeField]
    private int _bgmIndex = 0;

    [Header("BGMをループ再生するかどうか")]
    [SerializeField]
    private bool _isLoop = true;

    private void Start()
    {
        var soundManager = GetValidSoundManagerInstance();
        if (soundManager != null)
        {
            if (_isLoop)
            {
                soundManager.SendMessage(
                    "PlayBGM",
                    _bgmIndex,
                    SendMessageOptions.DontRequireReceiver
                );
            }
            else
            {
                soundManager.SendMessage(
                    "PlayBGMOnce",
                    _bgmIndex,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }

    private void OnDestroy()
    {
        var soundManager = GetValidSoundManagerInstance();
        if (soundManager != null)
        {
            soundManager.SendMessage("StopBGM", SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>
    /// Destroy予定の重複オブジェクトを避け、Instanceが保持されている本物のSoundManagerを取得します
    /// </summary>
    private Component GetValidSoundManagerInstance()
    {
        GameObject soundObj = GameObject.Find("Sound_Manager");
        if (soundObj == null)
            return null;

        var comp = soundObj.GetComponent("SoundManager");
        if (comp == null)
            return null;

        // Instance プロパティを取得し、自分自身が Instance であるか（本物か）確認
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
                return activeInstance; // 破棄されない本物を返す
            }
        }

        return comp;
    }
}

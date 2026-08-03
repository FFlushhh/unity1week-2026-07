using UnityEngine;

public class SceneSEManager : MonoBehaviour
{
    [Header("再生するSEの番号 (SoundManagerのリスト順)")]
    [SerializeField]
    private int _seIndex = 0;

    [Header("ピッチ")]
    [SerializeField]
    private float _pitch = 1.0f;

    [Header("音量")]
    [SerializeField]
    private float _volumeScale = 1.0f;

    [Header("再生までの遅延時間（秒）")]
    [SerializeField]
    private float _delay = 0f;

    private void Start()
    {
        if (_delay > 0f)
        {
            Invoke(nameof(PlaySE), _delay);
        }
        else
        {
            PlaySE();
        }
    }

    public void PlaySE()
    {
        var soundManager = GetValidSoundManagerInstance();
        if (soundManager != null)
        {
            // SoundManager.PlaySE(int, float, float) を安全に呼び出す
            var method = soundManager
                .GetType()
                .GetMethod("PlaySE", new[] { typeof(int), typeof(float), typeof(float) });
            if (method != null)
            {
                method.Invoke(soundManager, new object[] { _seIndex, _pitch, _volumeScale });
            }
            else
            {
                // 万が一引数1つの PlaySE しかない場合のフォールバック
                soundManager.SendMessage(
                    "PlaySE",
                    _seIndex,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }

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

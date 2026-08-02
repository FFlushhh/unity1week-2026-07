using UnityEngine;
using UnityEngine.UI;

namespace Title
{
    /// <summary>
    /// タイトル画面用の音量設定UI制御スクリプト
    /// </summary>
    public class TitleVolumeSettingsUI : MonoBehaviour
    {
        public Slider bgmSlider;
        public Slider seSlider;

        private void Start()
        {
            if (SoundManager.Instance != null)
            {
                if (bgmSlider != null)
                {
                    bgmSlider.value = Mathf.RoundToInt(
                        SoundManager.Instance.MasterBgmVolume * 100f
                    );
                    bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
                }

                if (seSlider != null)
                {
                    seSlider.value = Mathf.RoundToInt(SoundManager.Instance.MasterSeVolume * 100f);
                    seSlider.onValueChanged.AddListener(OnSeVolumeChanged);
                }
            }
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                // 0~100を0.0~1.0に変換
                SoundManager.Instance.SetMasterBGMVolume(value / 100f);
            }
        }

        private void OnSeVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                // 0~100を0.0~1.0に変換
                SoundManager.Instance.SetMasterSEVolume(value / 100f);
            }
        }
    }
}

using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("音源リストのアセット")]
    [SerializeField]
    private AudioDataList _audioDataList;

    [Header("再生用 AudioSource")]
    [SerializeField]
    private AudioSource _bgmAudioSource;

    [SerializeField]
    private AudioSource _seAudioSource;

    private AudioSource _pitchedSeAudioSource;

    [Header("全体音量設定 (0.0 ～ 1.0)")]
    [SerializeField, Range(0f, 1f)]
    private float _masterBgmVolume = 1.0f;

    [SerializeField, Range(0f, 1f)]
    private float _masterSeVolume = 1.0f;

    private int _currentBgmIndex = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_bgmAudioSource == null)
            _bgmAudioSource = gameObject.AddComponent<AudioSource>();
        if (_seAudioSource == null)
            _seAudioSource = gameObject.AddComponent<AudioSource>();

        if (_pitchedSeAudioSource == null)
            _pitchedSeAudioSource = gameObject.AddComponent<AudioSource>();

        _bgmAudioSource.loop = true;
    }

    /// <summary>
    /// SE（効果音）を番号指定で再生
    /// </summary>
    /// <param name="index">リストの要素番号 (0, 1, 2...)</param>
    public void PlaySE(int index, float pitch = 1.0f)
    {
        if (_audioDataList == null || index < 0 || index >= _audioDataList.seList.Count)
        {
            Debug.LogWarning($"[SoundManager] SE番号 {index} は無効です。");
            return;
        }

        AudioItem item = _audioDataList.seList[index];
        if (item.clip != null)
        {
            float finalVolume = _masterSeVolume * item.volume;

            if (Mathf.Approximately(pitch, 1.0f))
            {
                // 通常のSEは共通のAudioSourceで再生
                _seAudioSource.pitch = 1.0f;
                _seAudioSource.PlayOneShot(item.clip, finalVolume);
            }
            else
            {
                // ピッチが変更されている場合は専用のAudioSourceで再生（他のSEに影響を与えないため）
                _pitchedSeAudioSource.pitch = pitch;
                _pitchedSeAudioSource.PlayOneShot(item.clip, finalVolume);
            }
        }
    }

    /// <summary>
    /// BGMを番号指定で再生
    /// </summary>
    /// <param name="index">リストの要素番号 (0, 1, 2...)</param>
    public void PlayBGM(int index)
    {
        if (_audioDataList == null || index < 0 || index >= _audioDataList.bgmList.Count)
        {
            Debug.LogWarning($"[SoundManager] BGM番号 {index} は無効です。");
            return;
        }

        // すでに同じ曲が流れている場合は何もしない
        if (_currentBgmIndex == index && _bgmAudioSource.isPlaying)
            return;

        AudioItem item = _audioDataList.bgmList[index];
        if (item.clip != null)
        {
            _currentBgmIndex = index;
            _bgmAudioSource.clip = item.clip;

            // 全体BGM音量 × 音源固有の音量倍率
            _bgmAudioSource.volume = _masterBgmVolume * item.volume;
            _bgmAudioSource.Play();
        }
    }

    /// <summary>
    /// BGMの停止
    /// </summary>
    public void StopBGM()
    {
        _bgmAudioSource.Stop();
        _currentBgmIndex = -1;
    }

    #region 全体音量設定用プロパティ（オプション設定UIなどから操作用）
    /// <summary>
    /// マスターBGM音量の変更 (0.0 ～ 1.0)
    /// </summary>
    public void SetMasterBGMVolume(float volume)
    {
        _masterBgmVolume = Mathf.Clamp01(volume);
        if (_currentBgmIndex >= 0 && _currentBgmIndex < _audioDataList.bgmList.Count)
        {
            _bgmAudioSource.volume =
                _masterBgmVolume * _audioDataList.bgmList[_currentBgmIndex].volume;
        }
    }

    /// <summary>
    /// マスターSE音量の変更 (0.0 ～ 1.0)
    /// </summary>
    public void SetMasterSEVolume(float volume)
    {
        _masterSeVolume = Mathf.Clamp01(volume);
    }
    #endregion
}

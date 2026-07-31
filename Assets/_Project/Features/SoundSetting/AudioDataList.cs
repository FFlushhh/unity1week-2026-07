using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音源データと個別音量のペア
/// </summary>
[Serializable]
public struct AudioItem
{
    [Tooltip("再生する音源")]
    public AudioClip clip;

    [Tooltip("音源ごとの個別音量倍率 (0.0 ～ 2.0程度)")]
    [Range(0f, 2f)]
    public float volume;

    // インスペクター上で初期値 1.0 になるように設定
    public static AudioItem Default => new AudioItem { volume = 1f };
}

[CreateAssetMenu(fileName = "AudioDataList", menuName = "Scriptable Objects/AudioDataList")]
public class AudioDataList : ScriptableObject
{
    [Header("BGMリスト (インデックス番号で管理)")]
    public List<AudioItem> bgmList = new List<AudioItem>();

    [Header("SEリスト (インデックス番号で管理)")]
    public List<AudioItem> seList = new List<AudioItem>();
}

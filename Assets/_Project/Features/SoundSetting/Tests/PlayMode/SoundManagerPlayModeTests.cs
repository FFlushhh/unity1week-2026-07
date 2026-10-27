using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SoundManagerPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<AudioClip> audioClips = new();
    private AudioDataList audioDataList;
    private GameObject soundManagerObject;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        soundManagerObject = new GameObject(nameof(SoundManagerPlayModeTests));
        if (Object.FindAnyObjectByType<AudioListener>() == null)
        {
            soundManagerObject.AddComponent<AudioListener>();
        }

        var soundManager = soundManagerObject.AddComponent<SoundManager>();

        audioDataList = ScriptableObject.CreateInstance<AudioDataList>();
        audioDataList.bgmList = new List<AudioItem>
        {
            CreateAudioItem("FirstBgm"),
            CreateAudioItem("SecondBgm"),
        };
        audioDataList.seList = new List<AudioItem>
        {
            CreateAudioItem("FirstSe"),
            CreateAudioItem("SecondSe"),
        };

        SetPrivateField(soundManager, "_audioDataList", audioDataList);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (soundManagerObject != null)
        {
            Object.Destroy(soundManagerObject);
        }

        if (audioDataList != null)
        {
            Object.Destroy(audioDataList);
        }

        foreach (var audioClip in audioClips)
        {
            Object.Destroy(audioClip);
        }

        audioClips.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayBgmRestoresLoopAfterAOneShotBgm()
    {
        var soundManager = soundManagerObject.GetComponent<SoundManager>();
        var bgmAudioSource = GetPrivateField<AudioSource>(soundManager, "_bgmAudioSource");

        soundManager.PlayBGMOnce(0);
        yield return null;

        Assert.That(bgmAudioSource.loop, Is.False);

        soundManager.PlayBGM(1);

        Assert.That(bgmAudioSource.clip, Is.SameAs(audioDataList.bgmList[1].clip));
        Assert.That(bgmAudioSource.loop, Is.True);
    }

    [Test]
    public void PlayBgmUsesTheMasterAndItemVolumes()
    {
        var soundManager = soundManagerObject.GetComponent<SoundManager>();
        var bgmAudioSource = GetPrivateField<AudioSource>(soundManager, "_bgmAudioSource");
        audioDataList.bgmList[0] = new AudioItem
        {
            clip = audioDataList.bgmList[0].clip,
            volume = 0.4f,
        };

        soundManager.SetMasterBGMVolume(0.5f);
        soundManager.PlayBGM(0);

        Assert.That(bgmAudioSource.volume, Is.EqualTo(0.2f).Within(0.0001f));

        soundManager.SetMasterBGMVolume(2f);

        Assert.That(bgmAudioSource.volume, Is.EqualTo(0.4f).Within(0.0001f));
    }

    [Test]
    public void InvalidOrMissingBgmDoesNotChangeTheCurrentBgm()
    {
        var soundManager = soundManagerObject.GetComponent<SoundManager>();
        var bgmAudioSource = GetPrivateField<AudioSource>(soundManager, "_bgmAudioSource");
        soundManager.PlayBGM(0);

        var currentClip = bgmAudioSource.clip;
        LogAssert.Expect(new Regex("\\[SoundManager\\] BGM番号 -1 は無効です。"));
        soundManager.PlayBGM(-1);

        Assert.That(bgmAudioSource.clip, Is.SameAs(currentClip));

        audioDataList.bgmList[1] = new AudioItem { volume = 1f };
        LogAssert.Expect(new Regex("\\[SoundManager\\] BGM番号 1 のAudioClipが未設定です。"));
        soundManager.PlayBGM(1);

        Assert.That(bgmAudioSource.clip, Is.SameAs(currentClip));
    }

    [Test]
    public void PlaySeUsesIndependentAudioSourcesForNormalAndPitchedPlayback()
    {
        var soundManager = soundManagerObject.GetComponent<SoundManager>();
        var seAudioSource = GetPrivateField<AudioSource>(soundManager, "_seAudioSource");
        var pitchedSeAudioSource = GetPrivateField<AudioSource>(
            soundManager,
            "_pitchedSeAudioSource"
        );
        seAudioSource.pitch = 0.5f;

        soundManager.PlaySE(0);

        Assert.That(seAudioSource.pitch, Is.EqualTo(1f));

        seAudioSource.pitch = 0.5f;
        soundManager.PlaySE(1, 1.5f, 0.5f);

        Assert.That(seAudioSource.pitch, Is.EqualTo(0.5f));
        Assert.That(pitchedSeAudioSource.pitch, Is.EqualTo(1.5f));
    }

    [Test]
    public void InvalidOrMissingSeLogsWarningsWithoutChangingAudioSourcePitch()
    {
        var soundManager = soundManagerObject.GetComponent<SoundManager>();
        var seAudioSource = GetPrivateField<AudioSource>(soundManager, "_seAudioSource");
        var pitchedSeAudioSource = GetPrivateField<AudioSource>(
            soundManager,
            "_pitchedSeAudioSource"
        );
        seAudioSource.pitch = 0.75f;
        pitchedSeAudioSource.pitch = 1.25f;

        LogAssert.Expect(new Regex("\\[SoundManager\\] SE番号 -1 は無効です。"));
        soundManager.PlaySE(-1);

        Assert.That(seAudioSource.pitch, Is.EqualTo(0.75f));
        Assert.That(pitchedSeAudioSource.pitch, Is.EqualTo(1.25f));

        audioDataList.seList[1] = new AudioItem { volume = 1f };
        LogAssert.Expect(new Regex("\\[SoundManager\\] SE番号 1 のAudioClipが未設定です。"));
        soundManager.PlaySE(1, 1.5f);

        Assert.That(seAudioSource.pitch, Is.EqualTo(0.75f));
        Assert.That(pitchedSeAudioSource.pitch, Is.EqualTo(1.25f));
    }

    [UnityTest]
    public IEnumerator StopPitchedSeDoesNotStopTheNormalSeAudioSource()
    {
        var soundManager = soundManagerObject.GetComponent<SoundManager>();
        var seAudioSource = GetPrivateField<AudioSource>(soundManager, "_seAudioSource");
        var pitchedSeAudioSource = GetPrivateField<AudioSource>(
            soundManager,
            "_pitchedSeAudioSource"
        );
        seAudioSource.clip = audioDataList.seList[0].clip;
        pitchedSeAudioSource.clip = audioDataList.seList[1].clip;
        seAudioSource.Play();
        pitchedSeAudioSource.Play();
        yield return null;

        soundManager.StopPitchedSE();

        Assert.That(seAudioSource.isPlaying, Is.True);
        Assert.That(pitchedSeAudioSource.isPlaying, Is.False);
    }

    [UnityTest]
    public IEnumerator DuplicateSoundManagerKeepsTheOriginalInstance()
    {
        var originalInstance = soundManagerObject.GetComponent<SoundManager>();
        var duplicateObject = new GameObject("DuplicateSoundManager");
        duplicateObject.AddComponent<SoundManager>();

        yield return null;

        Assert.That(SoundManager.Instance, Is.SameAs(originalInstance));
        Assert.That(duplicateObject == null, Is.True);
    }

    private AudioItem CreateAudioItem(string clipName)
    {
        var audioClip = AudioClip.Create(clipName, 44100, 1, 44100, false);
        audioClips.Add(audioClip);
        return new AudioItem { clip = audioClip, volume = 1f };
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }
}

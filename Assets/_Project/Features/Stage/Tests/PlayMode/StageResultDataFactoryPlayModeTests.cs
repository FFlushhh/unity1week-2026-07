using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ResultScene;
using UnityEngine;

public sealed class StageResultDataFactoryPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private readonly List<Texture2D> createdTextures = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();

        foreach (var createdTexture in createdTextures)
        {
            if (createdTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(createdTexture);
            }
        }

        createdTextures.Clear();
    }

    [Test]
    public void CreatesOnlyCapturedDogAndBirdInFixedDisplayOrder()
    {
        var capturedPhoto = CreateCapturedPhoto(SubjectId.Dog, SubjectId.Dog, SubjectId.Bird);

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 1");

        Assert.That(resultData.Bonuses, Has.Count.EqualTo(2));
        Assert.That(resultData.Bonuses[0].BonusName, Is.EqualTo("犬"));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(2));
        Assert.That(resultData.Bonuses[1].BonusName, Is.EqualTo("ハト"));
        Assert.That(resultData.Bonuses[1].Count, Is.EqualTo(1));
    }

    [TestCase(SubjectId.Dog, "犬")]
    [TestCase(SubjectId.DirtyClothesPerson, "汚れた服の人")]
    [TestCase(SubjectId.RabidDog, "狂犬")]
    [TestCase(SubjectId.PlasticBag, "ビニール袋")]
    [TestCase(SubjectId.Bird, "ハト")]
    [TestCase(SubjectId.Sparrow, "青鳥")]
    [TestCase(SubjectId.SelfieGirl, "自撮り")]
    public void UsesResultBonusNameForEverySubjectId(SubjectId subjectId, string expectedBonusName)
    {
        var capturedPhoto = CreateCapturedPhoto(subjectId);

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 1");

        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses[0].BonusName, Is.EqualTo(expectedBonusName));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(1));
    }

    [Test]
    public void CreatesBonusesForAllSevenSubjectsInFixedDisplayOrderWhenAllCaptured()
    {
        var capturedPhoto = CreateCapturedPhoto(
            SubjectId.Dog,
            SubjectId.DirtyClothesPerson,
            SubjectId.RabidDog,
            SubjectId.PlasticBag,
            SubjectId.Bird,
            SubjectId.Sparrow,
            SubjectId.SelfieGirl
        );

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 1");

        var expectedNamesInOrder = new[]
        {
            "犬",
            "汚れた服の人",
            "狂犬",
            "ビニール袋",
            "ハト",
            "青鳥",
            "自撮り",
        };
        Assert.That(resultData.Bonuses, Has.Count.EqualTo(expectedNamesInOrder.Length));
        for (var i = 0; i < expectedNamesInOrder.Length; i++)
        {
            Assert.That(resultData.Bonuses[i].BonusName, Is.EqualTo(expectedNamesInOrder[i]));
            Assert.That(resultData.Bonuses[i].Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void DoesNotIncludeSelfieGirlBonusWhenNotCaptured()
    {
        var capturedPhoto = CreateCapturedPhoto(SubjectId.Dog);

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 1");

        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses.Exists(bonus => bonus.BonusName == "自撮り"), Is.False);
    }

    [Test]
    public void CreatesEmptyNonNullBonusesWhenNoSubjectsWereCaptured()
    {
        var capturedPhoto = CreateCapturedPhoto();

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 1");

        Assert.That(resultData.Bonuses, Is.Not.Null);
        Assert.That(resultData.Bonuses, Is.Empty);
    }

    [Test]
    public void CopiesDisplayFieldsAndImageReferenceWithoutCalculatingScore()
    {
        var capturedPhoto = CreateCapturedPhoto(SubjectId.PlasticBag);

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 1");

        Assert.That(resultData.PlayerName, Is.EqualTo("プレイヤー"));
        Assert.That(resultData.LocationName, Is.EqualTo("Stage 1"));
        Assert.That(resultData.CapturedImage, Is.SameAs(capturedPhoto.Image));
        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(1));
    }

    [Test]
    public void ThrowsForMissingCapturedPhoto()
    {
        Assert.That(
            () => StageResultDataFactory.Create(null, "プレイヤー", "Stage 1"),
            Throws.TypeOf<ArgumentNullException>()
        );
    }

    private CapturedPhoto CreateCapturedPhoto(params SubjectId[] subjectIds)
    {
        var image = new Texture2D(2, 2);
        createdTextures.Add(image);
        var subjects = new List<StageSubject>();
        foreach (var subjectId in subjectIds)
        {
            subjects.Add(CreateSubject(subjectId));
        }

        return new CapturedPhoto(image, subjects);
    }

    private StageSubject CreateSubject(SubjectId subjectId)
    {
        var subjectObject = new GameObject($"{subjectId}Subject");
        createdObjects.Add(subjectObject);
        var subject = subjectObject.AddComponent<StageSubject>();
        var subjectIdField = typeof(StageSubject).GetField("subjectId", PrivateInstance);
        Assert.That(subjectIdField, Is.Not.Null);
        subjectIdField.SetValue(subject, subjectId);
        return subject;
    }
}

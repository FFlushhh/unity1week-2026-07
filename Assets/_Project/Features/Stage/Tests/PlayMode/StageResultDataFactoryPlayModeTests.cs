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

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 0");

        Assert.That(resultData.Bonuses, Has.Count.EqualTo(2));
        Assert.That(resultData.Bonuses[0].BonusName, Is.EqualTo("犬"));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(2));
        Assert.That(resultData.Bonuses[1].BonusName, Is.EqualTo("鳥"));
        Assert.That(resultData.Bonuses[1].Count, Is.EqualTo(1));
    }

    [TestCase(SubjectId.Dog, "犬")]
    [TestCase(SubjectId.DirtyClothesPerson, "汚れた服の人")]
    [TestCase(SubjectId.RabidDog, "狂犬")]
    [TestCase(SubjectId.PlasticBag, "ビニール袋")]
    [TestCase(SubjectId.Bird, "鳥")]
    [TestCase(SubjectId.Sparrow, "スズメ")]
    public void UsesResultBonusNameForEverySubjectId(SubjectId subjectId, string expectedBonusName)
    {
        var capturedPhoto = CreateCapturedPhoto(subjectId);

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 0");

        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses[0].BonusName, Is.EqualTo(expectedBonusName));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(1));
    }

    [Test]
    public void CreatesEmptyNonNullBonusesWhenNoSubjectsWereCaptured()
    {
        var capturedPhoto = CreateCapturedPhoto();

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 0");

        Assert.That(resultData.Bonuses, Is.Not.Null);
        Assert.That(resultData.Bonuses, Is.Empty);
    }

    [Test]
    public void CopiesDisplayFieldsAndImageReferenceWithoutCalculatingScore()
    {
        var capturedPhoto = CreateCapturedPhoto(SubjectId.PlasticBag);

        var resultData = StageResultDataFactory.Create(capturedPhoto, "プレイヤー", "Stage 0");

        Assert.That(resultData.PlayerName, Is.EqualTo("プレイヤー"));
        Assert.That(resultData.LocationName, Is.EqualTo("Stage 0"));
        Assert.That(resultData.CapturedImage, Is.SameAs(capturedPhoto.Image));
        Assert.That(resultData.BaseScore, Is.EqualTo(1000));
        Assert.That(resultData.Bonuses, Has.Count.EqualTo(1));
        Assert.That(resultData.Bonuses[0].Count, Is.EqualTo(1));
    }

    [Test]
    public void ThrowsForMissingCapturedPhoto()
    {
        Assert.That(
            () => StageResultDataFactory.Create(null, "プレイヤー", "Stage 0"),
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

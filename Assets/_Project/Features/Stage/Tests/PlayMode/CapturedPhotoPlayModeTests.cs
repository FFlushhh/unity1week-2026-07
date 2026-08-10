using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CapturedPhotoPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var createdObject in createdObjects)
        {
            Object.DestroyImmediate(createdObject);
        }

        createdObjects.Clear();
    }

    [Test]
    public void CountsSubjectsByIdAndReturnsZeroForAnIdThatWasNotCaptured()
    {
        var image = new Texture2D(2, 2);
        var dog = CreateSubject(SubjectId.Dog);
        var secondDog = CreateSubject(SubjectId.Dog);
        var bird = CreateSubject(SubjectId.Bird);

        var capturedPhoto = new CapturedPhoto(image, new[] { dog, secondDog, bird, null });

        Assert.That(capturedPhoto.Image, Is.EqualTo(image));
        Assert.That(capturedPhoto.GetSubjectCount(SubjectId.Dog), Is.EqualTo(2));
        Assert.That(capturedPhoto.GetSubjectCount(SubjectId.Bird), Is.EqualTo(1));
        Assert.That(capturedPhoto.GetSubjectCount(SubjectId.RabidDog), Is.EqualTo(0));
        Assert.That(capturedPhoto.SubjectCounts, Has.Count.EqualTo(2));

        Object.DestroyImmediate(image);
    }

    [Test]
    public void AllowsAnEmptySubjectCollection()
    {
        var image = new Texture2D(2, 2);

        var capturedPhoto = new CapturedPhoto(image, null);

        Assert.That(capturedPhoto.SubjectCounts, Is.Empty);
        Assert.That(capturedPhoto.GetSubjectCount(SubjectId.Sparrow), Is.EqualTo(0));

        Object.DestroyImmediate(image);
    }

    [Test]
    public void StoresTheForcedZeroScoreFlag()
    {
        var image = new Texture2D(2, 2);

        var capturedPhoto = new CapturedPhoto(image, null, true);

        Assert.That(capturedPhoto.IsScoreForcedToZero, Is.True);

        Object.DestroyImmediate(image);
    }

    private StageSubject CreateSubject(SubjectId subjectId)
    {
        var subjectObject = new GameObject($"{subjectId}Subject");
        createdObjects.Add(subjectObject);
        var subject = subjectObject.AddComponent<StageSubject>();
        var field = typeof(StageSubject).GetField("subjectId", PrivateInstance);
        Assert.That(field, Is.Not.Null);
        field.SetValue(subject, subjectId);
        return subject;
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class StageSubjectPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject subjectObject;

    [TearDown]
    public void TearDown()
    {
        if (subjectObject != null)
        {
            Object.DestroyImmediate(subjectObject);
        }
    }

    [TestCase(SubjectId.Dog, 500)]
    [TestCase(SubjectId.DirtyClothesPerson, -600)]
    [TestCase(SubjectId.RabidDog, -800)]
    [TestCase(SubjectId.PlasticBag, -100)]
    [TestCase(SubjectId.Bird, 800)]
    [TestCase(SubjectId.Sparrow, 5)]
    public void ReturnsConfiguredIdAndScore(SubjectId subjectId, int score)
    {
        subjectObject = new GameObject("Subject");
        var stageSubject = subjectObject.AddComponent<StageSubject>();
        SetPrivateField(stageSubject, "subjectId", subjectId);
        SetPrivateField(stageSubject, "score", score);

        Assert.That(stageSubject.Id, Is.EqualTo(subjectId));
        Assert.That(stageSubject.Score, Is.EqualTo(score));
    }

    private static void SetPrivateField<T>(StageSubject stageSubject, string fieldName, T value)
    {
        var field = typeof(StageSubject).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(stageSubject, value);
    }
}

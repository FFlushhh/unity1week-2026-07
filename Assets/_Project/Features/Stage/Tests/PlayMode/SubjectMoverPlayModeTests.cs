using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SubjectMoverPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject subject;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (subject != null)
        {
            Object.DestroyImmediate(subject);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator MovesRightBeforeReachingDespawnPosition()
    {
        var mover = CreateMover(positionX: -1f, moveSpeed: 2f);
        var initialPositionX = subject.transform.position.x;

        yield return null;

        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.transform.position.x, Is.GreaterThan(initialPositionX));
        Assert.That(mover, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator MovesLeftWhenConfiguredForRightToLeft()
    {
        var mover = CreateMover(positionX: 1f, moveSpeed: 2f);
        mover.Configure(SubjectMoveDirection.RightToLeft, 2f);
        var initialPositionX = subject.transform.position.x;

        yield return null;

        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.transform.position.x, Is.LessThan(initialPositionX));
    }

    [UnityTest]
    public IEnumerator ZeroMoveSpeedKeepsSubjectAtItsCurrentPosition()
    {
        CreateMover(positionX: -1f, moveSpeed: 0f);

        yield return null;

        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.transform.position.x, Is.EqualTo(-1f));
    }

    [UnityTest]
    public IEnumerator StopKeepsSubjectAtItsCurrentPosition()
    {
        var mover = CreateMover(positionX: -1f, moveSpeed: 2f);
        mover.Stop();
        var stoppedPositionX = subject.transform.position.x;

        yield return null;

        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.transform.position.x, Is.EqualTo(stoppedPositionX));
    }

    [UnityTest]
    public IEnumerator ReachingRightDespawnPositionDestroysSubject()
    {
        CreateMover(positionX: 10f, moveSpeed: 2f);

        yield return null;

        Assert.That(subject, Is.Null);
    }

    [UnityTest]
    public IEnumerator ReachingLeftDespawnPositionDestroysSubject()
    {
        var mover = CreateMover(positionX: -10f, moveSpeed: 2f);
        mover.Configure(SubjectMoveDirection.RightToLeft, 2f);

        yield return null;

        Assert.That(subject, Is.Null);
    }

    private SubjectMover CreateMover(float positionX, float moveSpeed)
    {
        subject = new GameObject("Subject");
        subject.transform.position = new Vector3(positionX, 0f, 0f);

        var mover = subject.AddComponent<SubjectMover>();
        SetPrivateField(mover, "moveSpeed", moveSpeed);
        SetPrivateField(mover, "despawnPositionX", 10f);

        return mover;
    }

    private static void SetPrivateField<T>(SubjectMover mover, string fieldName, T value)
    {
        var field = typeof(SubjectMover).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(mover, value);
    }
}

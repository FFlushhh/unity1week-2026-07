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
    public IEnumerator ConfiguredVerticalSwayStaysWithinItsAmplitude()
    {
        var mover = CreateMover(positionX: -1f, moveSpeed: 0f);
        mover.Configure(SubjectMoveDirection.LeftToRight, 0f, 0.5f, 1f);
        var basePositionY = subject.transform.position.y;
        var maximumOffset = 0f;

        for (var frame = 0; frame < 10; frame++)
        {
            yield return null;
            maximumOffset = Mathf.Max(
                maximumOffset,
                Mathf.Abs(subject.transform.position.y - basePositionY)
            );
        }

        Assert.That(maximumOffset, Is.GreaterThan(0.001f));
        Assert.That(maximumOffset, Is.LessThanOrEqualTo(0.5001f));
        Assert.That(subject.transform.position.x, Is.EqualTo(-1f));
    }

    [UnityTest]
    public IEnumerator NegativeVerticalSwayValuesKeepSubjectAtItsBaseHeight()
    {
        var mover = CreateMover(positionX: -1f, moveSpeed: 0f);
        mover.Configure(SubjectMoveDirection.LeftToRight, 0f, -0.5f, -1f);
        var basePositionY = subject.transform.position.y;

        for (var frame = 0; frame < 3; frame++)
        {
            yield return null;
        }

        Assert.That(subject.transform.position.y, Is.EqualTo(basePositionY));
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

        yield return WaitForSubjectDestruction();
    }

    [UnityTest]
    public IEnumerator ReachingLeftDespawnPositionDestroysSubject()
    {
        var mover = CreateMover(positionX: -10f, moveSpeed: 2f);
        mover.Configure(SubjectMoveDirection.RightToLeft, 2f);

        yield return WaitForSubjectDestruction();
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

    private IEnumerator WaitForSubjectDestruction()
    {
        const float timeoutSeconds = 1f;
        var timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (subject != null && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        Assert.That(
            subject == null,
            Is.True,
            "Subject was not destroyed after reaching the despawn position."
        );
    }

    private static void SetPrivateField<T>(SubjectMover mover, string fieldName, T value)
    {
        var field = typeof(SubjectMover).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(mover, value);
    }
}

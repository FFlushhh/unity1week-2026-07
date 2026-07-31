using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class MoveRightAndDestroyPlayModeTests
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
    public IEnumerator MovesRightBeforeReachingDestroyPosition()
    {
        var mover = CreateMover(positionX: -1f, moveSpeed: 2f, destroyPositionX: 10f);
        var initialPositionX = subject.transform.position.x;

        yield return null;

        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.transform.position.x, Is.GreaterThan(initialPositionX));
        Assert.That(mover, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator ZeroMoveSpeedKeepsSubjectAtItsCurrentPosition()
    {
        CreateMover(positionX: -1f, moveSpeed: 0f, destroyPositionX: 10f);

        yield return null;

        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.transform.position.x, Is.EqualTo(-1f));
    }

    [UnityTest]
    public IEnumerator ReachingDestroyPositionDestroysSubject()
    {
        CreateMover(positionX: 10f, moveSpeed: 2f, destroyPositionX: 10f);

        yield return null;

        Assert.That(subject, Is.Null);
    }

    private MoveRightAndDestroy CreateMover(
        float positionX,
        float moveSpeed,
        float destroyPositionX
    )
    {
        subject = new GameObject("Subject");
        subject.transform.position = new Vector3(positionX, 0f, 0f);

        var mover = subject.AddComponent<MoveRightAndDestroy>();
        SetPrivateField(mover, "moveSpeed", moveSpeed);
        SetPrivateField(mover, "destroyPositionX", destroyPositionX);

        return mover;
    }

    private static void SetPrivateField<T>(MoveRightAndDestroy mover, string fieldName, T value)
    {
        var field = typeof(MoveRightAndDestroy).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(mover, value);
    }
}

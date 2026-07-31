using UnityEngine;

public enum SubjectMoveDirection
{
    LeftToRight,
    RightToLeft,
}

/// <summary>
/// 被写体を設定された水平方向へ移動し、画面外まで進んだら破棄します。
/// </summary>
public sealed class SubjectMover : MonoBehaviour
{
    [SerializeField]
    private SubjectMoveDirection moveDirection = SubjectMoveDirection.LeftToRight;

    [SerializeField, Min(0f)]
    private float moveSpeed = 2f;

    [SerializeField, Min(0f)]
    private float despawnPositionX = 10f;

    public void Configure(SubjectMoveDirection direction, float speed)
    {
        moveDirection = direction;
        moveSpeed = Mathf.Max(0f, speed);
    }

    private void Update()
    {
        if (HasReachedDespawnPosition())
        {
            Destroy(gameObject);
            return;
        }

        transform.position += GetMoveDirection() * (moveSpeed * Time.deltaTime);
    }

    private bool HasReachedDespawnPosition()
    {
        return moveDirection == SubjectMoveDirection.LeftToRight
            ? transform.position.x >= despawnPositionX
            : transform.position.x <= -despawnPositionX;
    }

    private Vector3 GetMoveDirection()
    {
        return moveDirection == SubjectMoveDirection.LeftToRight ? Vector3.right : Vector3.left;
    }
}

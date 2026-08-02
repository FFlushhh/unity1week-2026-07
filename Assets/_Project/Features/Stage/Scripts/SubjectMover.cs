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

    [SerializeField, Min(0f)]
    private float verticalSwayAmplitude;

    [SerializeField, Min(0f)]
    private float verticalSwayFrequencyHz;

    private bool isStopped;
    private float verticalSwayBasePositionY;
    private float verticalSwayElapsedSeconds;

    private void Awake()
    {
        verticalSwayBasePositionY = transform.localPosition.y;
    }

    public void Configure(SubjectMoveDirection direction, float speed)
    {
        Configure(direction, speed, verticalSwayAmplitude, verticalSwayFrequencyHz);
    }

    public void Configure(
        SubjectMoveDirection direction,
        float speed,
        float swayAmplitude,
        float swayFrequencyHz
    )
    {
        moveDirection = direction;
        moveSpeed = Mathf.Max(0f, speed);
        verticalSwayAmplitude = Mathf.Max(0f, swayAmplitude);
        verticalSwayFrequencyHz = Mathf.Max(0f, swayFrequencyHz);
        verticalSwayBasePositionY = transform.localPosition.y;
        verticalSwayElapsedSeconds = 0f;
        isStopped = false;
    }

    public void Stop()
    {
        isStopped = true;
    }

    private void Update()
    {
        if (isStopped)
        {
            return;
        }

        if (HasReachedDespawnPosition())
        {
            Destroy(gameObject);
            return;
        }

        transform.position += GetMoveDirection() * (moveSpeed * Time.deltaTime);
        ApplyVerticalSway();
    }

    private void ApplyVerticalSway()
    {
        if (verticalSwayAmplitude <= 0f || verticalSwayFrequencyHz <= 0f)
        {
            return;
        }

        verticalSwayElapsedSeconds += Time.deltaTime;
        var localPosition = transform.localPosition;
        localPosition.y =
            verticalSwayBasePositionY
            + verticalSwayAmplitude
                * Mathf.Sin(2f * Mathf.PI * verticalSwayFrequencyHz * verticalSwayElapsedSeconds);
        transform.localPosition = localPosition;
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

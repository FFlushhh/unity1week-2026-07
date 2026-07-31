using UnityEngine;

/// <summary>
/// 仮被写体を右方向へ移動し、画面外まで進んだら破棄します。
/// </summary>
public sealed class MoveRightAndDestroy : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 2f;

    [SerializeField]
    private float destroyPositionX = 10f;

    private void Update()
    {
        if (transform.position.x >= destroyPositionX)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.right * (moveSpeed * Time.deltaTime);
    }
}

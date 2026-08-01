using UnityEngine;

/// <summary>
/// 写真枠の内側に被写体の判断ポイントがあるかを判定します。
/// </summary>
public sealed class PhotoFrameSubjectJudge : MonoBehaviour
{
    private const string LogPrefix = "[PhotoFrameSubjectJudge]";
    private const int MaximumOverlappingColliders = 128;

    [SerializeField]
    private Camera photoCamera;

    [SerializeField]
    private RectTransform photoFrame;

    [SerializeField]
    private LayerMask photoSubjectLayerMask = 1 << 6;

    // 撮影は一度だけだが、最大100体の被写体を判定してもGCを発生させないように再利用する。
    private readonly Collider2D[] overlappingColliders = new Collider2D[
        MaximumOverlappingColliders
    ];

    /// <summary>
    /// 枠線上を枠外として、被写体の判断ポイントが写真枠内かを返します。
    /// </summary>
    public bool IsInsidePhotoFrame(StageSubject subject)
    {
        return subject != null && IsInsidePhotoFrame(subject.JudgementPoint);
    }

    /// <summary>
    /// 判断ポイントが写真枠内にあり、より前面に描画されるCollider2Dに覆われていないかを判定する。
    /// </summary>
    public bool IsCapturable(StageSubject subject)
    {
        return IsInsidePhotoFrame(subject) && IsJudgementPointVisible(subject);
    }

    /// <summary>
    /// 判断ポイントを覆うPhotoSubjectのうち、候補より前面に描画されるものがないかを判定する。
    /// </summary>
    public bool IsJudgementPointVisible(StageSubject subject)
    {
        if (subject == null || subject.JudgementPoint == null)
        {
            return false;
        }

        if (!subject.TryGetSortingPriority(out var subjectLayerValue, out var subjectOrder))
        {
            Debug.LogWarning(
                $"{LogPrefix} {subject.name} has no Subject Renderer. It cannot be a capture target.",
                subject
            );
            return false;
        }

        var contactFilter = ContactFilter2D.noFilter;
        contactFilter.SetLayerMask(photoSubjectLayerMask);
        var overlapCount = Physics2D.OverlapPoint(
            subject.JudgementPoint.position,
            contactFilter,
            overlappingColliders
        );

        if (overlapCount == overlappingColliders.Length)
        {
            Debug.LogWarning(
                $"{LogPrefix} Overlap buffer reached {MaximumOverlappingColliders} colliders. Some occluders may not have been checked.",
                this
            );
        }

        for (var index = 0; index < overlapCount; index++)
        {
            var overlappingCollider = overlappingColliders[index];
            if (
                overlappingCollider == null
                || IsColliderOwnedBySubject(overlappingCollider, subject)
            )
            {
                continue;
            }

            var occluderSubject = overlappingCollider.GetComponentInParent<StageSubject>();
            var occluderRenderer = GetOccluderRenderer(overlappingCollider, occluderSubject);
            if (occluderRenderer == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} {overlappingCollider.name} has a PhotoSubject Collider2D but no SpriteRenderer. It is treated as an occluder.",
                    overlappingCollider
                );
                return false;
            }

            if (!occluderRenderer.enabled || !occluderRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (occluderSubject == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} {overlappingCollider.name} has a PhotoSubject Collider2D and SpriteRenderer but no StageSubject. It is treated as an occluder when drawn in front.",
                    overlappingCollider
                );
            }

            var occluderLayerValue = SortingLayer.GetLayerValueFromID(
                occluderRenderer.sortingLayerID
            );
            var occluderOrder = occluderRenderer.sortingOrder;
            if (IsDrawnInFront(occluderLayerValue, occluderOrder, subjectLayerValue, subjectOrder))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsColliderOwnedBySubject(
        Collider2D overlappingCollider,
        StageSubject subject
    )
    {
        return overlappingCollider.transform.IsChildOf(subject.transform);
    }

    private static SpriteRenderer GetOccluderRenderer(
        Collider2D overlappingCollider,
        StageSubject occluderSubject
    )
    {
        if (occluderSubject != null && occluderSubject.SubjectRenderer != null)
        {
            return occluderSubject.SubjectRenderer;
        }

        return overlappingCollider.GetComponentInParent<SpriteRenderer>();
    }

    private static bool IsDrawnInFront(
        int candidateLayerValue,
        int candidateOrder,
        int subjectLayerValue,
        int subjectOrder
    )
    {
        return candidateLayerValue > subjectLayerValue
            || (candidateLayerValue == subjectLayerValue && candidateOrder > subjectOrder);
    }

    /// <summary>
    /// 枠線上を枠外として、判断ポイントが写真枠内かを返します。
    /// </summary>
    private bool IsInsidePhotoFrame(Transform judgementPoint)
    {
        if (photoCamera == null || photoFrame == null || judgementPoint == null)
        {
            return false;
        }

        var judgementPointViewportPosition = photoCamera.WorldToViewportPoint(
            judgementPoint.position
        );
        if (judgementPointViewportPosition.z <= 0f)
        {
            return false;
        }

        // PhotoPreviewはPhotoCameraのRenderTexture全体を表示しているため、
        // Viewport座標の厳密比較が写真枠内に写るかの判定と一致する。
        return judgementPointViewportPosition.x > 0f
            && judgementPointViewportPosition.x < 1f
            && judgementPointViewportPosition.y > 0f
            && judgementPointViewportPosition.y < 1f;
    }

    /// <summary>
    /// 写真枠の左下・右上をGame View上の画面座標で取得します。
    /// </summary>
    private bool TryGetPhotoFrameScreenCorners(out Vector2 bottomLeft, out Vector2 topRight)
    {
        bottomLeft = default;
        topRight = default;

        var canvas = photoFrame.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        // 写真枠はCanvas直下の固定UIなので、Canvasの実ピクセル領域から求める。
        // Transformのスケールではなく、Gameビューに描画される座標と一致させるためです。
        var canvasRect = canvas.pixelRect;
        var scaleFactor = canvas.scaleFactor;
        bottomLeft = new Vector2(
            canvasRect.xMin
                + (canvasRect.width * photoFrame.anchorMin.x)
                + (photoFrame.offsetMin.x * scaleFactor),
            canvasRect.yMin
                + (canvasRect.height * photoFrame.anchorMin.y)
                + (photoFrame.offsetMin.y * scaleFactor)
        );
        topRight = new Vector2(
            canvasRect.xMin
                + (canvasRect.width * photoFrame.anchorMax.x)
                + (photoFrame.offsetMax.x * scaleFactor),
            canvasRect.yMin
                + (canvasRect.height * photoFrame.anchorMax.y)
                + (photoFrame.offsetMax.y * scaleFactor)
        );

        return bottomLeft.x < topRight.x && bottomLeft.y < topRight.y;
    }

    private bool TryGetJudgementPointScreenPosition(
        Transform judgementPoint,
        out Vector2 screenPosition
    )
    {
        screenPosition = default;

        if (photoCamera == null || judgementPoint == null)
        {
            return false;
        }

        if (!TryGetPhotoFrameScreenCorners(out var bottomLeft, out var topRight))
        {
            return false;
        }

        var viewportPosition = photoCamera.WorldToViewportPoint(judgementPoint.position);
        if (viewportPosition.z <= 0f)
        {
            return false;
        }

        screenPosition = new Vector2(
            Mathf.Lerp(bottomLeft.x, topRight.x, viewportPosition.x),
            Mathf.Lerp(bottomLeft.y, topRight.y, viewportPosition.y)
        );
        return true;
    }

    /// <summary>
    /// 判断ポイントの位置を調整する間だけ、判定結果をConsoleへ出力します。
    /// </summary>
    public bool LogJudgementResult(StageSubject subject)
    {
        var isInside = IsInsidePhotoFrame(subject);
        var subjectName = subject == null ? "Missing subject" : subject.name;
        var screenPosition = default(Vector2);
        var hasScreenPosition =
            subject != null
            && TryGetJudgementPointScreenPosition(subject.JudgementPoint, out screenPosition);
        var screenPositionText = hasScreenPosition
            ? $" JudgementPoint screen position: {screenPosition}."
            : " JudgementPoint screen position is unavailable.";

        Debug.Log(
            $"{LogPrefix} {subjectName} is {(isInside ? "inside" : "outside")} the photo frame.{screenPositionText}",
            this
        );

        return isInside;
    }

    [ContextMenu("Log Active Subject Judgements")]
    private void LogActiveSubjectJudgements()
    {
        // 判断ポイントの調整確認専用であり、通常のゲームループでは検索しません。
        var subjects = FindObjectsByType<StageSubject>(FindObjectsInactive.Exclude);
        foreach (var subject in subjects)
        {
            LogJudgementResult(subject);
        }
    }
}

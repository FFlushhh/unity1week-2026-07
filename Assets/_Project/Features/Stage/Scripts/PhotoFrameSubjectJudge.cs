using UnityEngine;

/// <summary>
/// 写真枠の内側に被写体の判断ポイントがあるかを判定します。
/// </summary>
public sealed class PhotoFrameSubjectJudge : MonoBehaviour
{
    private const string LogPrefix = "[PhotoFrameSubjectJudge]";

    [SerializeField]
    private Camera photoCamera;

    [SerializeField]
    private RectTransform photoFrame;

    /// <summary>
    /// 枠線上を枠外として、被写体の判断ポイントが写真枠内かを返します。
    /// </summary>
    public bool IsInsidePhotoFrame(StageSubject subject)
    {
        return subject != null && IsInsidePhotoFrame(subject.JudgementPoint);
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

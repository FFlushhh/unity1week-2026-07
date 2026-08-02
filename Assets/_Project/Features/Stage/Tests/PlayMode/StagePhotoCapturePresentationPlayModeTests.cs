using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StagePhotoCapturePresentationPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;

        foreach (var createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator ResetPresentationHidesBlackoutAndPreviewAndRestoresInitialScale()
    {
        var presentation = CreatePresentation(new Vector3(1.5f, 0.75f, 1f));
        var blackout = GetPrivateField<CanvasGroup>(presentation, "shutterBlackout");
        var preview = GetPrivateField<RawImage>(presentation, "capturedPhotoPreview");

        presentation.ResetPresentation();
        yield return null;

        Assert.That(blackout.gameObject.activeSelf, Is.False);
        Assert.That(blackout.alpha, Is.EqualTo(0f));
        Assert.That(preview.gameObject.activeSelf, Is.False);
        Assert.That(preview.rectTransform.localScale, Is.EqualTo(new Vector3(1.5f, 0.75f, 1f)));
    }

    [UnityTest]
    public IEnumerator PlayAsyncShowsPreviewOnlyAfterBlackoutHasReturned()
    {
        var presentation = CreatePresentation(Vector3.one);
        SetDurations(presentation, 0.04f, 0.04f, 0.04f, 0.04f);
        var blackout = GetPrivateField<CanvasGroup>(presentation, "shutterBlackout");
        var preview = GetPrivateField<RawImage>(presentation, "capturedPhotoPreview");

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => blackout.gameObject.activeSelf && blackout.alpha > 0f,
            "Blackout did not start."
        );

        Assert.That(preview.gameObject.activeSelf, Is.False);

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not complete."
        );

        Assert.That(blackout.gameObject.activeSelf, Is.False);
        Assert.That(preview.gameObject.activeSelf, Is.True);
        Assert.That(preview.rectTransform.localScale, Is.EqualTo(Vector3.one));
    }

    [UnityTest]
    public IEnumerator ZeroDurationsCompleteWithTheExactInitialPreviewScale()
    {
        var initialScale = new Vector3(1.2f, 0.8f, 1f);
        var presentation = CreatePresentation(initialScale);
        SetDurations(presentation, 0f, 0f, 0f, 0f);
        var preview = GetPrivateField<RawImage>(presentation, "capturedPhotoPreview");

        presentation.PlayAsync(default);
        yield return null;

        Assert.That(presentation.IsPlaying, Is.False);
        Assert.That(preview.gameObject.activeSelf, Is.True);
        Assert.That(preview.rectTransform.localScale, Is.EqualTo(initialScale));
    }

    [UnityTest]
    public IEnumerator PreviewRevealStartsAtZeroScaleBeforeGrowingToItsInitialScale()
    {
        var initialScale = new Vector3(1.5f, 0.75f, 1f);
        var presentation = CreatePresentation(initialScale);
        SetDurations(presentation, 0f, 0f, 0f, 0.04f);
        var preview = GetPrivateField<RawImage>(presentation, "capturedPhotoPreview");

        presentation.PlayAsync(default);

        Assert.That(preview.gameObject.activeSelf, Is.True);
        Assert.That(preview.rectTransform.localScale, Is.EqualTo(Vector3.zero));

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not complete."
        );

        Assert.That(preview.rectTransform.localScale, Is.EqualTo(initialScale));
    }

    [UnityTest]
    public IEnumerator PlayAsyncUsesUnscaledTimeWhenTimeScaleIsZero()
    {
        var presentation = CreatePresentation(Vector3.one);
        SetDurations(presentation, 0.02f, 0.02f, 0.02f, 0.02f);
        Time.timeScale = 0f;

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not complete while timeScale was zero."
        );

        var preview = GetPrivateField<RawImage>(presentation, "capturedPhotoPreview");
        Assert.That(preview.gameObject.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator CancellationHidesBlackoutWithoutLeavingTheScreenBlack()
    {
        var presentation = CreatePresentation(Vector3.one);
        SetDurations(presentation, 1f, 0f, 0f, 0f);
        var blackout = GetPrivateField<CanvasGroup>(presentation, "shutterBlackout");
        using var cancellation = new System.Threading.CancellationTokenSource();

        presentation.PlayAsync(cancellation.Token);
        yield return WaitUntilOrTimeout(
            () => blackout.gameObject.activeSelf && blackout.alpha > 0f,
            "Blackout did not start."
        );

        cancellation.Cancel();
        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not finish after cancellation."
        );

        Assert.That(blackout.gameObject.activeSelf, Is.False);
        Assert.That(blackout.alpha, Is.EqualTo(0f));
    }

    [UnityTest]
    public IEnumerator RepeatedPlayAsyncDoesNotRestartThePresentation()
    {
        var presentation = CreatePresentation(Vector3.one);
        SetDurations(presentation, 0.1f, 0.02f, 0.02f, 0.02f);
        var blackout = GetPrivateField<CanvasGroup>(presentation, "shutterBlackout");

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => blackout.gameObject.activeSelf && blackout.alpha > 0f,
            "Blackout did not start."
        );

        var alphaBeforeSecondCall = blackout.alpha;
        presentation.PlayAsync(default);
        yield return null;

        Assert.That(presentation.IsPlaying, Is.True);
        Assert.That(blackout.gameObject.activeSelf, Is.True);
        Assert.That(blackout.alpha, Is.GreaterThanOrEqualTo(alphaBeforeSecondCall));

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Capture presentation did not complete."
        );
    }

    private StagePhotoCapturePresentation CreatePresentation(Vector3 previewScale)
    {
        var root = new GameObject("StagePhotoCapturePresentation");
        createdObjects.Add(root);

        var blackoutObject = new GameObject("ShutterBlackout", typeof(RectTransform));
        blackoutObject.transform.SetParent(root.transform);
        var blackout = blackoutObject.AddComponent<CanvasGroup>();

        var previewObject = new GameObject("CapturedPhotoPreview", typeof(RectTransform));
        previewObject.transform.SetParent(root.transform);
        var preview = previewObject.AddComponent<RawImage>();
        preview.rectTransform.localScale = previewScale;

        var presentation = root.AddComponent<StagePhotoCapturePresentation>();
        SetPrivateField(presentation, "shutterBlackout", blackout);
        SetPrivateField(presentation, "capturedPhotoPreview", preview);
        SetPrivateField(presentation, "capturedPhotoPreviewTransform", preview.rectTransform);
        presentation.ResetPresentation();

        return presentation;
    }

    private static IEnumerator WaitUntilOrTimeout(Func<bool> predicate, string message)
    {
        const float timeoutSeconds = 1f;
        var startedAt = Time.realtimeSinceStartup;

        while (!predicate())
        {
            if (Time.realtimeSinceStartup - startedAt > timeoutSeconds)
            {
                Assert.Fail(message);
            }

            yield return null;
        }
    }

    private static void SetDurations(
        StagePhotoCapturePresentation presentation,
        float fadeIn,
        float hold,
        float fadeOut,
        float previewScale
    )
    {
        SetPrivateField(presentation, "blackoutFadeInDuration", fadeIn);
        SetPrivateField(presentation, "blackoutHoldDuration", hold);
        SetPrivateField(presentation, "blackoutFadeOutDuration", fadeOut);
        SetPrivateField(presentation, "capturedPhotoScaleDuration", previewScale);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }
}

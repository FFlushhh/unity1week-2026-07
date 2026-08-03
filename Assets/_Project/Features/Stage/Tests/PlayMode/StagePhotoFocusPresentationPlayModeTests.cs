using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StagePhotoFocusPresentationPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<GameObject> createdObjects = new();
    private readonly List<Material> createdMaterials = new();
    private readonly List<RenderTexture> createdRenderTextures = new();

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

        foreach (var createdMaterial in createdMaterials)
        {
            if (createdMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(createdMaterial);
            }
        }
        createdMaterials.Clear();

        foreach (var createdRenderTexture in createdRenderTextures)
        {
            if (createdRenderTexture != null)
            {
                createdRenderTexture.Release();
                UnityEngine.Object.DestroyImmediate(createdRenderTexture);
            }
        }
        createdRenderTextures.Clear();

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayAsyncUsesAMaterialInstanceAndLeavesTheSharedAssetUntouched()
    {
        var (presentation, preview, blurMaterialSource) = CreatePresentation(
            new Vector2Int(1440, 1080)
        );
        SetDurations(presentation, 0.02f, 0.02f);

        presentation.PlayAsync(default);
        yield return null;

        Assert.That(preview.material, Is.Not.SameAs(blurMaterialSource));
        Assert.That(preview.material.shader.name, Is.EqualTo("Stage/PhotoPreviewBlur"));
        Assert.That(blurMaterialSource.GetFloat("_BlurStrength"), Is.EqualTo(0f));

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete."
        );

        Assert.That(blurMaterialSource.GetFloat("_BlurStrength"), Is.EqualTo(0f));
    }

    [UnityTest]
    public IEnumerator BlurStrengthDecreasesMonotonicallyAndReachesZero()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0.1f, 0f);

        presentation.PlayAsync(default);
        yield return null;

        var previousStrength = float.PositiveInfinity;
        while (presentation.IsPlaying)
        {
            var material = preview.material;
            if (material != null)
            {
                var currentStrength = material.GetFloat("_BlurStrength");
                Assert.That(currentStrength, Is.LessThanOrEqualTo(previousStrength));
                previousStrength = currentStrength;
            }
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator CompletedPresentationRestoresTheDefaultUiMaterial()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0.02f, 0.02f);

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete."
        );

        Assert.That(preview.material, Is.SameAs(preview.defaultMaterial));
    }

    [UnityTest]
    public IEnumerator PlayAsyncWaitsThePostBlurDurationAfterTheBlurIsCleared()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0.02f, 0.3f);

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => preview.material != null && preview.material.GetFloat("_BlurStrength") <= 0f,
            "Blur did not clear."
        );

        Assert.That(presentation.IsPlaying, Is.True);

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete after the post-blur wait."
        );
    }

    [UnityTest]
    public IEnumerator ZeroDurationsCompleteImmediatelyWithoutLeavingBlur()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0f, 0f);

        presentation.PlayAsync(default);
        yield return null;

        Assert.That(presentation.IsPlaying, Is.False);
        Assert.That(preview.material, Is.SameAs(preview.defaultMaterial));
    }

    [UnityTest]
    public IEnumerator CancellationRemovesTheBlurSoThePreviewIsNotLeftBlurred()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 1f, 0f);
        using var cancellation = new System.Threading.CancellationTokenSource();

        presentation.PlayAsync(cancellation.Token);
        yield return WaitUntilOrTimeout(
            () => preview.material != null && preview.material != preview.defaultMaterial,
            "Blur did not start."
        );

        cancellation.Cancel();
        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not finish after cancellation."
        );

        Assert.That(preview.material, Is.SameAs(preview.defaultMaterial));
    }

    [UnityTest]
    public IEnumerator ResetPresentationRemovesTheBlurImmediately()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 1f, 0f);

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => preview.material != null && preview.material != preview.defaultMaterial,
            "Blur did not start."
        );

        presentation.ResetPresentation();
        yield return null;

        Assert.That(presentation.IsPlaying, Is.False);
        Assert.That(preview.material, Is.SameAs(preview.defaultMaterial));
    }

    [UnityTest]
    public IEnumerator PlayAsyncUsesUnscaledTimeWhenTimeScaleIsZero()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0.02f, 0.02f);
        Time.timeScale = 0f;

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete while timeScale was zero."
        );

        Assert.That(preview.material, Is.SameAs(preview.defaultMaterial));
    }

    [UnityTest]
    public IEnumerator RepeatedPlayAsyncDoesNotRestartThePresentation()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0.2f, 0.02f);

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => preview.material != null && preview.material != preview.defaultMaterial,
            "Blur did not start."
        );

        var strengthBeforeSecondCall = preview.material.GetFloat("_BlurStrength");
        presentation.PlayAsync(default);
        yield return null;

        Assert.That(presentation.IsPlaying, Is.True);
        Assert.That(
            preview.material.GetFloat("_BlurStrength"),
            Is.LessThanOrEqualTo(strengthBeforeSecondCall)
        );

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete."
        );
    }

    [UnityTest]
    public IEnumerator MissingReferencesStillHonorTheTimingWithoutBlockingProgress()
    {
        var (presentation, _, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetPrivateField(presentation, "photoPreview", null);
        SetDurations(presentation, 0.02f, 0.02f);

        LogAssert.Expect(
            LogType.Error,
            "[StagePhotoFocusPresentation] Photo preview or blur material is not assigned."
        );
        presentation.PlayAsync(default);
        yield return null;

        Assert.That(presentation.IsPlaying, Is.True);

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete without blur references."
        );
    }

    [UnityTest]
    public IEnumerator SourceTexelSizeMatchesTheAssignedRenderTexture()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 0.02f, 0f);

        presentation.PlayAsync(default);
        yield return null;

        var texelSize = preview.material.GetVector("_SourceTexelSize");
        Assert.That(texelSize.x, Is.EqualTo(1f / 1440f).Within(0.0000001f));
        Assert.That(texelSize.y, Is.EqualTo(1f / 1080f).Within(0.0000001f));
        Assert.That(texelSize.z, Is.EqualTo(1440f));
        Assert.That(texelSize.w, Is.EqualTo(1080f));

        yield return WaitUntilOrTimeout(
            () => !presentation.IsPlaying,
            "Focus presentation did not complete."
        );
    }

    [UnityTest]
    public IEnumerator OnDestroyDestroysTheRuntimeMaterialInstance()
    {
        var (presentation, preview, _) = CreatePresentation(new Vector2Int(1440, 1080));
        SetDurations(presentation, 1f, 0f);

        presentation.PlayAsync(default);
        yield return WaitUntilOrTimeout(
            () => preview.material != null && preview.material != preview.defaultMaterial,
            "Blur did not start."
        );

        var runtimeMaterial = preview.material;
        UnityEngine.Object.DestroyImmediate(presentation.gameObject);
        yield return null;

        Assert.That(runtimeMaterial == null, Is.True);
    }

    private (
        StagePhotoFocusPresentation Presentation,
        RawImage Preview,
        Material BlurMaterialSource
    ) CreatePresentation(Vector2Int renderTextureSize)
    {
        var root = new GameObject("StagePhotoFocusPresentation");
        createdObjects.Add(root);

        var previewObject = new GameObject("PhotoPreview", typeof(RectTransform));
        previewObject.transform.SetParent(root.transform);
        var preview = previewObject.AddComponent<RawImage>();
        var renderTexture = new RenderTexture(
            renderTextureSize.x,
            renderTextureSize.y,
            0,
            RenderTextureFormat.ARGB32
        );
        createdRenderTextures.Add(renderTexture);
        preview.texture = renderTexture;

        var blurMaterialSource = new Material(Shader.Find("Stage/PhotoPreviewBlur"));
        createdMaterials.Add(blurMaterialSource);

        var presentation = root.AddComponent<StagePhotoFocusPresentation>();
        SetPrivateField(presentation, "photoPreview", preview);
        SetPrivateField(presentation, "blurMaterialSource", blurMaterialSource);

        return (presentation, preview, blurMaterialSource);
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
        StagePhotoFocusPresentation presentation,
        float blurClearDuration,
        float postBlurWaitDuration
    )
    {
        SetPrivateField(presentation, "blurClearDuration", blurClearDuration);
        SetPrivateField(presentation, "postBlurWaitDuration", postBlurWaitDuration);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }
}

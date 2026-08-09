using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 撮影中に発生するピンボケの予定と、指定時刻における状態を評価します。
/// </summary>
internal sealed class RandomDefocusTimeline
{
    internal const float EventDurationSeconds = 3f;
    internal const float PeakTimeSeconds = 1.5f;
    internal const float ScorePenaltyStartSeconds = 1f;
    internal const float ScorePenaltyEndSeconds = 2f;

    private readonly List<float> eventStartTimes;

    internal RandomDefocusTimeline(IEnumerable<float> eventStartTimes)
    {
        this.eventStartTimes =
            eventStartTimes == null ? new List<float>() : new List<float>(eventStartTimes);
        this.eventStartTimes.Sort();
    }

    internal int EventCount => eventStartTimes.Count;

    internal static RandomDefocusTimeline Create(
        float playingDurationSeconds,
        float drawIntervalSeconds,
        float gameOccurrenceProbability,
        System.Random random
    )
    {
        if (playingDurationSeconds <= 0f || drawIntervalSeconds <= 0f)
        {
            return new RandomDefocusTimeline(null);
        }

        if (random == null)
        {
            throw new ArgumentNullException(nameof(random));
        }

        var drawCount = Mathf.CeilToInt(playingDurationSeconds / drawIntervalSeconds);
        var probabilityPerDraw = CalculateProbabilityPerDraw(gameOccurrenceProbability, drawCount);
        var eventStartTimes = new List<float>();

        for (var drawIndex = 0; drawIndex < drawCount; drawIndex++)
        {
            if (random.NextDouble() >= probabilityPerDraw)
            {
                continue;
            }

            var intervalStart = drawIndex * drawIntervalSeconds;
            var intervalEnd = Mathf.Min(
                intervalStart + drawIntervalSeconds,
                playingDurationSeconds
            );
            var intervalDuration = Mathf.Max(0f, intervalEnd - intervalStart);
            var offset = (float)random.NextDouble() * intervalDuration;
            eventStartTimes.Add(intervalStart + offset);
        }

        return new RandomDefocusTimeline(eventStartTimes);
    }

    internal static float CalculateProbabilityPerDraw(
        float gameOccurrenceProbability,
        int drawCount
    )
    {
        if (drawCount <= 0)
        {
            return 0f;
        }

        var clampedProbability = Mathf.Clamp01(gameOccurrenceProbability);
        return 1f - Mathf.Pow(1f - clampedProbability, 1f / drawCount);
    }

    internal RandomDefocusState Evaluate(float elapsedSeconds)
    {
        var maximumBlurStrength = 0f;
        var isScoreForcedToZero = false;

        foreach (var eventStartTime in eventStartTimes)
        {
            var eventElapsedSeconds = elapsedSeconds - eventStartTime;
            maximumBlurStrength = Mathf.Max(
                maximumBlurStrength,
                CalculateBlurStrength(eventElapsedSeconds)
            );
            isScoreForcedToZero |= IsScorePenaltyActive(eventElapsedSeconds);
        }

        return new RandomDefocusState(maximumBlurStrength, isScoreForcedToZero);
    }

    internal static float CalculateBlurStrength(float eventElapsedSeconds)
    {
        if (eventElapsedSeconds < 0f || eventElapsedSeconds >= EventDurationSeconds)
        {
            return 0f;
        }

        if (eventElapsedSeconds <= PeakTimeSeconds)
        {
            return Mathf.SmoothStep(0f, 1f, eventElapsedSeconds / PeakTimeSeconds);
        }

        return Mathf.SmoothStep(
            1f,
            0f,
            (eventElapsedSeconds - PeakTimeSeconds) / (EventDurationSeconds - PeakTimeSeconds)
        );
    }

    internal static bool IsScorePenaltyActive(float eventElapsedSeconds)
    {
        return eventElapsedSeconds >= ScorePenaltyStartSeconds
            && eventElapsedSeconds <= ScorePenaltyEndSeconds;
    }
}

/// <summary>
/// シャッター時点のピンボケ状態です。画像のぼかし強度とスコア判定を同時に確定します。
/// </summary>
internal readonly struct RandomDefocusState
{
    internal RandomDefocusState(float blurStrength, bool isScoreForcedToZero)
    {
        BlurStrength = Mathf.Clamp01(blurStrength);
        IsScoreForcedToZero = isScoreForcedToZero;
    }

    internal float BlurStrength { get; }

    internal bool IsScoreForcedToZero { get; }
}

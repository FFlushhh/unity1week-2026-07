using System;
using System.Collections.Generic;

/// <summary>
/// 1種類の被写体について、1プレイ中に使用する出現時刻と経路を生成します。
/// </summary>
internal static class SubjectSpawnScheduleBuilder
{
    public static bool TryBuild(
        SubjectSpawnScheduleRequest request,
        Random random,
        out List<SubjectSpawnScheduleEntry> schedule
    )
    {
        schedule = new List<SubjectSpawnScheduleEntry>();

        if (random == null || !IsValid(request))
        {
            return false;
        }

        if (random.NextDouble() >= request.AppearanceProbability)
        {
            return true;
        }

        var count = random.Next(request.MinimumCount, request.MaximumCount + 1);
        var flexibleRangeSeconds =
            request.LatestSpawnTimeSeconds
            - request.EarliestSpawnTimeSeconds
            - (count - 1) * request.MinimumIntervalSeconds;
        var normalizedTimeOffsets = new List<float>(count);

        for (var index = 0; index < count; index++)
        {
            normalizedTimeOffsets.Add((float)random.NextDouble());
        }

        normalizedTimeOffsets.Sort();

        for (var index = 0; index < count; index++)
        {
            var spawnTimeSeconds =
                request.EarliestSpawnTimeSeconds
                + index * request.MinimumIntervalSeconds
                + normalizedTimeOffsets[index] * flexibleRangeSeconds;
            schedule.Add(
                new SubjectSpawnScheduleEntry(
                    spawnTimeSeconds,
                    SelectRouteIndex(request.RouteWeights, random)
                )
            );
        }

        return true;
    }

    private static bool IsValid(SubjectSpawnScheduleRequest request)
    {
        if (
            !IsFinite(request.AppearanceProbability)
            || request.AppearanceProbability < 0f
            || request.AppearanceProbability > 1f
            || request.MinimumCount < 1
            || request.MaximumCount < request.MinimumCount
            || !IsFinite(request.EarliestSpawnTimeSeconds)
            || !IsFinite(request.LatestSpawnTimeSeconds)
            || request.EarliestSpawnTimeSeconds < 0f
            || request.LatestSpawnTimeSeconds < request.EarliestSpawnTimeSeconds
            || !IsFinite(request.MinimumIntervalSeconds)
            || request.MinimumIntervalSeconds < 0f
            || request.RouteWeights == null
            || request.RouteWeights.Length == 0
        )
        {
            return false;
        }

        var requiredSpanSeconds = (request.MaximumCount - 1) * request.MinimumIntervalSeconds;
        if (requiredSpanSeconds > request.LatestSpawnTimeSeconds - request.EarliestSpawnTimeSeconds)
        {
            return false;
        }

        var totalRouteWeight = 0f;
        foreach (var routeWeight in request.RouteWeights)
        {
            if (!IsFinite(routeWeight) || routeWeight < 0f)
            {
                return false;
            }

            totalRouteWeight += routeWeight;
        }

        return totalRouteWeight > 0f && IsFinite(totalRouteWeight);
    }

    private static int SelectRouteIndex(float[] routeWeights, Random random)
    {
        var totalRouteWeight = 0f;
        foreach (var routeWeight in routeWeights)
        {
            totalRouteWeight += routeWeight;
        }

        var selectedWeight = (float)(random.NextDouble() * totalRouteWeight);
        var accumulatedWeight = 0f;
        var lastSelectableRouteIndex = 0;

        for (var index = 0; index < routeWeights.Length; index++)
        {
            if (routeWeights[index] <= 0f)
            {
                continue;
            }

            lastSelectableRouteIndex = index;
            accumulatedWeight += routeWeights[index];
            if (selectedWeight < accumulatedWeight)
            {
                return index;
            }
        }

        return lastSelectableRouteIndex;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// 被写体1種類のランダム出現条件です。最小個数は、出現抽選に当たった場合の個数です。
/// </summary>
internal readonly struct SubjectSpawnScheduleRequest
{
    public SubjectSpawnScheduleRequest(
        float appearanceProbability,
        int minimumCount,
        int maximumCount,
        float earliestSpawnTimeSeconds,
        float latestSpawnTimeSeconds,
        float minimumIntervalSeconds,
        float[] routeWeights
    )
    {
        AppearanceProbability = appearanceProbability;
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
        EarliestSpawnTimeSeconds = earliestSpawnTimeSeconds;
        LatestSpawnTimeSeconds = latestSpawnTimeSeconds;
        MinimumIntervalSeconds = minimumIntervalSeconds;
        RouteWeights = routeWeights;
    }

    public float AppearanceProbability { get; }

    public int MinimumCount { get; }

    public int MaximumCount { get; }

    public float EarliestSpawnTimeSeconds { get; }

    public float LatestSpawnTimeSeconds { get; }

    public float MinimumIntervalSeconds { get; }

    public float[] RouteWeights { get; }
}

/// <summary>
/// 生成済みの出現時刻と、Inspectorで設定する経路の添字を表します。
/// </summary>
internal readonly struct SubjectSpawnScheduleEntry
{
    public SubjectSpawnScheduleEntry(float spawnTimeSeconds, int routeIndex)
    {
        SpawnTimeSeconds = spawnTimeSeconds;
        RouteIndex = routeIndex;
    }

    public float SpawnTimeSeconds { get; }

    public int RouteIndex { get; }
}

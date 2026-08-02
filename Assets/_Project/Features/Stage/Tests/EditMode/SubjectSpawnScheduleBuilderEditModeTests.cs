using System;
using NUnit.Framework;

public sealed class SubjectSpawnScheduleBuilderEditModeTests
{
    [Test]
    public void TryBuild_WhenAppearanceRollMisses_ReturnsEmptySchedule()
    {
        var result = SubjectSpawnScheduleBuilder.TryBuild(
            CreateRequest(appearanceProbability: 0f),
            new Random(0),
            out var schedule
        );

        Assert.That(result, Is.True);
        Assert.That(schedule, Is.Empty);
    }

    [Test]
    public void TryBuild_WhenAppearanceIsGuaranteed_UsesConfiguredCountAndTimeRange()
    {
        var request = CreateRequest(
            appearanceProbability: 1f,
            minimumCount: 3,
            maximumCount: 3,
            earliestSpawnTimeSeconds: 1f,
            latestSpawnTimeSeconds: 5f,
            minimumIntervalSeconds: 0.5f
        );

        var result = SubjectSpawnScheduleBuilder.TryBuild(request, new Random(1), out var schedule);

        Assert.That(result, Is.True);
        Assert.That(schedule, Has.Count.EqualTo(3));
        Assert.That(schedule[0].SpawnTimeSeconds, Is.GreaterThanOrEqualTo(1f));
        Assert.That(schedule[2].SpawnTimeSeconds, Is.LessThanOrEqualTo(5f));
        Assert.That(
            schedule[1].SpawnTimeSeconds - schedule[0].SpawnTimeSeconds,
            Is.GreaterThanOrEqualTo(0.5f)
        );
        Assert.That(
            schedule[2].SpawnTimeSeconds - schedule[1].SpawnTimeSeconds,
            Is.GreaterThanOrEqualTo(0.5f)
        );
    }

    [Test]
    public void TryBuild_WithSameSeed_ProducesSameSchedule()
    {
        var request = CreateRequest(
            appearanceProbability: 1f,
            minimumCount: 1,
            maximumCount: 3,
            routeWeights: new[] { 1f, 1f }
        );

        SubjectSpawnScheduleBuilder.TryBuild(request, new Random(42), out var firstSchedule);
        SubjectSpawnScheduleBuilder.TryBuild(request, new Random(42), out var secondSchedule);

        Assert.That(secondSchedule, Has.Count.EqualTo(firstSchedule.Count));
        for (var index = 0; index < firstSchedule.Count; index++)
        {
            Assert.That(
                secondSchedule[index].SpawnTimeSeconds,
                Is.EqualTo(firstSchedule[index].SpawnTimeSeconds)
            );
            Assert.That(
                secondSchedule[index].RouteIndex,
                Is.EqualTo(firstSchedule[index].RouteIndex)
            );
        }
    }

    [Test]
    public void TryBuild_WhenOnlyOneRouteHasWeight_UsesThatRouteForEveryEntry()
    {
        var request = CreateRequest(
            appearanceProbability: 1f,
            minimumCount: 3,
            maximumCount: 3,
            routeWeights: new[] { 0f, 1f }
        );

        var result = SubjectSpawnScheduleBuilder.TryBuild(request, new Random(3), out var schedule);

        Assert.That(result, Is.True);
        Assert.That(
            schedule,
            Has.All.Matches<SubjectSpawnScheduleEntry>(entry => entry.RouteIndex == 1)
        );
    }

    [TestCase(-0.01f)]
    [TestCase(1.01f)]
    public void TryBuild_WhenAppearanceProbabilityIsOutsideRange_ReturnsFalse(
        float appearanceProbability
    )
    {
        var result = SubjectSpawnScheduleBuilder.TryBuild(
            CreateRequest(appearanceProbability: appearanceProbability),
            new Random(4),
            out var schedule
        );

        Assert.That(result, Is.False);
        Assert.That(schedule, Is.Empty);
    }

    [Test]
    public void TryBuild_WhenMaximumCountCannotFitTimeRange_ReturnsFalse()
    {
        var result = SubjectSpawnScheduleBuilder.TryBuild(
            CreateRequest(
                appearanceProbability: 1f,
                minimumCount: 1,
                maximumCount: 3,
                earliestSpawnTimeSeconds: 1f,
                latestSpawnTimeSeconds: 1.5f,
                minimumIntervalSeconds: 0.5f
            ),
            new Random(5),
            out var schedule
        );

        Assert.That(result, Is.False);
        Assert.That(schedule, Is.Empty);
    }

    [Test]
    public void TryBuild_WhenRouteWeightsCannotSelectARoute_ReturnsFalse()
    {
        var result = SubjectSpawnScheduleBuilder.TryBuild(
            CreateRequest(routeWeights: Array.Empty<float>()),
            new Random(6),
            out var schedule
        );

        Assert.That(result, Is.False);
        Assert.That(schedule, Is.Empty);
    }

    private static SubjectSpawnScheduleRequest CreateRequest(
        float appearanceProbability = 1f,
        int minimumCount = 1,
        int maximumCount = 1,
        float earliestSpawnTimeSeconds = 1f,
        float latestSpawnTimeSeconds = 5f,
        float minimumIntervalSeconds = 0f,
        float[] routeWeights = null
    )
    {
        return new SubjectSpawnScheduleRequest(
            appearanceProbability,
            minimumCount,
            maximumCount,
            earliestSpawnTimeSeconds,
            latestSpawnTimeSeconds,
            minimumIntervalSeconds,
            routeWeights ?? new[] { 1f }
        );
    }
}

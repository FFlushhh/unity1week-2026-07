using System.Collections.Generic;
using NUnit.Framework;

public sealed class RandomDefocusTimelineEditModeTests
{
    [Test]
    public void CalculateProbabilityPerDraw_MatchesTwentyPercentAcrossTwentyDraws()
    {
        var probability = RandomDefocusTimeline.CalculateProbabilityPerDraw(0.2f, 20);

        Assert.That(probability, Is.EqualTo(0.011095167f).Within(0.0000001f));
        Assert.That(1f - UnityEngine.Mathf.Pow(1f - probability, 20), Is.EqualTo(0.2f));
    }

    [Test]
    public void Create_ZeroOccurrenceProbabilityCreatesNoEvents()
    {
        var timeline = RandomDefocusTimeline.Create(10f, 0.5f, 0f, new System.Random(123));

        Assert.That(timeline.EventCount, Is.Zero);
    }

    [Test]
    public void Create_CertainOccurrenceCreatesAnEventInEveryHalfSecondInterval()
    {
        var timeline = RandomDefocusTimeline.Create(10f, 0.5f, 1f, new System.Random(123));

        Assert.That(timeline.EventCount, Is.EqualTo(20));
    }

    [Test]
    public void CalculateBlurStrength_UsesTheSpecifiedThreeSecondCurve()
    {
        Assert.That(RandomDefocusTimeline.CalculateBlurStrength(0f), Is.Zero);
        Assert.That(RandomDefocusTimeline.CalculateBlurStrength(0.75f), Is.GreaterThan(0f));
        Assert.That(RandomDefocusTimeline.CalculateBlurStrength(1.5f), Is.EqualTo(1f));
        Assert.That(RandomDefocusTimeline.CalculateBlurStrength(2.25f), Is.GreaterThan(0f));
        Assert.That(RandomDefocusTimeline.CalculateBlurStrength(3f), Is.Zero);
    }

    [TestCase(0.999f, false)]
    [TestCase(1f, true)]
    [TestCase(2f, true)]
    [TestCase(2.001f, false)]
    public void IsScorePenaltyActive_UsesInclusiveBoundaries(float elapsedSeconds, bool expected)
    {
        Assert.That(
            RandomDefocusTimeline.IsScorePenaltyActive(elapsedSeconds),
            Is.EqualTo(expected)
        );
    }

    [Test]
    public void Evaluate_UsesTheMaximumBlurAndAnyPenaltyFromOverlappingEvents()
    {
        var timeline = new RandomDefocusTimeline(new List<float> { 0f, 0.8f });

        var state = timeline.Evaluate(1.5f);

        Assert.That(state.BlurStrength, Is.EqualTo(1f));
        Assert.That(state.IsScoreForcedToZero, Is.True);
    }

    [Test]
    public void Evaluate_EndsAllEffectsAfterTheLastEventDuration()
    {
        var timeline = new RandomDefocusTimeline(new[] { 9.8f });

        var state = timeline.Evaluate(12.8f);

        Assert.That(state.BlurStrength, Is.Zero);
        Assert.That(state.IsScoreForcedToZero, Is.False);
    }
}

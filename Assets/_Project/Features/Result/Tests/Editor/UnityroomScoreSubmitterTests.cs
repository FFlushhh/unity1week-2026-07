using NUnit.Framework;
using ResultScene;

namespace ResultScene.Tests
{
    public class UnityroomScoreSubmitterTests
    {
        [TestCase(0, 0f)]
        [TestCase(1, 1f)]
        [TestCase(10000, 10000f)]
        [TestCase(-1, 0f)]
        [TestCase(-10000, 0f)]
        [TestCase(int.MinValue, 0f)]
        [TestCase(int.MaxValue, (float)int.MaxValue)]
        public void ClampScoreForSubmission_ReturnsNonNegativeScore(
            int rawScore,
            float expectedScore
        )
        {
            float actualScore = UnityroomScoreSubmitter.ClampScoreForSubmission(rawScore);

            Assert.AreEqual(expectedScore, actualScore);
        }
    }
}

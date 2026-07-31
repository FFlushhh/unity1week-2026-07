using System.Collections.Generic;
using NUnit.Framework;
using ResultScene;

namespace ResultScene.Tests
{
    public class ResultScoreCalculatorTests
    {
        private List<BonusScoreMaster> _masterList;

        [SetUp]
        public void SetUp()
        {
            _masterList = new List<BonusScoreMaster>
            {
                new BonusScoreMaster { BonusName = "PositiveBonus", ScorePerItem = 500 },
                new BonusScoreMaster { BonusName = "NegativeBonus", ScorePerItem = -300 },
            };
        }

        [Test]
        public void CalculateTotalScore_NoBonuses_ReturnsBaseScore()
        {
            int baseScore = 1000;
            var bonuses = new List<BonusInputData>();

            int total = ResultScoreCalculator.CalculateTotalScore(baseScore, bonuses, _masterList);

            Assert.AreEqual(1000, total);
        }

        [Test]
        public void CalculateTotalScore_MixedBonuses_CalculatesCorrectly()
        {
            int baseScore = 1000;
            var bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "PositiveBonus", Count = 2 }, // +1000
                new BonusInputData { BonusName = "NegativeBonus", Count = 1 }, // -300
            };

            int total = ResultScoreCalculator.CalculateTotalScore(baseScore, bonuses, _masterList);

            Assert.AreEqual(1700, total);
        }

        [Test]
        public void CalculateTotalScore_ScoreBecomesZeroOrNegative_CalculatesCorrectly()
        {
            int baseScore = 500;
            var bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "NegativeBonus", Count = 3 }, // -900
            };

            int total = ResultScoreCalculator.CalculateTotalScore(baseScore, bonuses, _masterList);

            Assert.AreEqual(-400, total);
        }

        [Test]
        public void CalculateTotalScore_UnknownBonus_IgnoresBonus()
        {
            int baseScore = 1000;
            var bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "PositiveBonus", Count = 1 }, // +500
                new BonusInputData { BonusName = "UnknownBonus", Count = 5 }, // +0
            };

            int total = ResultScoreCalculator.CalculateTotalScore(baseScore, bonuses, _masterList);

            Assert.AreEqual(1500, total);
        }

        [TestCase(10000, Rank.S)]
        [TestCase(15000, Rank.S)]
        [TestCase(9999, Rank.A)]
        [TestCase(8000, Rank.A)]
        [TestCase(7999, Rank.B)]
        [TestCase(5000, Rank.B)]
        [TestCase(4999, Rank.C)]
        [TestCase(0, Rank.C)]
        [TestCase(-100, Rank.C)]
        public void DetermineRank_ReturnsCorrectRank(int score, Rank expectedRank)
        {
            Rank actualRank = ResultScoreCalculator.DetermineRank(score, 10000, 8000, 5000);
            Assert.AreEqual(expectedRank, actualRank);
        }
    }
}

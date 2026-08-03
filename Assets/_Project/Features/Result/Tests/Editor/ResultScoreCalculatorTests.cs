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

        [Test]
        public void CalculateTotalScore_SelfieGirlBonus_AddsThousandPerItem()
        {
            var stageMaster = CreateStageBonusMaster();
            var bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "自撮り", Count = 1 },
            };

            int total = ResultScoreCalculator.CalculateTotalScore(1000, bonuses, stageMaster);

            Assert.AreEqual(2000, total);
        }

        [Test]
        public void CalculateTotalScore_MultipleSelfieGirlBonuses_MultipliesByCount()
        {
            var stageMaster = CreateStageBonusMaster();
            var bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "自撮り", Count = 3 },
            };

            int total = ResultScoreCalculator.CalculateTotalScore(1000, bonuses, stageMaster);

            Assert.AreEqual(4000, total);
        }

        [Test]
        public void CalculateTotalScore_SelfieGirlWithExistingBonuses_KeepsExistingScores()
        {
            var stageMaster = CreateStageBonusMaster();
            var bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "自撮り", Count = 1 },
                new BonusInputData { BonusName = "犬", Count = 1 },
                new BonusInputData { BonusName = "狂犬", Count = 1 },
            };

            int total = ResultScoreCalculator.CalculateTotalScore(1000, bonuses, stageMaster);

            // 1000(基礎) + 1000(自撮り) + 500(犬) - 800(狂犬)
            Assert.AreEqual(1700, total);
        }

        private static List<BonusScoreMaster> CreateStageBonusMaster()
        {
            return new List<BonusScoreMaster>
            {
                new BonusScoreMaster { BonusName = "犬", ScorePerItem = 500 },
                new BonusScoreMaster { BonusName = "汚れた服の人", ScorePerItem = -600 },
                new BonusScoreMaster { BonusName = "狂犬", ScorePerItem = -800 },
                new BonusScoreMaster { BonusName = "ビニール袋", ScorePerItem = -100 },
                new BonusScoreMaster { BonusName = "鳥", ScorePerItem = 800 },
                new BonusScoreMaster { BonusName = "スズメ", ScorePerItem = 5 },
                new BonusScoreMaster { BonusName = "自撮り", ScorePerItem = 1000 },
            };
        }

        [TestCase(10000, Rank.S)]
        [TestCase(15000, Rank.S)]
        [TestCase(9999, Rank.A)]
        [TestCase(8000, Rank.A)]
        [TestCase(7999, Rank.B)]
        [TestCase(5000, Rank.B)]
        [TestCase(4999, Rank.B)]
        [TestCase(0, Rank.B)]
        [TestCase(-100, Rank.B)]
        public void DetermineRank_ReturnsCorrectRank(int score, Rank expectedRank)
        {
            Rank actualRank = ResultScoreCalculator.DetermineRank(score, 10000, 8000);
            Assert.AreEqual(expectedRank, actualRank);
        }
    }
}

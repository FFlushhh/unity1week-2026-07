using System.Collections.Generic;
using System.Linq;

namespace ResultScene
{
    public enum Rank
    {
        S,
        A,
        B,
    }

    public static class ResultScoreCalculator
    {
        public static int CalculateTotalScore(
            int baseScore,
            List<BonusInputData> bonuses,
            List<BonusScoreMaster> masterList
        )
        {
            int totalScore = baseScore;

            if (bonuses != null && masterList != null)
            {
                foreach (var bonus in bonuses)
                {
                    if (bonus.Count > 0)
                    {
                        var master = masterList.FirstOrDefault(m => m.BonusName == bonus.BonusName);
                        if (master != null)
                        {
                            totalScore += master.ScorePerItem * bonus.Count;
                        }
                    }
                }
            }

            return totalScore;
        }

        public static Rank DetermineRank(int totalScore, int sThreshold, int aThreshold)
        {
            if (totalScore >= sThreshold)
                return Rank.S;
            if (totalScore >= aThreshold)
                return Rank.A;

            return Rank.B;
        }
    }
}

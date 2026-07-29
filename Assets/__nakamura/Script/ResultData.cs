using System.Collections.Generic;
using UnityEngine;

namespace ResultScene
{
    [System.Serializable]
    public class ResultData
    {
        [Header("プレイヤー情報")]
        public string PlayerName;
        
        [Header("撮影情報")]
        public string LocationName;
        public Sprite CapturedImage;
        
        [Header("スコア情報")]
        public int BaseScore; // 基礎点
        public List<BonusInputData> Bonuses;
    }

    [System.Serializable]
    public class BonusInputData
    {
        public string BonusName; // ボーナスの名前
        public int Count;        // ボーナスの獲得数
    }
}

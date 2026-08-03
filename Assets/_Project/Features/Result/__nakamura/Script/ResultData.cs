using System.Collections.Generic;
using UnityEngine;

namespace ResultScene
{
    [System.Serializable]
    public class ResultData
    {
        [Header("プレイヤー情報")]
        [Tooltip("プレイヤーの名前")]
        public string PlayerName;

        [Header("撮影情報")]
        [Tooltip("撮影場所の名前")]
        public string LocationName;

        [Tooltip("撮影された写真（Texture2D）")]
        public Texture2D CapturedImage;

        [Header("スコア情報")]
        [Tooltip("獲得したボーナスのリスト")]
        public List<BonusInputData> Bonuses;
    }

    [System.Serializable]
    public class BonusInputData
    {
        [Tooltip("ボーナスの名前")]
        public string BonusName; // ボーナスの名前

        [Tooltip("ボーナスの獲得数")]
        public int Count; // ボーナスの獲得数
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ResultScene
{
    public class MockGameManager : MonoBehaviour
    {
        [Header("テスト用のダミーデータ")]
        [Tooltip("ここに設定したデータがResultSceneに渡さる。")]
        public ResultData MockData = new ResultData
        {
            PlayerName = "テスター",
            LocationName = "秋葉原",
            BaseScore = 1000,
            Bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "犬", Count = 1 },
                new BonusInputData { BonusName = "鳥", Count = 3 }
            }
        };

        /// <summary>
        /// ボタン等から呼び出される
        /// </summary>
        public void GoToResult()
        {
            // データをトランスポーターに預ける
            ResultDataTransporter.CurrentData = MockData;

            // ResultSceneをロード
            SceneManager.LoadScene("ResultScene");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResultScene
{
    public class MockGameManager : MonoBehaviour
    {
        [Header("テスト用のダミーデータ")]
        [Tooltip("ここに設定したデータがResultSceneに渡る。")]
        public ResultData MockData = new ResultData
        {
            PlayerName = "テスター",
            LocationName = "秋葉原",
            Bonuses = new List<BonusInputData>
            {
                new BonusInputData { BonusName = "犬", Count = 1 },
                new BonusInputData { BonusName = "ハト", Count = 3 },
            },
        };

        /// <summary>
        /// ボタン等から呼び出される
        /// </summary>
        public void GoToResult()
        {
            // データをトランスポーターに預ける前にコピーを生成
            // （ResultScene側でテクスチャを破棄する所有権ルールのため、元アセットを守る）
            ResultData passData = new ResultData
            {
                PlayerName = MockData.PlayerName,
                LocationName = MockData.LocationName,
                Bonuses =
                    MockData.Bonuses != null
                        ? new List<BonusInputData>(MockData.Bonuses)
                        : new List<BonusInputData>(),
            };

            if (MockData.CapturedImage != null)
            {
                // 非Readableなアセットテクスチャでもクローンできるように、RenderTextureを経由してコピー
                passData.CapturedImage = CreateTextureCopy(MockData.CapturedImage);
            }

            ResultDataTransporter.CurrentData = passData;

            // ResultSceneをロード
            SceneManager.LoadScene("ResultScene");
        }

        private Texture2D CreateTextureCopy(Texture2D original)
        {
            if (original == null)
                return null;

            RenderTexture tmp = RenderTexture.GetTemporary(
                original.width,
                original.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );
            Graphics.Blit(original, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D copy = new Texture2D(
                original.width,
                original.height,
                TextureFormat.RGBA32,
                false
            );
            copy.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            copy.Apply();
            copy.name = original.name;

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return copy;
        }
    }
}

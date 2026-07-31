namespace ResultScene
{
    public static class ResultDataTransporter
    {
        /// <summary>
        /// シーン間でResultDataを受け渡すための静的変数
        /// 前のシーンでここにデータを詰め込み、ResultSceneをロード
        /// </summary>
        public static ResultData CurrentData;
    }
}

# テストと動作確認

Unity Editorで **Window > General > Test Runner** を開き、EditModeまたはPlayModeを選んで対象テストを実行する。実行結果は環境・対象・未実行項目とともに記録する。

- `Assets/_Project/Features/Stage/Tests/`：被写体判定、撮影、ピンボケ、進行・遷移など
- `Assets/_Project/Features/Result/Tests/`：スコア計算、送信値、リザルト表示など。Editor／Runtimeに分かれる
- `Assets/_Project/Features/SoundSetting/Tests/`：音声設定のPlayModeテスト

実際の画面・音声・入力、シーン遷移、Missing参照は`Title`からPlayして確認する。0点・強制0点・1点以上、撮影しない場合、スキップと再プレイも確認する。WebGL日本語入力とunityroom送信は対象ビルド・環境で別途確認する。

Gitフックの`dotnet build`はUnityが生成したC#プロジェクトのビルドであり、Unity Test Runnerの実行やプレイヤービルドを代替しない。GitHub ActionsはCSharpierとSemgrepを実行し、Unity Editorは起動しない。

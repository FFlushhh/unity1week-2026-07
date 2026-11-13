# 『いいね』しろっ！

動く被写体をタイミングよく自撮り写真に収め、架空のSNSで「いいね」を集める、1人用の2D写真撮影・スコアゲームです。3人で制作したUnity 1週間ゲーム制作プロジェクトです。

## 遊び方

10秒間の撮影チャンスに、固定された写真枠の中へ被写体が入るタイミングを狙ってシャッターを切ります。写った対象に応じていいね数が増減し、撮影した写真と結果がSNS風の画面に表示されます。時間内に撮影できなかった場合はゲームオーバーです。

| 操作 | 入力 |
|---|---|
| ゲーム開始・撮影 | ボタン、Space、Enter |
| リザルトの演出スキップ | 画面クリック、Space、Enter |
| リザルトから進む | ボタン、または画面・Space・Enterの長押し |

撮影中にはランダムでピントがぼけることがあります。特にぼけが強いタイミングで撮った写真は0点になります。0いいねのリザルトでは、コメントを流しつつ専用の効果音を再生します。

## リポジトリの内容

- `Assets/_Project/Features/`：タイトル、撮影ステージ、リザルト、音声設定などの機能とテスト
- `Assets/_Project/Scenes/`：タイトル、ステージ、リザルトなどのシーン
- `Assets/Images/`・`Assets/Sounds/`：画像・音声素材
- `Packages/`・`ProjectSettings/`：Unityの依存関係とプロジェクト設定
- `docs/`：ゲーム仕様、開発手順、設計判断

## 主な技術

- Unity 6000.5.5f1、C#、Universal Render Pipeline
- Unity Input System：入力処理
- UniTask：非同期処理
- WebGLInput：WebGLでの文字入力支援
- unityroom-sdk：WebGL向けスコア送信

WebGLビルドでは、unityroomのスコアボードにスコアを送信します。

## ドキュメント

- [ゲーム概要](docs/game/overview.md)・[ゲーム仕様](docs/game/specifications.md)
- [開発環境のセットアップ](docs/development/setup.md)・[テストと動作確認](docs/development/testing.md)
- [アーキテクチャと構成](docs/architecture/overview.md)・[ディレクトリ構成](docs/architecture/directory-structure.md)
- [開発への参加方法](CONTRIBUTING.md)・[ドキュメント一覧](docs/README.md)
- [素材・ライブラリ情報](docs/credits.md)

# ADR-0013: 非同期処理にUniTaskを使用する

## 状態

採用

## 日付

2026-07-30

## 決定

ゲーム進行に関する時間待機、演出、非同期シーン読み込みには、UniTaskを使用する。

- 時間待機には`UniTask.Delay`を使用する
- `MonoBehaviour`の寿命に従う処理には`destroyCancellationToken`を渡す
- 非同期処理は`UniTask`または`UniTaskVoid`を返し、`async void`は使用しない
- 非同期処理を開始する箇所では、例外を握りつぶさないよう`.Forget()`を使用する
- 1フレーム待機やフレーム末尾待機は、用途に応じて`UniTask.Yield`、`UniTask.NextFrame`、`UniTask.WaitForEndOfFrame`を使い分ける

## 背景

本ゲームでは、開始メッセージ、10秒カウントダウン、撮影後の待機、SNS投稿演出、
ステージ遷移など、順序とキャンセルが重要な非同期処理が複数あります。
UniTaskで処理を直列に記述し、GameObject破棄後に古い処理が継続することを防ぎます。

## 影響

- `Packages/manifest.json`と`Packages/packages-lock.json`でUniTaskのバージョンを固定する
- Coroutineを既存コードから一律に置換するのではなく、新規のゲーム進行処理から適用する
- 各処理で必要なキャンセル単位を明確にする

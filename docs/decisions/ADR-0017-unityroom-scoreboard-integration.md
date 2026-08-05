# ADR-0017: unityroomスコアボードへのスコア送信にunityroom-sdkを採用

## 状況

公開版のリザルト画面で確定した合計いいね数を、unityroom（WebGLゲーム投稿サイト）の
スコアランキング機能へ送信したい。従来`docs/game/specifications.md`の「Won't」に
「オンラインランキング」を挙げていたが、ユーザーからの明示的な依頼により対応する。
対応範囲は「スコアをunityroomのスコアボードへ送信する」までとし、ゲーム内にランキング
一覧を表示するUIは作らない（unityroom側のゲームページで見る前提）。

## 選択肢

1. **公式unityroom-sdkの導入（採用）**
   - `https://github.com/unityroom/unityroom-sdk`。MITライセンス、Unity 2021.2以上対応。
   - async/await対応、UniTaskとの併用が推奨されており、本プロジェクトは既に
     `com.cysharp.unitask`を導入済みのため追加コストが小さい。
2. **旧nuskey8/unityroom-api（AnnulusGames由来）の利用**
   - アーカイブ済みで非推奨。公式がunityroom-sdkへの移行を案内している。不採用。
3. **送信機能を実装しない**
   - 要件を満たせない。不採用。

## 決定

**unityroom-sdk**を`Packages/manifest.json`に追加し、
`Assets/_Project/Features/Result/Scoreboard/UnityroomScoreSubmitter.cs`から
リザルト確定時（`ResultSceneManager.FinishSequence()`）に1回だけ送信する。
送信はfire-and-forgetとし、WebGLビルド時のみ実行する（Unity Editor実行時は
送信をスキップし、ログのみ出力する）。

HMAC認証用キーは`UnityroomScoreSubmitter`のInspectorへ直接設定し、コミット対象と
する。クライアント側キーはWebGLビルドに同梱される時点で完全な秘匿が原理上不可能
であり、unityroom公式SDKもクライアント埋め込み運用を前提にしているため。

## 影響

- `Packages/manifest.json`・`packages-lock.json`にunityroom-sdkの依存が追加される。
- `docs/game/specifications.md`の「Won't」からオンラインランキングの記述を外し、
  「Could」および「スコア」節へ送信タイミングと失敗時方針を追記した。
- unityroom管理画面でのスコアボード作成、HMAC認証用キーの発行は開発者が手動で行う
  （AIによる代行不可）。

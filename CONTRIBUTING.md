# Contributing Guide

## 目的

この文書では、本プロジェクトにおける作業手順、Git運用、Pull Request、レビューの基本ルールを定めます。

1週間の短期開発であるため、理想的な設計よりも、競合を減らし、レビュー可能な単位で完成させることを優先します。

## 作業開始前

1. 対応するIssueまたは作業内容を確認する
2. 他のメンバーと編集対象が重複していないか確認する
3. 最新の`main`を取得する
4. 作業用ブランチを作成する

```bash
git switch main
git pull
git switch -c feature/add-player-move
```

## ブランチ

### main

- リリース可能な状態を維持する
- 直接コミット・直接pushは禁止
- Pull Request経由でのみ更新する

### feature

機能追加に使用します。

```text
feature/add-player-move
feature/add-title-ui
feature/add-enemy-ai
```

### fix

不具合修正に使用します。

```text
fix/cannot-jump-player
fix/missing-scene-reference
```

### docs

ドキュメントのみを変更する場合に使用します。

```text
docs/update-setup-guide
```

## コミットメッセージ

```text
feat: プレイヤー移動を追加
fix: NullReferenceExceptionを修正
refactor: EnemyControllerを整理
docs: READMEを更新
test: Playerテストを追加
chore: Unity設定を変更
```

| type | 内容 |
|---|---|
| feat | 新機能 |
| fix | バグ修正 |
| refactor | 動作を変えない整理 |
| docs | ドキュメント |
| test | テスト |
| chore | 設定変更・その他 |

1コミット1目的を意識します。ただし、細かく分割しすぎてレビューしにくくしないでください。

## 開発フロー

1. `main`から作業ブランチを作成する
2. 実装する
3. Unity Editor上で動作確認する
4. Consoleエラーがないことを確認する
5. 自動フォーマットを適用する
6. 変更内容を確認してコミットする
7. pushする
8. Pull Requestを作成する
9. レビュー対応後にマージする

```bash
git add .
git commit
git push -u origin feature/add-player-move
```

## Unity固有の注意

- `.meta`ファイルは必ずコミットする
- ファイルの移動・名前変更は、原則としてUnity EditorのProjectウィンドウで行う
- `Library/`、`Temp/`、`Logs/`、生成された`.csproj`・`.slnx`はコミットしない
- シーン、Prefab、ProjectSettingsを編集する前に、他のメンバーと担当を確認する
- Play Mode中のInspector変更は、原則として保存されない
- Missing参照を残したままPull Requestを作成しない
- Consoleにエラーを残したままPull Requestを作成しない

詳細は[Unity開発フロー](docs/development/unity-workflow.md)を参照してください。

## コード品質チェック

コミット前とpush前にGitフックが実行されます。

フォーマッターがファイルを変更した場合は、内容を確認して再ステージしてください。

```bash
git add <変更したファイル>
git commit
```

部分ステージ済みのC#ファイルは、未ステージ変更の保護のためコミットを停止します。対象ファイルの変更をすべてステージしてから再実行してください。

## Pull Request

Pull Requestには以下を含めます。

- 変更内容
- 変更理由
- 確認方法
- UI・見た目を変更した場合のスクリーンショットまたは動画
- 関連Issue
- 影響範囲
- 更新したドキュメント

### Pull Requestの大きさ

1つのPull Requestでは、1つの目的に集中してください。

避ける例:

- プレイヤー移動追加とフォルダ全体のリネーム
- バグ修正と無関係な大規模リファクタリング
- UI追加とパッケージ更新

## レビュー

レビュアーは、以下を確認します。

- 仕様どおりに動作するか
- 他の機能を壊していないか
- Unity参照が切れていないか
- 過剰な抽象化がないか
- 初心者が理解できる実装になっているか
- 変更内容に対してPRが大きすぎないか
- 必要なドキュメントが更新されているか

詳細は[レビューガイド](docs/development/review-guide.md)を参照してください。

## ドキュメント更新

仕様、設計方針、ディレクトリルール、開発手順を変更した場合は、実装と同じPull Requestでドキュメントを更新してください。

同じ説明を複数ファイルにコピーせず、正本へリンクします。

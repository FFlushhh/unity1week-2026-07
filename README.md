# unity1week-2026-07

## 概要

このプロジェクトの概要を記載します。

-   ジャンル:
-   開発期間: 2026-07-26～2026-08-02
-   使用Unityバージョン: 6000.5.5f1
-   開発人数: 3人

------------------------------------------------------------------------

## 開発環境

| 項目 | バージョン |
|---|---|
| Unity | 6000.5.5f1 |

------------------------------------------------------------------------

## セットアップ

### 1. リポジトリをクローン

``` bash
git clone https://github.com/FFlushhh/unity1week-2026-07.git
```

### 2. コミットテンプレートを設定

``` bash
git config --local commit.template .github/commit-message-template.txt
```

### 3. Unityでプロジェクトを開く

Unity Hubからプロジェクトを開いてください。

------------------------------------------------------------------------

## ブランチ運用

### main

リリース可能な状態を維持するブランチ

直接のコミットやプッシュは禁止

### feature

機能開発用

例

``` text
feature/add-player-move
feature/add-title-ui
feature/add-enemy-ai
```

### fix

バグ修正用

``` text
fix/cannot-jump-player
```

------------------------------------------------------------------------

## 開発フロー

1. 最新のmainを取得

``` bash
git switch main
git pull
```

2. 作業ブランチを作成

``` bash
git switch -c feature/○○
```

3. 開発

4. フォーマット

5. コミット

``` bash
git add .
git commit
```

6. プッシュ

``` bash
git push -u origin feature/○○
```

7. Pull Requestを作成

8. レビュー後にmainへマージ

------------------------------------------------------------------------

## コミットメッセージ

例:
``` text
feat: プレイヤー移動を追加
fix: NullReferenceExceptionを修正
refactor: EnemyManagerを整理
docs: README更新
test: Playerテスト追加
chore: Unity設定変更
```

| type | 内容 |
|---|---|
| feat | 新機能 |
| fix | バグ修正 |
| refactor | リファクタリング |
| docs | ドキュメント |
| test | テスト |
| chore | その他 |

------------------------------------------------------------------------

## Pull Requestに必要な情報

-   変更内容
-   確認方法
-   スクリーンショット（UI変更時）
-   関連Issue

------------------------------------------------------------------------

## コーディングルール

-   `SerializeField` を使用する
-   `public` メソッドは必要最低限とし、可能な限り `private` メソッドを優先
-   Inspectorで設定できるものは `SerializeField` を優先
-   命名規則を統一する
-   1コミット1目的を意識する
-   自動フォーマットを必ず適用する

------------------------------------------------------------------------

## ディレクトリ構成

``` text
Assets/
    Scripts/
    Scenes/
    Prefabs/
    Materials/
    Animations/
    Audio/
    UI/

.github/
    workflows/
    pull_request_template.md
    commit-message-template.txt

README.md
```

------------------------------------------------------------------------

## 使用技術

-   Unity
-   C#
-   Git
-   GitHub

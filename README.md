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
| .NET SDK | 10.0.302 |

------------------------------------------------------------------------

## セットアップ

### 1. リポジトリをクローン

``` bash
git clone https://github.com/FFlushhh/unity1week-2026-07.git
```

### 2. 必要な開発ツールを導入

- Unity HubからUnity `6000.5.5f1`を導入してください。
- .NET SDK `10.0.302`を導入してください。プロジェクトの品質チェックに使用します。
- VS Codeを使う場合は、推奨拡張機能のCSharpierを導入してください。C#保存時に自動整形されます。

### 3. Unityでプロジェクトを一度開く

Unity HubからプロジェクトをUnity `6000.5.5f1`で一度開いてください。UnityがC#プロジェクトファイル（`.slnx`・`.csproj`）を生成します。

### 4. 開発環境を設定

macOS・Linux・Git Bashでは、次を実行します。

```bash
./scripts/setup.sh
```

Windows PowerShellでは、次を実行します。

```powershell
.\scripts\setup.ps1
```

実行ポリシーによりスクリプトを実行できない場合は、現在のプロセスに限って許可して実行してください。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
```

セットアップスクリプトは、次の処理を行います。

- リポジトリローカルのコミットテンプレートと`.githooks`を設定
- CSharpierなどのリポジトリローカル.NETツールを復元
- `ProjectSettings/ProjectVersion.txt`からUnityバージョンを取得
- UnityYAMLMergeを検出し、Unity YAMLファイル用のGitマージドライバーとして登録

UnityYAMLMergeが自動検出されない場合は、環境変数`UNITY_YAML_MERGE_PATH`で実行ファイルを指定できます。

```bash
UNITY_YAML_MERGE_PATH="/path/to/UnityYAMLMerge" ./scripts/setup.sh
```

```powershell
$env:UNITY_YAML_MERGE_PATH = "C:\path\to\UnityYAMLMerge.exe"
.\scripts\setup.ps1
```

初回セットアップ時と、Gitフックやマージドライバーが動かなくなった場合に再実行してください。設定はこのリポジトリのみに適用されます。

## コード品質チェック

ローカルとGitHub Actionsで、次のチェックを実施します。

| 実行タイミング | 内容 | 失敗時の扱い |
|---|---|---|
| `git commit`前 | CSharpierによるC#整形、Unity Analyzerの重大エラー検出 | 整形された場合やエラーがある場合はコミットを停止 |
| `git push`前 | Unityが生成したC#プロジェクトのコンパイル確認 | コンパイルまたはAnalyzerエラーでpushを停止 |
| Pull Request・mainへのpush | CSharpierの整形確認、Semgrepによる静的解析 | 違反・検出時はCIを失敗 |

フォーマッターがファイルを変更した場合は、内容を確認してから再ステージし、もう一度コミットしてください。

```bash
git add <変更したファイル>
git commit
```

部分ステージ済みのC#ファイルは、未ステージの変更を保護するためコミットを停止します。対象ファイルの変更をすべてステージしてから再試行してください。

### CIの制約

GitHub ActionsではUnity Editorを起動しません。そのため、Unity固有のコンパイル・テスト・プレイヤービルドはCIでは実行せず、ローカルのpre-pushフックとUnity Editorで確認します。CIはフォーマットとUnity非依存の静的解析を最終チェックとして担当します。

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

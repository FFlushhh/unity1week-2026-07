# unity1week-2026-07

## 概要

3人で開発するUnity 1週間ゲーム制作プロジェクトです。

- ジャンル: 2D写真撮影・スコアゲーム（草案）
- 開発期間: 2026-07-26 ～ 2026-08-02
- 使用Unityバージョン: 6000.5.5f1
- 開発人数: 3人
- 開発方針: 1週間で完成させることを最優先とする

## 開発環境

| 項目 | バージョン |
|---|---|
| Unity | 6000.5.5f1 |
| .NET SDK | 10.0.302 |

## セットアップ

### 1. リポジトリをクローン

```bash
git clone https://github.com/FFlushhh/unity1week-2026-07.git
cd unity1week-2026-07
```

### 2. 必要な開発ツールを導入

- Unity HubからUnity `6000.5.5f1`を導入してください。
- .NET SDK `10.0.302`を導入してください。
- VS Codeを使う場合は、推奨拡張機能のCSharpierを導入してください。

### 3. Unityでプロジェクトを一度開く

Unity HubからプロジェクトをUnity `6000.5.5f1`で一度開いてください。
UnityがC#プロジェクトファイル（`.slnx`・`.csproj`）を生成します。

### 4. 開発環境を設定

macOS・Linux・Git Bash:

```bash
./scripts/setup.sh
```

Windows PowerShell:

```powershell
.\scripts\setup.ps1
```

実行ポリシーによりスクリプトを実行できない場合:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
```

UnityYAMLMergeが自動検出されない場合は、環境変数`UNITY_YAML_MERGE_PATH`で実行ファイルを指定してください。

```bash
UNITY_YAML_MERGE_PATH="/path/to/UnityYAMLMerge" ./scripts/setup.sh
```

```powershell
$env:UNITY_YAML_MERGE_PATH = "C:\path\to\UnityYAMLMerge.exe"
.\scripts\setup.ps1
```

## ドキュメント

最初に以下を確認してください。

1. [開発への参加方法](CONTRIBUTING.md)
2. [ドキュメント一覧](docs/README.md)
3. [アーキテクチャ概要](docs/architecture/overview.md)
4. [ディレクトリ構成](docs/architecture/directory-structure.md)
5. [Unity開発フロー](docs/development/unity-workflow.md)
6. [ゲーム仕様](docs/game/specifications.md)

AIエージェントは、実装前に[AGENTS.md](AGENTS.md)を確認してください。

## ディレクトリ構成

本プロジェクトでは、機能単位でファイルをまとめるVertical Slice構成を採用します。

```text
Assets/
├── _Project/
│   ├── Features/
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── Stage/
│   │   └── UI/
│   ├── Shared/
│   ├── Scenes/
│   └── Settings/
├── Plugins/
└── ThirdParty/

docs/
.github/
scripts/
```

詳細は[ディレクトリ構成](docs/architecture/directory-structure.md)を参照してください。

## コード品質チェック

| 実行タイミング | 内容 | 失敗時の扱い |
|---|---|---|
| `git commit`前 | CSharpierによるC#整形、Unity Analyzerの重大エラー検出 | 整形された場合やエラーがある場合はコミットを停止 |
| `git push`前 | Unityが生成したC#プロジェクトのコンパイル確認 | コンパイルまたはAnalyzerエラーでpushを停止 |
| Pull Request・mainへのpush | CSharpierの整形確認、Semgrepによる静的解析 | 違反・検出時はCIを失敗 |

GitHub ActionsではUnity Editorを起動しません。
Unity固有のコンパイル、テスト、プレイヤービルドは、ローカルのpre-pushフックとUnity Editorで確認します。

## 使用技術

- Unity
- C#
- Git
- GitHub

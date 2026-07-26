# Troubleshooting

## Unityでプロジェクトが開けない

確認:

1. Unity Hubで`6000.5.5f1`を使用しているか
2. リポジトリのルートを選択しているか
3. `ProjectSettings/ProjectVersion.txt`が存在するか
4. パッケージの解決が完了しているか
5. ConsoleまたはEditor.logにエラーがないか

`Library/`は再生成可能ですが、削除すると再インポートに時間がかかります。最初の対応として機械的に削除しないでください。

## C#プロジェクトファイルがない

Unityでプロジェクトを一度開いてください。

`.csproj`や`.slnx`はUnityが生成するため、Gitから取得するものではありません。

IDE連携に問題がある場合は、UnityのExternal Tools設定からプロジェクトファイルを再生成します。

## setupスクリプトが実行できない

### PowerShellの実行ポリシー

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
```

### 実行権限がない

macOS・Linux:

```bash
chmod +x ./scripts/setup.sh
./scripts/setup.sh
```

## UnityYAMLMergeが見つからない

環境変数`UNITY_YAML_MERGE_PATH`へ実行ファイルを指定します。

```bash
UNITY_YAML_MERGE_PATH="/path/to/UnityYAMLMerge" ./scripts/setup.sh
```

```powershell
$env:UNITY_YAML_MERGE_PATH = "C:\path\to\UnityYAMLMerge.exe"
.\scripts\setup.ps1
```

## コミットが止まる

### CSharpierがファイルを変更した

変更内容を確認して再ステージします。

```bash
git add <変更したファイル>
git commit
```

### 部分ステージされたC#ファイルがある

未ステージ変更を保護するため停止しています。

対象ファイルの変更をすべてステージするか、コミット対象を分けてください。

## pushが止まる

pre-pushでC#コンパイルまたはAnalyzerエラーが発生している可能性があります。

確認:

- Unityで一度プロジェクトを開いたか
- `.csproj`が生成されているか
- Unity Consoleにコンパイルエラーがないか
- Analyzerのエラー内容
- 使用中の.NET SDKが`10.0.302`か

## SceneまたはPrefabが競合した

1. 作業をバックアップする
2. UnityYAMLMergeを使用する
3. Unity Editorで対象ファイルを開く
4. Missing参照を確認する
5. 両方の変更が残っているか確認する
6. Play Modeで動作確認する

競合解消が難しい場合は、担当者の変更を片方ずつ再適用する方が安全です。

## Missing Scriptが表示される

考えられる原因:

- スクリプトを削除した
- クラス名とファイル名が一致していない
- コンパイルエラーで型を読み込めない
- `.meta`またはGUID参照が壊れた
- asmdefの参照が不足している

まずConsoleのコンパイルエラーを解消してください。

## Inspector参照がNoneになった

考えられる原因:

- アセットをOS上で移動した
- `.meta`が失われた
- PrefabへApplyしていない
- Scene変更が競合で消えた
- 対象Componentを削除した

参照を付け直す前に、Git差分と`.meta`の状態を確認してください。

## 大量の意図しない差分が出た

確認:

- 異なるUnityバージョンで開いていないか
- ProjectSettingsが自動更新されていないか
- Package Managerがlockファイルを更新していないか
- Sceneを開いただけで再保存していないか
- 改行コードが一括変更されていないか

意図を説明できない差分はコミットしないでください。

## 解決できない場合

次を共有してください。

- 発生した操作
- エラーメッセージ全文
- Unityバージョン
- 使用OS
- 対象ブランチ
- `git status`
- 問題発生前後の変更
- ConsoleまたはEditor.logの該当箇所

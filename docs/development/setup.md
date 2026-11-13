# 開発環境のセットアップ

1. Git、Unity Hub、Unity `6000.5.5f1`、.NET SDK `10.0.302`を用意する。バージョンの正本は`ProjectSettings/ProjectVersion.txt`と`global.json`
2. リポジトリを取得し、ルートディレクトリをUnity Hubから開く。Package Managerの解決とコンパイルを待ち、Unity生成の`.slnx`と`.csproj`を用意する
3. リポジトリのルートでセットアップスクリプトを実行する

macOS・Linux・Git Bash:

```bash
./scripts/setup.sh
```

Windows PowerShell:

```powershell
.\scripts\setup.ps1
```

スクリプトはローカル.NETツールを復元し、Gitフックとコミットテンプレートを設定する。UnityYAMLMergeを自動検出できない場合は、環境変数`UNITY_YAML_MERGE_PATH`に実行ファイルのパスを設定して再実行する。実行ポリシーなどの問題は[トラブルシューティング](troubleshooting.md)を参照。

ゲームをEditorで確認するときは`Assets/_Project/Scenes/Title.unity`を開き、Playする。ゲームの流れは[仕様書](../game/specifications.md)、確認手順は[テスト](testing.md)を参照。

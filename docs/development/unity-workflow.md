# Unity Workflow

## 目的

Unity特有の参照切れ、シーン競合、Prefab事故、`.meta`ファイルの欠落を防ぐための作業手順です。

## 作業前

1. 最新の`main`を取得する
2. 作業ブランチを作る
3. 編集予定のシーン・Prefab・ProjectSettingsを共有する
4. Unityバージョンが`6000.5.5f1`であることを確認する

## ファイル操作

Unityアセットの移動・名前変更・削除は、原則としてUnity EditorのProjectウィンドウで行ってください。

Unityは`.meta`ファイル内のGUIDで参照を管理しています。

OS上でアセットだけを移動・削除すると、Prefabやシーンからの参照が壊れる可能性があります。

## .metaファイル

`.meta`ファイルは必ずコミットします。

次の場合は特に注意してください。

- 新しいアセットを追加した
- フォルダを追加した
- アセットを移動した
- アセット名を変更した
- アセットを削除した

`.meta`だけが残った場合や、アセットだけが存在する場合は、操作ミスの可能性があります。

## シーン

シーンファイルは競合しやすいため、原則として同じシーンを複数人で同時編集しません。

推奨:

- シーン担当を決める
- 機能はPrefabとして作成する
- シーン担当がPrefabを配置する
- 必要なら作業用シーンで確認する

シーン変更を含むPRでは、意図しないオブジェクト移動や設定変更がないか確認してください。

## Prefab

繰り返し使用するGameObject、独立して開発したい機能、シーン競合を減らしたい機能はPrefab化を検討します。

Prefab変更時の確認:

- 意図しないOverrideがない
- Apply対象が正しい
- Missing Scriptがない
- Inspector参照が維持されている
- Variant元へ不要な変更を反映していない

Prefab Variantは、共通部分が明確で差分が小さい場合だけ使用してください。

## Play Mode

Play Mode中のInspector変更は、Play Mode終了時に原則として元へ戻ります。

調整値を保存したい場合は、値を記録してPlay Mode終了後に設定し直してください。

Play Mode中にPrefabへApplyする操作は、意図しない状態を保存する可能性があるため注意してください。

## Inspector参照

PR前に次を確認します。

- Noneになっている必須参照がない
- Missingになっている参照がない
- 対象PrefabやSceneに必要なComponentが付いている
- Layer、Tagが正しい
- Serialized Fieldの値が想定どおり
- Prefab InstanceのOverrideが想定どおり

## Console

PR前にConsoleを確認します。

- コンパイルエラーがない
- Play Mode中に例外が出ない
- Missing Script警告がない
- 大量の警告・ログが出続けない

既存の警告がある場合は、PR説明に既存問題であることを記載してください。

## ProjectSettings・Packages

`ProjectSettings/`または`Packages/`を変更する場合は、変更理由をPRへ明記してください。

特に注意するもの:

- Input System
- Tags and Layers
- Physics
- Graphics
- Quality
- Player Settings
- Package manifest
- Render Pipeline

無関係な設定変更が大量に含まれる場合は、Unityが自動保存した差分か確認してください。

## Git操作とUnity

通常のコミット・pushのために、毎回Unityを閉じる必要はありません。

ただし、次の場合はUnityを閉じるか、操作後に再読み込み状態を確認してください。

- 大量のブランチ切り替え
- AssetsやProjectSettingsの大規模変更
- パッケージ変更
- Unityバージョン変更
- 外部ツールによる大量のファイル移動
- マージ競合の解消

ブランチ切り替え後は、Unityの再インポートやコンパイルが完了してから作業を再開します。

## UnityYAMLMerge

Scene、PrefabなどUnity YAMLファイルの競合解消にはUnityYAMLMergeを使用します。

ただし、自動マージに成功しても、意味的に正しいとは限りません。

マージ後はUnity Editorで以下を確認してください。

- シーンが開ける
- Prefabが壊れていない
- Missing参照がない
- 意図した両方の変更が残っている

## PR前チェック

- [ ] Unity `6000.5.5f1`で開いた
- [ ] Consoleエラーがない
- [ ] Play Modeで対象機能を確認した
- [ ] Missing参照がない
- [ ] `.meta`ファイルを含めた
- [ ] シーン・Prefabの不要差分がない
- [ ] フォーマットを実行した
- [ ] pre-pushチェックが成功した
- [ ] 確認方法をPRへ記載した

# Directory Structure

## 目的

ファイルの配置場所を統一し、検索時間、競合、依存関係の混乱を減らします。

## 全体構成

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

## Assets/_Project

このプロジェクトで作成したアセットを配置します。

Unity標準、外部プラグイン、Asset Store由来のものとは分離します。

## Assets/_Project/Features

ゲームの機能単位でディレクトリを作成します。

例:

```text
Features/
├── Player/
│   ├── Scripts/
│   ├── Prefabs/
│   ├── Materials/
│   ├── Animations/
│   └── Audio/
├── Enemy/
├── Stage/
└── UI/
```

Feature内のサブディレクトリは、実際に必要になったものだけ作成してください。

空の`Scripts`、`Materials`、`Audio`などを先に大量作成しないでください。

## Assets/_Project/Shared

複数Featureで共有されるプロジェクト固有アセットを配置します。

```text
Shared/
├── Scripts/
├── Prefabs/
├── Materials/
├── Audio/
└── UI/
```

### Sharedへ置くもの

- 複数Featureから実際に使用される
- 特定Featureに所有させると不自然
- 共通化によって責務が明確になる

### Sharedへ置かないもの

- 将来使うかもしれないもの
- Playerだけが使う汎用風クラス
- 1つのFeatureからしか参照されないもの
- 所有先を決めるのが面倒という理由だけのもの

## Assets/_Project/Scenes

シーンを配置します。

```text
Scenes/
├── Bootstrap.unity
├── Title.unity
├── Game.unity
└── Result.unity
```

ゲーム内容が決まるまでは、必要以上にシーンを分割しません。

シーンファイルは競合しやすいため、編集担当を明確にしてください。

## Assets/_Project/Settings

プロジェクト固有の設定アセットを配置します。

例:

- Input Actions
- ScriptableObject設定
- Render Pipeline関連のプロジェクト固有設定
- ゲームバランス設定

Unityの`ProjectSettings/`ディレクトリとは別物です。

## Assets/Plugins

Unityの規約上、`Plugins`への配置が必要なネイティブプラグインやDLLを置きます。

理由なく自作コードを置かないでください。

## Assets/ThirdParty

Asset Store、外部配布素材、外部ライブラリなどを配置します。

可能な限り配布元のディレクトリ構成を維持し、自作コードと混在させません。

ライセンス情報が必要な場合は、同じディレクトリまたはリポジトリ内の
ライセンス文書へ記録してください。

## Editorコード

Editor専用コードは、Unityが認識できる`Editor`ディレクトリへ配置します。

例:

```text
Features/Player/Editor/
Shared/Editor/
```

実行時コードからEditorコードを参照しないでください。

## Tests

テストを追加する場合は、対象Featureの近くへ配置します。

```text
Features/Player/Tests/EditMode/
Features/Player/Tests/PlayMode/
```

複数Featureを横断するテストは`Shared/Tests/`または専用の統合テスト領域を
検討します。

## asmdef

asmdefは必要性が明確になってから導入します。

導入を検討する条件:

- コンパイル範囲を分離したい
- EditorコードとRuntimeコードを明確に分けたい
- テストアセンブリが必要
- Feature間依存をコンパイル時に制約したい

1週間プロジェクトでは、asmdefの分割自体が目的にならないよう注意してください。

## ファイル移動

Unityアセットの移動・名前変更は、原則としてUnity EditorのProjectウィンドウで
行います。

OSのファイルマネージャーで移動すると、`.meta`ファイルとの対応やGUID参照を
壊す可能性があります。

## 新しい配置先に迷った場合

次の順番で判断します。

1. どのゲーム機能が所有するか
2. そのFeature内に置けるか
3. 複数Featureで実際に共有されているか
4. Unityの規約上、特殊なディレクトリが必要か
5. 判断できなければ、勝手にSharedへ置かず相談する

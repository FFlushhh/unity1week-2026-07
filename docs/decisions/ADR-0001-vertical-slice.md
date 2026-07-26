# ADR-0001: Vertical Slice構成を採用する

## 状態

採用

## 日付

2026-07-26

## 背景

本プロジェクトは3人で1週間の開発を行います。

Unity初心者を含むため、担当範囲を理解しやすくし、同時作業時のファイル競合を減らす必要があります。

従来型の技術別構成では、すべてのスクリプトが`Scripts`、すべてのPrefabが
`Prefabs`へ集まり、1つの機能に関係するファイルが複数ディレクトリへ分散します。

## 決定

`Assets/_Project/Features/`配下へ、Player、Enemy、Stage、UIなどの機能単位で
ファイルを配置するVertical Slice構成を採用します。

```text
Assets/_Project/Features/
├── Player/
├── Enemy/
├── Stage/
└── UI/
```

各Featureのスクリプト、Prefab、Material、Animation、Audioは、原則として同じFeature内へ配置します。

複数Featureから実際に利用されるものだけを`Assets/_Project/Shared/`へ配置します。

## 理由

- 担当範囲を分けやすい
- 機能に関係するファイルを探しやすい
- 異なる機能の同時編集で競合しにくい
- 不要なFeature間依存を発見しやすい
- AIエージェントが変更範囲を判断しやすい
- 短期開発でも理解しやすい

## 代替案

### 技術種別で分ける

```text
Assets/
├── Scripts/
├── Prefabs/
├── Materials/
└── Audio/
```

Unity初心者には見慣れた構成ですが、機能に関係するファイルが分散し、担当境界が曖昧になります。

### 完全なレイヤードアーキテクチャ

Domain、Application、Infrastructureなどに分ける案です。

1週間のゲーム制作には学習・実装コストが高く、完成を妨げる可能性があるため採用しません。

## 影響

### 良い影響

- 機能単位で作業しやすい
- PRの変更範囲が把握しやすい
- Feature削除や変更がしやすい
- 技術種別をまたいだ検索が減る

### 注意点

- Sharedへ何でも移すと、従来型構成と同じ問題が再発する
- Feature間依存のルールが必要
- 共通アセットの所有先に迷う場合がある
- Unityの特殊フォルダ規約は優先する必要がある

## 運用

配置先に迷った場合は、まず所有するFeatureを決めます。

Sharedへの移動は、複数Featureから実際に利用されることを確認してから行います。

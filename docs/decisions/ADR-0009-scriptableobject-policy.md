# ADR-0009: ScriptableObjectの採用基準を定める

## 状態

採用

## 日付

2026-07-26

## 背景

ScriptableObjectはUnityでデータを共有・編集するために便利ですが、すべての設定を
ScriptableObject化すると、ファイル数、参照、Inspector操作が増えます。

短期開発では、利用基準を決めないと過剰設計になりやすいため方針が必要です。

## 決定

ScriptableObjectは、次の条件を満たす場合に使用します。

- 複数のPrefab、Scene、Componentから同じ設定値を共有する
- デザイナーまたは開発メンバーがInspectorから調整する必要がある
- Prefabごとの重複設定を避ける価値がある
- 実行時状態ではなく、基本的に設定データとして扱える
- データをコードから分離することで理解しやすくなる

次の用途では、原則として使用しません。

- 1つのComponentからしか使わない単純な値
- `const`や`static readonly`で十分な固定値
- GameObject固有の参照
- 頻繁に変化する実行時状態
- 将来使うかもしれないだけの汎用設定
- すべてのフィールドをデータ駆動化する目的

## 理由

ScriptableObjectの利点を活かしつつ、アセット数と参照管理の複雑さを抑えるためです。

## 代替案

### すべてSerializeFieldへ置く

単純ですが、複数Prefab間で値が重複し、調整漏れが発生する場合があります。

### すべてScriptableObject化する

再利用性は高まりますが、1週間開発では管理負担が増えるため採用しません。

## 影響

ScriptableObjectを追加するPRでは、次を説明します。

- 共有する対象
- ScriptableObjectにする理由
- 実行時状態を保持しないか
- 所有するFeatureまたは配置先

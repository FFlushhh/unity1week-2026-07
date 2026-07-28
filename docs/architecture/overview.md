# Architecture Overview

## 目的

本プロジェクトで採用する設計方針をまとめます。

このプロジェクトでは、1週間で完成させること、3人が並行作業しやすいこと、
Unity初心者でもレビューできることを重視します。

## 基本方針

- Vertical Slice構成を採用する
- 機能ごとにファイルをまとめる
- 機能間の直接依存を増やしすぎない
- Feature間の境界では、必要に応じて小さなinterfaceと手動DIを使用する
- MonoBehaviourはUnityとの接点として使用する
- ゲームロジックは必要に応じて通常のC#へ分離する
- 将来の拡張を理由に過剰設計しない
- 既存の単純な実装を、理由なく複雑な設計へ置き換えない
- 完成を最優先とする

## Vertical Slice

Player、Enemy、Stage、UIなど、ゲームの機能単位でファイルを配置します。

```text
Assets/_Project/Features/
├── Player/
├── Enemy/
├── Stage/
└── UI/
```

各Featureには、その機能だけで使用するものをまとめます。

例:

```text
Player/
├── Scripts/
├── Prefabs/
├── Materials/
├── Animations/
└── Audio/
```

これにより、担当範囲が分かりやすくなり、異なる機能を担当するメンバー同士の
競合を減らします。

## Shared

複数機能から利用されるものは`Assets/_Project/Shared/`へ配置できます。

ただし、最初からSharedへ置かないでください。

Sharedへ移す基準:

- 2つ以上のFeatureから実際に利用されている
- 特定のFeatureに所有させると不自然
- 共通化によって依存関係が分かりやすくなる
- 短期開発でも保守負担が減る

似た処理が2つ存在するだけでは、共通化しません。

## MonoBehaviour

MonoBehaviourは主に次を担当します。

- Unityライフサイクル
- Inspector参照
- GameObjectやTransformの操作
- Physics、Animator、AudioSourceなどのUnity API
- UnityEventや衝突イベントの受信

複雑な計算、状態判定、ルールは、必要に応じて通常のC#クラスや小さなメソッドへ
分離します。

ただし、単純な処理を無理に分離してファイル数を増やさないでください。

## 依存方向

原則として、各Featureは他Featureの内部実装へ直接依存しません。

推奨:

- Inspectorから必要な参照を渡す
- 明確なCoordinatorまたは所有元から参照を渡す
- 小さなインターフェースを必要な場合だけ使用する
- イベントは依存削減に効果がある場合だけ使用する

非推奨:

- `GameObject.Find`で他Featureを探す
- Scene内の存在を暗黙に前提とする
- どこからでも参照できる巨大なManagerを作る
- すべてをSingletonにする

## Feature間の契約とDI

担当者が異なるFeatureを並行開発するときは、境界となる小さなinterfaceを先に
合意できます。interfaceには、メソッドの意味、呼び出しタイミング、null可否、
失敗時の挙動を含めます。

依存関係は、次の方法で明示的に渡します。

- Inspector参照
- メソッドまたは通常のC#クラスのコンストラクタ引数
- BootstrapやCoordinatorなど、明確な所有元での手動接続

DIコンテナは導入しません。Feature内だけで完結し、差し替えや並行開発の必要が
ない処理まで機械的にinterface化しないでください。

## Manager

Managerという名前自体は禁止しませんが、責務を明確にしてください。

悪い例:

- GameManagerが入力、スコア、敵生成、UI、シーン遷移をすべて担当する

良い例:

- ScoreManagerがスコア状態だけを管理する
- StageFlowがゲーム進行だけを制御する

ただし、1週間の小規模ゲームであれば、進行制御を1つのクラスへまとめることは
許容します。クラス名と責務が一致していることを重視します。

## ScriptableObject

ScriptableObjectは次の用途に限定して検討します。

- 複数オブジェクトから共有する設定値
- Inspectorで編集したいデータ
- Prefabへ重複設定したくないデータ

単一箇所でしか使わない値や、単純な定数のためだけには導入しません。

## 採用しないもの

明確な必要性が出るまでは、次を導入しません。

- DIコンテナ
- ECS/DOTS
- 複雑なイベントバス
- 独自フレームワーク
- 過剰なRepository・Service層
- 将来機能のための抽象基底クラス
- 汎用化されたゲームエンジン層

## 変更方法

この方針を変更する場合は、理由、影響、代替案をADRとして`docs/decisions/`へ
記録してください。

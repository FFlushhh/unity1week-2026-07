# Coding Style

## 目的

短期間で読みやすく安全にレビューできるコードを書くための基準です。

C#の整形はCSharpierに従います。この文書では、フォーマッターでは決められない
設計・命名・Unity固有ルールを扱います。

## アクセス修飾子

外部公開が不要なものは`private`を基本とします。

```csharp
[SerializeField]
private float moveSpeed = 5f;
```

`public`フィールドは原則として使用しません。

外部から読み取りだけ許可したい場合:

```csharp
public int Score { get; private set; }
```

## SerializeField

Inspectorから設定する必要があるフィールドは、`[SerializeField] private`を
使用します。

```csharp
[SerializeField]
private Rigidbody playerRigidbody;
```

`SerializeField`は「初心者向けの一時的な書き方」ではありません。Unityで参照や
設定値を安全に保持する標準的な方法です。

## 命名

- 型、メソッド、プロパティ: PascalCase
- privateフィールド、ローカル変数、引数: camelCase
- bool: 状態や判定が分かる名前
- 単位が重要な数値: 名前に単位を含める

```csharp
private bool isGrounded;
private float invincibleDurationSeconds;
```

避ける名前:

```text
data
temp
manager
obj
flag
value
```

短いスコープのループ変数など、意味が明確な場合は例外です。

## MonoBehaviour

MonoBehaviourに処理を追加する前に、Unity APIが必要か確認してください。

MonoBehaviourに置きやすい処理:

- `Awake`
- `Start`
- `Update`
- `OnTriggerEnter`
- Transform操作
- Animator操作
- Inspector参照の利用

通常のC#へ分離しやすい処理:

- スコア計算
- ダメージ計算
- 勝敗判定
- 状態遷移のルール
- 入力値から移動量を計算する処理

ただし、分離によって理解しにくくなる小さな処理は、そのままMonoBehaviour内に
置いて構いません。

## Update

`Update`では、毎フレーム必要な処理だけを実行します。

避けるもの:

- `GameObject.Find`
- 毎フレームのLINQ
- 毎フレームの大きな配列生成
- 毎フレームのコンポーネント検索
- 不要な文字列生成

参照は`Awake`、Inspector、生成時などに取得してください。

## null

Inspector参照が必須の場合は、設定漏れを早期に検出できるようにします。

```csharp
private void Awake()
{
    if (playerRigidbody == null)
    {
        Debug.LogError("Player Rigidbody is not assigned.", this);
    }
}
```

すべてのフィールドに機械的なnullチェックを追加する必要はありません。実際に
参照切れが起きたとき、原因を特定しにくい箇所を優先します。

## Find系API

次のAPIは、初期化や一時的な試作を除いて常用しません。

- `GameObject.Find`
- `FindObjectOfType`
- `FindFirstObjectByType`
- `FindAnyObjectByType`
- `Transform.Find`

推奨される代替:

- Inspector参照
- Prefab生成時に参照を渡す
- 所有元からメソッド引数で渡す
- 小さなCoordinatorが参照を保持する

## Singleton

便利さだけを理由にSingletonを作らないでください。

Singletonを検討できるもの:

- アプリ全体で明確に1つだけ存在する
- ライフサイクルが明確
- テストやシーン遷移を不必要に難しくしない

1週間プロジェクトでは、シーン内の明確な所有オブジェクトから参照を渡す方を
優先します。

## コメント

処理内容をそのまま日本語で言い換えるコメントは避けます。

悪い例:

```csharp
// スコアを1増やす
score++;
```

良い例:

```csharp
// 同じコインのTriggerが複数回発火する可能性があるため、取得済みを先に記録する。
isCollected = true;
```

## 例外処理

例外を握りつぶさないでください。

```csharp
try
{
    // ...
}
catch
{
}
```

Unityの通常フローで発生しうる状態は、例外ではなく条件分岐で扱います。

## ログ

- 通常プレイで大量に出る`Debug.Log`は残さない
- エラーは原因と対象オブジェクトが分かるようにする
- 一時デバッグログはPR前に削除する
- 意図的に残すログは理由をコメントまたはPRへ記載する

## クラス分割

分割を検討する目安:

- 1クラスが入力、移動、攻撃、UI更新など複数の責務を持つ
- 変更理由が複数ある
- レビュー時に全体を把握しにくい
- Unity API不要なルールが大量に含まれる

ただし、行数だけを理由に分割しません。

## 過剰抽象化を避ける

以下は必要性が出るまで作らないでください。

- Feature間の契約、並行開発、テストでの差し替えのいずれにも使わない
  インターフェース
- 将来の敵種類のための複雑な継承階層
- 汎用イベントシステム
- Service Locator
- FactoryのためのFactory
- すべての値をScriptableObject化する仕組み

## フォーマット

CSharpierの結果を正とします。

手動でフォーマッターと異なる整形へ戻さないでください。

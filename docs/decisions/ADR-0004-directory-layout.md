# ADR-0004: ディレクトリ構成

## 状態

採用

## 決定

`Assets/_Project/`配下を自作コード・アセットのルートとし、ADR-0001で採用した
Vertical Slice構成に従ってFeature単位で管理する。

```text
Assets/_Project/
├── Features/
│   ├── Player/
│   ├── Enemy/
│   ├── Stage/
│   └── UI/
├── Shared/
├── Scenes/
└── Settings/
```

スクリプト、Prefab、Material、Animation、Audioなどを技術種別ごとに
プロジェクト全体へ分散させず、それらを利用するFeature内へまとめる。
複数Featureから実際に利用されるものだけを`Shared`へ配置する。

Unityの規約上分離が必要なPluginsと、外部由来のThirdPartyは
`Assets/_Project/`の外に配置する。

先頭に`_`を付けることで、UnityのProjectウィンドウ上で自作アセットのルートを見つけやすくし、Unity標準・外部アセットと区別する。

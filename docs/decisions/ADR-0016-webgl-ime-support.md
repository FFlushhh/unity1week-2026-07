# ADR-0016: WebGLでのIME（日本語入力）対応にWebGLSupportパッケージを採用

## 状況

unity1weekプロジェクトはWebGL向けにビルドされるが、Unityの標準機能ではWebGLビルド上の `TMP_InputField` へのIME（日本語入力）に対応していない。そのため、TitleScene等でプレイヤー名の入力などに日本語を利用できない問題が発生した。

## 選択肢

1. **WebGLSupport (WebGLInput) パッケージの導入 (採用)**
   - kou-yeung氏によるOSSパッケージ（`https://github.com/kou-yeung/WebGLInput`）。
   - Unity WebGLにおけるIME対応として広く使われている。
   - スクリプトからコンポーネントをアタッチするだけで機能する。

2. **日本語入力を諦める**
   - プレイヤー名の入力という要件を満たせない。

## 決定

**WebGLSupport (WebGLInput)** を導入し、WebGLビルド環境でのみ必要な `InputField` へ動的に `WebGLInput` コンポーネントをアタッチする運用とする。

## 影響

- プロジェクトの依存関係 (`manifest.json`) に `com.github.kou-yeung.webglinput` が追加される。
- UIの入力を担当するスクリプト（例: `TitleNameInput.cs`）において、WebGLビルド時のみ `WebGLSupport.WebGLInput` を追加する処理が必要になる。
- これにより、エディタ上およびWebGLビルド後の両方で日本語入力が可能となる。

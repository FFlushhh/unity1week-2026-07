# ADR-0002: Git品質ゲートを採用する

## 状態

採用

## 背景

1週間開発で品質を一定に保つ。

## 決定

- pre-commit: CSharpier・Unity Analyzer
- pre-push: C#コンパイル
- GitHub Actions: フォーマット・Semgrep

## 理由

レビュー前に基本品質を保証するため。

# Documentation

## 目的

このディレクトリには、プロジェクトの仕様、設計、開発手順、判断記録を置きます。

READMEには概要だけを置き、詳細はこのディレクトリを正本とします。

## 初めて開発に参加する人

1. [プロジェクトREADME](../README.md)
2. [開発への参加方法](../CONTRIBUTING.md)
3. [アーキテクチャ概要](architecture/overview.md)
4. [ディレクトリ構成](architecture/directory-structure.md)
5. [Unity開発フロー](development/unity-workflow.md)
6. [ゲーム概要](game/overview.md)
7. [ゲーム仕様](game/specifications.md)

## 実装時に確認する文書

- [アーキテクチャ概要](architecture/overview.md)
- [ディレクトリ構成](architecture/directory-structure.md)
- [コーディングスタイル](architecture/coding-style.md)
- [Unity開発フロー](development/unity-workflow.md)
- [ゲーム仕様](game/specifications.md)

## レビュー時に確認する文書

- [レビューガイド](development/review-guide.md)
- [Unity開発フロー](development/unity-workflow.md)
- [ゲーム仕様](game/specifications.md)

## 困ったとき

- [トラブルシューティング](development/troubleshooting.md)
- [未決定事項](game/todo.md)
- [設計判断記録](decisions/README.md)

## AIエージェント

AIエージェントは、実装前に[AGENTS.md](../AGENTS.md)を確認してください。

## 更新ルール

- 実装と仕様変更は同じPull Requestで更新する
- 同じ内容を複数ファイルへ重複して書かない
- 未決定事項を確定事項のように書かない
- 重要な設計変更はADRとして記録する
- 古い記述を残したまま新しい記述を追加しない

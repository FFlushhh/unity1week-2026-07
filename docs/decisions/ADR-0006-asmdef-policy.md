# ADR-0006: asmdef運用方針

## 状態

採用

## 決定

asmdefは必要になるまで導入しない。

現在はStage、Result、SoundSettingのRuntimeとテストに必要なasmdefを導入済み。

## 導入条件

- コンパイル時間短縮
- Editor/Runtime分離
- テスト追加

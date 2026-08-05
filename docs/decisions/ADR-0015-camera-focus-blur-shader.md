# ADR-0015: カメラのピント合わせ演出にUIブラーシェーダを採用する

## 状態

採用

## 日付

2026-08-03

## 背景

`Game_Stage1`へ遷移した直後に、4:3プレビュー映像へぼかしを適用し、撮影タイム開始までに
徐々に解除する「ピント合わせ演出」を追加する必要があります。要件は次のとおりです。

- 4:3プレビューだけにぼかしを適用し、カメラUI・タイマー・開始メッセージには適用しない。
- 撮影画像およびResultへ渡す画像にぼかしを焼き込まない。
- 演出中も被写体の生成・移動は既存どおり進行する。

一般的にはURPのポストプロセス（Volume＋Depth of Field）でこの種の演出を実装しますが、
本プロジェクトは`Universal2D Renderer`を採用しており、`m_RenderPostProcessing: 0`かつ
Volume・RendererFeatureを1つも使用していません。加えてURPの2D RendererはDepth of Field
を含むポストプロセスの多くを正式サポートしていません。

## 決定

- PhotoCameraやRenderTextureには一切手を加えず、`PhotoPreview`の`RawImage`に自作の
  UIシェーダ（`Stage/PhotoPreviewBlur`）を実行時だけ割り当てる方式を採用する。
- シェーダは13タップの円形ディスク近似（中心1点＋内周6点＋外周6点）でぼかしを表現し、
  `RectMask2D`・`ZTest`・Stencilなど`UI/Default`互換の構成を保つ。
- ぼかしの強度・半径は`_BlurStrength`／`_MaxBlurRadiusPixels`などのマテリアルプロパティで
  制御し、`StagePhotoFocusPresentation`が`UniTask`で毎フレーム更新する。
- 共有マテリアル資産（`PhotoPreviewBlur.mat`）を実行時に書き換えないよう、
  `new Material(...) { hideFlags = HideFlags.DontSave }`で複製したインスタンスにのみ
  値を設定する。シーンの`RawImage.m_Material`は`{fileID: 0}`のまま維持する。
- 演出完了後（撮影タイム開始後）は`RawImage.material`を`null`に戻し、既定のUIマテリアルへ
  復帰させる。これにより撮影タイム中の描画コスト増加をゼロにする。
- `PhotoPreview.renderTexture`のミップマップを有効化し、大きい半径でのゴースト（多重像）を
  抑える。撮影処理は常にミップ0（フル解像度）を`ReadPixels`するため、撮影画像の解像度・
  鮮明さには影響しない。

## 理由

- 撮影は`PhotoCamera.targetTexture`を直接`ReadPixels`する経路であり、UIマテリアルは
  この経路に一切登場しない。そのため「撮影画像にぼかしを含めない」という要件を、
  実装を分けて注意深く維持する必要なく、構造的に満たせる。
- 既存の`StagePhotoCapturePresentation`（シャッター演出）と同じ`UniTask`＋
  `CancellationTokenSource`の契約に揃えられるため、レビューコストと保守コストが低い。
- 新規Cameraや追加RenderTextureを必要とせず、Scene変更が既存GameObjectへの
  コンポーネント追加のみで済む。

## 代替案

### URP Volume + Depth of Field

標準的な手段ですが、本プロジェクトが採用する2D RendererはDepth of Fieldを正式サポート
していません。Volume・RendererFeatureも未使用であり、導入コストと動作保証のリスクが
残り開発期間に対して大きいため採用しません。

### ダウンサンプルRenderTexture + Graphics.Blit方式

低解像度のRenderTextureへ段階的に縮小し、それを重ねてクロスフェードする方式も検討
しました。自作シェーダーを増やさずに済みますが、Blit用の中間RenderTextureの生成・解放を
毎回自前で管理する必要があり、リーク・カラースペースの不整合など運用時のリスクが増えます。
UIシェーダ方式は資産追加が1本のシェーダーとマテリアルだけで済み、後始末もマテリアルの
アタッチ/デタッチのみで完結するため、こちらを採用します。

### 追加カメラで低解像度描画する方式

被写体の輪郭がジャギーになりやすく、「ぼかし」ではなく「モザイク」に近い見た目になるため
採用しません。

## 影響

- `Assets/_Project/Features/Stage/Shaders/PhotoPreviewBlur.shader`と
  `Assets/_Project/Features/Stage/Materials/PhotoPreviewBlur.mat`が新規の資産として増える。
- `Project.Stage`アセンブリにURP関連の参照を追加する必要はない（シェーダはURP APIを
  使わないUIシェーダのため）。
- `PhotoPreview.renderTexture`のミップマップ有効化は、将来このRenderTextureを別用途で
  再利用する場合にもミップコストが常に発生する点に注意する。

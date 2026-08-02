# iOS 26-inspired landscape camera UI assets

Unityの1920×1080基準Canvasへ重ねるための、横向きカメラUI素材です。Appleの公開資料から確認できるiOS 26の簡素化されたカメラ構成とLiquid Glass表現を参考にしていますが、Apple純正UIの完全な複製ではありません。指定に合わせ、画面正面から見てシャッターボタンを左側へ配置しています。

## 収録内容

- `camera_ui_complete.png`: 完成状態のUIオーバーレイ（1920×1080、中央とサムネイル内部は完全透過）
- `camera_ui_frame.png`: ボタンを含まない左右フレーム（1920×1080）
- `shutter_button.png`: シャッターボタン
- `thumbnail_frame.png`: 前回撮影画像の円形フレーム（内部は完全透過）
- `camera_switch_button.png`: 前面／背面カメラ切り替え
- `flash_button.png`: フラッシュ
- `live_photo_button.png`: Live Photo風の撮影状態ボタン
- `timer_button.png`: タイマー
- `exposure_button.png`: 露出
- `styles_button.png`: 撮影スタイル
- `aspect_ratio_button.png`: アスペクト比
- `camera_controls_menu_button.png`: カメラ設定メニュー
- `photo_video_mode_selector.png`: PHOTO／VIDEOモード選択
- `zoom_selector.png`: 0.5×／1×／2×ズーム選択
- `secondary_controls_sheet.png`: 設定系6部品の生成元シート
- `all_controls_sprite_sheet.png`: 全12部品の4×3スプライトシート（2048×1536、1セル512×512）
- `unity_layout_1920x1080.json`: 推奨配置とスプライト切り出し情報

## Unity推奨設定

- Canvas Scaler: `Scale With Screen Size`
- Reference Resolution: `1920 × 1080`
- Screen Match Mode: `Match Width Or Height`
- Match: `0.5`
- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: 個別PNGは`Single`、スプライトシートは`Multiple`
- Alpha Is Transparency: `On`
- Mesh Type: UI用途では`Full Rect`
- Wrap Mode: `Clamp`
- Filter Mode: `Bilinear`
- Compression: 輪郭を優先する場合は`None`、容量を優先する場合は用途に合わせて調整

推奨座標は`unity_layout_1920x1080.json`に記録しています。`screenTopLeft`は画面左上を `(0, 0)` とするピクセル座標、`unityCenterAnchor`は中央アンカーを基準にした`anchoredPosition`です。

## 透過について

- `camera_ui_complete.png`と`camera_ui_frame.png`の中央ピクセルはアルファ値0です。
- `thumbnail_frame.png`の中央ピクセルはアルファ値0です。
- 半透明のガラス部分には中間アルファ値が含まれます。
- 生成時のクロマ背景は最終PNGから除去済みです。

## 注意点

- 横向きUIの正確なピクセル寸法はAppleから公開されていないため、配置と寸法は1920×1080ゲーム画面向けの推奨値です。
- Liquid Glassの屈折は静止PNGとして表現しています。Unity上で実時間の背景ぼかしや屈折を再現する場合は、別途シェーダーまたはUI Blurが必要です。
- `PHOTO`、`VIDEO`、ズーム倍率は画像へ焼き込まれています。文言や選択状態を動的に変える場合は、Unity UIのテキストと選択背景に置き換えてください。


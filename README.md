# タマゴインキュベータ

![タマゴインキュベータ タイトル](Assets/Art/Images/ゲームタイトル画像.png)

スマートフォンを傾けてフライパンを操作し、暴れるタマゴを落とさずに孵化させる、縦持ちの1人用バランスアクションゲームです。

ただ耐えるだけでは孵化は進みません。フライパン上を移動する「スイートスポット」にタマゴを乗せ続けると孵化が進みますが、スポットは縁寄りへ動き、滞在中ほどタマゴも激しく暴れます。「安全を取るか、危険を承知で孵化を進めるか」という判断を短いプレイの中に組み込みました。

## 基本情報

| 項目 | 内容 |
| --- | --- |
| ジャンル | スマートフォン向けバランスアクション |
| プレイ人数 | 1人 |
| 対応端末 | iPhone / iPad（iOS 15.0以降） |
| 画面 | 縦持ち |
| 開発環境 | Unity 6.3 LTS（6000.3.22f1） / C# |
| 主な技術 | URP、Input System、DOTween、iOSジャイロ・加速度センサー |
| 開発体制 | 3人チーム |
| バージョン | 0.1 |

## 遊び方

1. `EASY` / `NORMAL` / `HARD` から難易度を選び、ゲーム開始をタップします。
2. 端末を左右・前後へ傾けてフライパンを操作します。
3. 移動するスイートスポットへタマゴを運び、落とさないように保ちます。
4. 必要時間スポットに滞在できれば孵化成功、フライパンから落ちると失敗です。
5. 3難易度をすべてクリアすると、最難関の `EXTRA` が解放されます。

起動時の端末姿勢を水平基準として自動校正します。持ち方を変えた場合は、プレイ画面のリセットボタンで基準姿勢を取り直せます。

## ゲーム設計・実装上のポイント

- `TITLE → INTRO → PLAYING → FALLING / HATCHING → RESULT` の状態遷移を中心に、入力・タイマー・物理・演出を分離しました。
- ジャイロの重力ベクトルを基準姿勢に垂直な平面へ射影し、端末を寝かせても立てても同じ感覚で傾きを取得します。
- 難易度ごとに制限時間、タマゴの力・頻度・速度上限、中央アシスト、スイートスポットの挙動を変えています。
- スイートスポットは、孵化速度が上がる一方でタマゴの暴れも強くなる、ハイリスク・ハイリターンの仕組みです。
- 危険度に応じて心音、振動、カメラ、ビネット、ヒビ発光を同期させ、落下・破壊・孵化時には専用のフィードバックを再生します。
- 落下後の復帰不能、連続入力による即リトライ、センサー値の急変、物理速度の飽和など、実機で起きる境界条件に対する安全処理を入れています。

## 制作メンバーと担当

| メンバー | 主な担当 |
| --- | --- |
| 伊藤俊生 | ゲーム進行・状態管理、タイマー／進行度、難易度／危険度、全体統合、スイートスポット、UI・演出調整、実機向け品質改善 |
| 川瀬莉平 | フライパンのジャイロ操作、タマゴの基本物理挙動、落下・衝突判定 |
| 松崎正弘 | ヒビ割れ・落下破壊・孵化表現、音・振動・カメラを含むフィードバック |

担当欄はリポジトリのコミット履歴と実装仕様書を基に整理しています。最終的な `GameScene` への統合と品質改善はチームで確認しながら進めました。

## 伊藤俊生の主な実装

- `GameManager` を中心としたゲームループと成功・失敗・リトライ処理
- `TimerManager` / `DifficultyManager` / `DifficultySelector` による進行度と難易度制御
- 位置ベースの危険度算出と、演出レイヤーへ渡す公開API
- `SweetSpotController` の導入、およびリスクと報酬が両立するルールへの再設計
- 難易度選択、クリア記録、`EXTRA` 解放、残り時間、操作ガイド、クレジットなどのUI
- 卵・フライパン・演出・UIを `GameScene` に統合し、1プレイの流れを完成
- 端末の持ち方に依存しにくい傾き計算、物理挙動、カメラ、振動、リトライ周りの不具合修正

## ソースコードの見どころ

| パス | 内容 |
| --- | --- |
| `Assets/Scripts/Gameplay/GameManager.cs` | 状態遷移とゲーム全体のオーケストレーション |
| `Assets/Scripts/Gameplay/DifficultyManager.cs` | 進行度から難易度パラメータを連続生成 |
| `Assets/Scripts/Gameplay/DifficultySelector.cs` | 難易度プリセット、クリア記録、EXTRA解放 |
| `Assets/Scripts/Gameplay/SweetSpotController.cs` | 移動エリア、滞在判定、リスク・報酬制御、見た目と音 |
| `Assets/Scripts/Input/PanController.cs` | ジャイロ／加速度を使ったフライパン操作と姿勢校正 |
| `Assets/Scripts/Input/EggController.cs` | タマゴの物理挙動、速度制限、中央アシスト |
| `Assets/Scripts/Feedback/FeedbackController.cs` | 音・振動・カメラ・VFXの統括 |
| `Assets/Shaders/SweetSpotArea.shader` | スイートスポットの手続き的な発光表現 |

## Unityで開く

1. Unity Hub に `Unity 6.3 LTS (6000.3.22f1)` を追加します。
2. このフォルダを Unity Hub から開きます。
3. `Assets/Scenes/GameScene.unity` を開きます。
4. iOS Build Support を追加し、Build Profiles から iOS を選択してビルドします。
5. 生成された Xcode プロジェクトを開き、Signing & Capabilities で自身の Team と Bundle Identifier を設定して実機へ転送します。

本作は端末センサーを使うため、最終確認はiOS実機で行ってください。Unity Editorではタイトル、UI、状態遷移などは確認できますが、通常のプレイ操作には端末センサーが必要です。

## ビルド設定

- 起動シーン: `Assets/Scenes/GameScene.unity`
- Bundle Identifier: `com.teamd.tamagoincubator`
- Minimum iOS Version: `15.0`
- Orientation: Portrait
- Scripting Backend: IL2CPP（iOS）

## 主な外部ライブラリ・素材

- DOTween
- M PLUS Rounded 1c（SIL Open Font License 1.1）
- TextMesh Pro
- SAColliderBuilder

各ライブラリ・素材のライセンスおよび同梱文書は、それぞれの配置フォルダを参照してください。

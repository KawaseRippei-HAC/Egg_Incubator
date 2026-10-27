# タマゴインキュベータ

![タマゴインキュベータ タイトル](Assets/Art/Images/ゲームタイトル画像.png)

## 制作時期とゲーム概要
2026/8/29,30 に開催されたサイバーエージェント様のインターン「プロトスプリントリーグ2026」で制作した、「スマートフォン向けバランスアクション」です。
スマートフォンを傾けてフライパンを操作し、暴れるタマゴを落とさずに孵化させる、縦持ちの1人用バランスアクションゲームです。

ただ耐えるだけでは孵化は進みません。フライパン上を移動する「スイートスポット」にタマゴを乗せ続けると孵化が進みますが、スポットは縁寄りへ動き、滞在中ほどタマゴも激しく暴れます。「安全を取るか、危険を承知で孵化を進めるか」という判断を短いプレイの中に組み込みました。

## 📝目標
- **チーム目標**
  「文字や言葉を使わず、非言語的コミュニケーションで伝わるユニークな体験をつくる」
- **個人目標**
  「プレイヤーが直感的に操作でき、理不尽さを感じさせずに終盤に向けて緊張感が高まる『絶妙なバランスの卵の挙動』を実現する」

## 🎮ゲーム概要

| 項目 | 内容 |
| :--- | :--- |
| **ジャンル** | スマートフォン向けバランスアクション |
| **プレイ人数** | 1人 |
| **対応端末** | iPhone / iPad（iOS 15.0以降） |
| **画面** | 縦持ち |
| **開発環境** | Unity 6.3 LTS（6000.3.22f1） / C# |
| **主な技術** | URP、Input System、DOTween、iOSジャイロ・加速度センサー |
| **開発体制** | 3人チーム（プログラマー3人） |
| **自身の役割** | PG（タマゴの挙動・ジャイロ操作・難易度調整・ver管理）|

## 📷プレイ画面
**動画リンク:** [プレイ動画(Google Drive)](https://x.gd/BWg2x)

- プレイ画面の上のゲージを満たすと孵化(クリア)
- スイートスポットに乗っていると孵化ゲージが進む

<table>
  <tr>
    <td align="center">
      <b>タイトル画面</b><br>
      <img width="216" height="466" alt="Title" src="https://github.com/user-attachments/assets/a2b455e9-b361-4593-a07d-e412f39a5e75" />
    </td>
    <td align="center">
      <b>通常時</b><br>
      <img width="216" height="466" alt="PlayScreen" src="https://github.com/user-attachments/assets/440fac93-4c1f-4aa1-8e23-0bb98e2cf451" />
    </td>
  </tr>
  <tr>
    <td align="center">
      <b>スイートスポット時</b><br>
      <img width="216" height="466" alt="SweetSpot" src="https://github.com/user-attachments/assets/53cf73ab-45fb-4c9b-833d-6877b1944aeb" />
    </td>
    <td align="center">
      <b>クリア時リザルト</b><br>
      <img width="216" height="466" alt="Result_Clear" src="https://github.com/user-attachments/assets/fa67d52a-3304-4bbe-9ac5-e5e5f77a1806" />
    </td>
  </tr>
</table>

## 💻担当箇所 (ソースコード)
主にタマゴの物理挙動とジャイロセンサーを利用した入力制御、およびゲームの難易度管理を実装しました。

[Assets/Scripts/Input](https://github.com/KawaseRippei-HAC/Egg_Incubator/tree/main/Assets/Scripts/Input)
- **`Scripts/Input/EggController`**<br>
  タマゴの基本物理挙動および、時間経過や難易度に応じて内部から暴れる処理を制御するスクリプトです。速度や角速度に上限を設け、想定外の吹き飛びを防ぐ安全制御も実装しています。
- **`Scripts/Input/EggFallDetector`**<br>
  タマゴがフライパンの判定外に出た（落下した）ことを検知するスクリプトです。落下時にカメラを追従させる演出のトリガーとしても機能します。
- **`Scripts/Input/PanController`**<br>
  スマートフォンのジャイロセンサー入力を取得し、フライパン（台）の回転・傾きへと直感的に変換するスクリプトです。<br>
- **`Scripts/GamePlay/EggFallDetector`**<br>
  ゲームループ進行上における落下の判定を行い、ゲームオーバー（失敗）ステートへの移行を管理するスクリプトです。<br>
  https://github.com/KawaseRippei-HAC/Egg_Incubator/blob/main/Assets/Scripts/Gameplay/DifficultyManager.cs
- **`Scripts/GamePlay/DifficultyManager`**<br>
  時間経過に伴う力の強さや間隔など、タマゴや台のパラメータを管理・調整するスクリプトです。<br>
  https://github.com/KawaseRippei-HAC/Egg_Incubator/blob/main/Assets/Scripts/Gameplay/EggFallDetector.cs

## ✨アピールポイント・苦労した点

### アピールポイント
タマゴの挙動において、物理演算（Rigidbody）に完全に依存するのではなく、スクリプト制御（AddForce / AddTorque）を組み合わせることで、「予測不能すぎない範囲で暴れる」という制御された不規則さを実現しました。また、ジャイロ入力値に感度や最大傾斜角、補間を設けることで細かい手ブレを防ぎ、プレイヤーの操作が直感的にタマゴの反応に繋がる手触りの良い操作性を構築できた点が技術的なアピールポイントです。

さらに、タマゴの挙動や台のパラメータを細かく設定できるよう設計し、`DifficultyManager`を通じて時間経過による力の強さや間隔の変化を一元管理したことで、直感的かつ容易な難易度調整を実現しました。

### 苦労した点
- **ジャイロ機能の理解と実装**<br>
  スマートフォン向けゲームの開発が初めてであり、ジャイロ機能の実装に関する具体的なイメージを持てませんでした。そのため、事前の準備期間にUnityリファレンスなどの技術記事を調査し、技術検証を行ったことで、2日間の開発期間のうち初日で実装を完了できました。
- **タマゴの挙動調整と理不尽さの解消**<br>
  実装後のテストにおいて、タマゴの挙動が想定以上に激しくなり、プレイヤーの操作に関わらずタマゴが落下してしまう問題が発生しました。これに対し、タマゴが中心へ向かうよう微小な補正（`EggController`）を加えることで、一度の動きで盤外に落ちることを防ぎ、プレイヤーのジャイロ操作で十分に対応できるよう改善しました。

# 勤怠管理システム

ASP.NET Core MVC、Entity Framework Core、SQL Server、Bootstrap を使用して開発した、Web ベースの勤怠管理システムです。

社員向け機能と管理者向け機能を分け、出退勤、勤怠履歴、勤怠修正、有給休暇、月別集計、月次締め、操作ログなどを一元管理できます。

---

## 目次

- [システム概要](#システム概要)
- [主な機能](#主な機能)
- [勤務ルール](#勤務ルール)
- [セキュリティ機能](#セキュリティ機能)
- [使用技術](#使用技術)
- [システム構成](#システム構成)
- [主要データ](#主要データ)
- [プロジェクト構成](#プロジェクト構成)
- [動作環境](#動作環境)
- [セットアップ手順](#セットアップ手順)
- [データベース設定](#データベース設定)
- [初期アカウント](#初期アカウント)
- [設計書](#設計書)
- [スクリーンショット](#スクリーンショット)
- [デモ動画](#デモ動画)
- [今後の改善案](#今後の改善案)
- [作成者](#作成者)
- [利用上の注意](#利用上の注意)

---

## システム概要

本システムは、社員約150名規模の会社を想定した勤怠管理システムです。

社員は、自分の出退勤、勤怠履歴、勤怠修正申請、有給休暇申請、有給残日数などを確認できます。

管理者は、社員管理、全社員の勤怠確認、申請承認、月別集計、月次締め、CSV出力、操作ログ確認などを行えます。

---

## 主な機能

### 社員向け機能

- ログイン・ログアウト
- 社員トップ画面
- 出勤打刻
- 退勤打刻
- 現在時刻・勤務経過時間の表示
- 固定休憩時間の自動計算
- 実働時間の自動計算
- 遅刻時間の自動計算
- 残業時間の自動計算
- 年月別の勤怠履歴確認
- 未打刻・欠勤状態の確認
- 勤怠修正申請
- 勤怠修正申請履歴
- 有給休暇申請
- 有給休暇申請履歴
- 有給残日数確認
- 年5日取得アラート
- 初回ログイン時のパスワード変更
- 本人によるパスワード変更
- 打刻時のGPS・端末情報記録

### 管理者向け機能

- 管理者ログイン・ログアウト
- 管理者ダッシュボード
- 本日の出勤人数・遅刻人数・退勤済み人数表示
- 未承認申請件数表示
- 社員一覧
- 部署・キーワード検索
- 社員登録
- 社員編集
- 社員有効化・無効化
- ログインロック解除
- 仮パスワード再発行
- パスワード再発行後の変更必須化
- 全社員勤怠一覧
- 日付・部署・社員名検索
- 未打刻管理
- 欠勤確定・欠勤取消
- 勤怠修正申請の承認・却下
- 承認・却下コメント
- 勤怠修正履歴
- 有給申請の承認・却下
- 有給残日数管理
- 有給付与履歴
- 有給繰越
- 有給失効
- 有給予約管理
- 会社カレンダー管理
- 月別勤怠集計
- 月次締め
- 月次再開
- 月次締め履歴
- 月次締め前の未処理チェック
- CSV出力
- 操作ログ確認
- 打刻ログ確認
- GPS・位置情報・ブラウザ・端末・IP情報確認

---

## 勤務ルール

| 項目 | 内容 |
|---|---|
| 基本勤務時間 | 09:00～18:00 |
| 固定休憩時間 | 12:00～13:00 |
| 休憩時間 | 60分 |
| 1日の基本実働時間 | 480分 |
| 遅刻判定 | 09:00を過ぎて出勤した場合 |
| 残業判定 | 実働時間が480分を超えた場合 |
| 出勤重複 | 同一日に複数回の出勤は不可 |
| 退勤前提 | 出勤前の退勤は不可 |
| 退勤重複 | 同一日に複数回の退勤は不可 |
| 営業日 | 会社カレンダーで管理 |
| 月次締め後 | 勤怠変更処理を制限 |

### 計算方法

```text
実働時間
= 退勤時間
- 出勤時間
- 適用される休憩時間

遅刻時間
= 0 と（出勤時間 - 09:00）の大きい方

残業時間
= 0 と（実働時間 - 480分）の大きい方
```

---

## セキュリティ機能

- パスワードのハッシュ化保存
- セッションによるログイン管理
- 社員・管理者の権限制御
- 社員は自分の勤怠情報のみ閲覧可能
- 管理者画面は管理者のみ利用可能
- 無効化された社員はログイン不可
- ログイン5回失敗時のアカウントロック
- 一定時間経過後の自動ロック解除
- 管理者によるロック解除
- 初回ログイン時のパスワード変更必須
- 仮パスワード再発行後の変更必須
- 権限のないURLへの直接アクセス制限
- ログイン・管理操作の操作ログ記録

---

## 使用技術

| 分類 | 技術 |
|---|---|
| 開発言語 | C# |
| フレームワーク | ASP.NET Core MVC |
| 対象フレームワーク | .NET 10 |
| ORM | Entity Framework Core 10 |
| データベース | SQL Server / SQL Server Express LocalDB |
| フロントエンド | HTML5 / CSS3 / JavaScript |
| UI | Bootstrap 5 |
| 開発環境 | Visual Studio 2022 |
| 対応ブラウザ | Google Chrome / Microsoft Edge |

---

## システム構成

```text
利用者
  │
  ▼
Webブラウザ
  │
  ▼
ASP.NET Core MVC
  ├── Controllers
  ├── ViewModels
  ├── Views
  ├── Services
  └── Helpers
  │
  ▼
Entity Framework Core
  │
  ▼
SQL Server Database
```

---

## 主要データ

| データ | 内容 |
|---|---|
| Employees | 社員情報、権限、部署、パスワード、アカウント状態 |
| Departments | 部署マスタ |
| Attendances | 出勤、退勤、実働、遅刻、残業、欠勤 |
| AttendanceCorrectionRequests | 勤怠修正申請と承認結果 |
| AttendanceStampLogs | GPS、端末、ブラウザ、IP、打刻情報 |
| PaidLeaveRequests | 有給申請と承認結果 |
| PaidLeaveBalances | 付与、使用、予約、繰越、失効、残日数 |
| PaidLeaveGrantHistories | 有給付与履歴 |
| CompanyCalendarDays | 営業日、休日、会社カレンダー |
| MonthlyClosings | 月次締め、再開、履歴 |
| OperationLogs | ログイン・管理操作履歴 |

---

## プロジェクト構成

各フォルダはクリックして確認できます。

- [Controllers](./Controllers)
- [Data](./Data)
- [Database](./Database)
- [Demo](./Demo)
- [Documents](./Documents)
- [Helpers](./Helpers)
- [Migrations](./Migrations)
- [Models](./Models)
- [Properties](./Properties)
- [Screenshots](./Screenshots)
- [Services](./Services)
- [ViewModels](./ViewModels)
- [Views](./Views)
- [wwwroot](./wwwroot)

主要ファイル：

- [プロジェクトファイル](./AttendanceManagementSystem.csproj)
- [Solutionファイル](./AttendanceManagementSystem.slnx)
- [Program.cs](./Program.cs)
- [appsettings.json](./appsettings.json)
- [appsettings.Development.json](./appsettings.Development.json)
- [.gitignore](./.gitignore)

---

## 動作環境

以下を準備してください。

- Windows 10 または Windows 11
- [Visual Studio 2022](https://visualstudio.microsoft.com/ja/vs/)
- ASP.NET と Web 開発ワークロード
- [.NET 10 SDK](https://dotnet.microsoft.com/ja-jp/download)
- SQL Server Express LocalDB
- [SQL Server Management Studio](https://learn.microsoft.com/ja-jp/ssms/install/install)
- [Git](https://git-scm.com/downloads)
- Google Chrome または Microsoft Edge

---

## セットアップ手順

### 1. リポジトリをクローン

```bash
git clone https://github.com/IslomJpn/AttendanceManagementSystem.git
cd AttendanceManagementSystem
```

リポジトリ：

[AttendanceManagementSystem GitHub Repository](https://github.com/IslomJpn/AttendanceManagementSystem)

### 2. .NETツールとNuGetパッケージを復元

```bash
dotnet tool restore
dotnet restore
```

### 3. データベースを準備

次のいずれかを実行します。

- Entity Framework Core Migrationで新規データベースを作成
- 同梱されているデモ用データベースバックアップを復元

### 4. ビルド

```bash
dotnet build
```

### 5. 実行

```bash
dotnet run
```

または、[AttendanceManagementSystem.slnx](./AttendanceManagementSystem.slnx) をVisual Studio 2022で開き、`Ctrl + F5` で実行します。

---

## データベース設定

標準の接続文字列：

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AttendanceManagementSystemFinalDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### 方法A：EF Core Migrationで新規作成

```bash
dotnet tool restore
dotnet ef database update
dotnet run
```

### 方法B：データベースバックアップを復元

データベースフォルダ：

[Databaseフォルダを開く](./Database)

バックアップファイル：

```text
Database/AttendanceManagementSystemFinalDb.bak
```

復元手順：

1. SQL Server Management Studioを起動します。
2. 次のサーバーへ接続します。

```text
(localdb)\MSSQLLocalDB
```

3. `Databases` を右クリックします。
4. `Restore Database` を選択します。
5. `Device` を選択します。
6. `AttendanceManagementSystemFinalDb.bak` を指定します。
7. データベース名を次の名前にします。

```text
AttendanceManagementSystemFinalDb
```

8. 復元を実行します。
9. アプリケーションを起動します。

> Publicリポジトリには、実在する社員情報、顧客情報、個人情報、機密情報を保存しないでください。

---

## 初期アカウント

新規データベース作成時に登録されるデモ用アカウントです。

| 権限 | メールアドレス | 初期パスワード |
|---|---|---|
| 管理者 | `admin@example.com` | `admin2026` |
| 社員 | `employee@example.com` | `password` |

初回ログイン後、パスワード変更画面へ移動します。

> 上記アカウントはローカルデモ用です。実運用前に必ず変更してください。

---

## 設計書

[Documentsフォルダを開く](./Documents)

以下の設計書をPDF・DOCX形式で収録しています。

| 資料 | 内容 |
|---|---|
| 勤怠管理システム 要件定義書 | 機能要件、非機能要件、勤務ルール、完成条件 |
| 勤怠管理システム 基本設計書 | システム構成、画面、URL、Controller、DB基本設計 |
| 勤怠管理システム 詳細設計書 | 画面項目、Action、入力チェック、DB、処理フロー |

---

## スクリーンショット

[Screenshotsフォルダを開く](./Screenshots)

主な画面：

- ログイン画面
- 社員トップ画面
- 管理者ダッシュボード
- 勤怠履歴
- 社員管理
- 勤怠一覧
- 未打刻・欠勤管理
- 月別集計
- 勤怠修正申請
- 有給管理
- 会社カレンダー
- 月次締め
- 操作ログ
- 打刻ログ

---

## デモ動画

[Google Driveでデモ動画を開く](https://drive.google.com/drive/folders/1AX95a7W2CRKgWBPT2wjm1GvNjuexaWuB)

Google Driveの共有設定：

```text
リンクを知っている全員
→ 閲覧者
```

---

## 今後の改善案

- メール通知
- ダッシュボードグラフ
- スマートフォン表示の追加最適化
- PDF出力
- 顔認証ログイン
- クラウド環境へのデプロイ
- 自動バックアップ
- CI/CD
- 自動テスト

---

## 作成者

**Islombek Kamolov**

- [GitHubプロフィール](https://github.com/IslomJpn)
- [プロジェクトリポジトリ](https://github.com/IslomJpn/AttendanceManagementSystem)

---

## 利用上の注意

本システムは、学習およびポートフォリオ公開を目的として開発したものです。

実運用前に、次の対応が必要です。

- 初期パスワードの変更
- 接続文字列・機密情報の安全な管理
- デモデータの削除または匿名化
- 権限・セキュリティ設定の再確認
- HTTPS設定
- 本番サーバー設定
- データベースバックアップ設定
- 利用環境ごとのテスト

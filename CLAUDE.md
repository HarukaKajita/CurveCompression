# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code) へのガイダンスを提供します。

## プロジェクト概要

これは、時系列データ最適化のための曲線圧縮アルゴリズムを実装するUnity 6 (6000.0.42f1) プロジェクトです。Ramer-Douglas-Peucker (RDP)、B-スプライン近似、ベジェ曲線、ハイブリッドアプローチを含む複数の高度なアルゴリズムを使用して時系列データを圧縮するための包括的で標準化されたAPIを提供します。

## 共通開発コマンド

### Unity エディターコマンド
- **プロジェクトを開く**: Unity Hub を開き、CurveCompression プロジェクトを選択
- **デモ/テスト実行**: 
  - Unity エディターを開く
  - シーン内のGameObjectに `CurveCompressionDemo` スクリプトをアタッチ
  - インスペクターで圧縮パラメータを設定
  - Play モードに入り、圧縮テストと可視化を実行
  - Console ウィンドウとシーンビューで結果を確認
- **プロジェクトビルド**: File > Build Settings > Build (または Ctrl+Shift+B)

### Unity コマンドライン (必要に応じて)
```bash
# コマンドラインからUnityテストを実行 (Unityのパスを適宜調整)
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode

# コマンドラインからビルド
Unity -batchmode -quit -projectPath . -buildTarget StandaloneLinux64 -buildLinux64Player ./Build/CurveCompression
```

## アーキテクチャ概要

### コアアルゴリズム構造
プロジェクトは標準化されたAPIを通じて複数の圧縮アルゴリズムを実装します：

1. **RDPAlgorithm** - Ramer-Douglas-Peucker 線単純化
   - 垂直距離閾値に基づいて点を再帰的に除去
   - 重み付け重要度と複数の曲線タイプ（線形、B-スプライン、ベジェ）をサポート
   - 鋭い特徴と重要なポイントの保持に優秀

2. **BSplineAlgorithm** - B-スプライン曲線近似
   - データポイントから滑らかなB-スプラインセグメントを作成
   - 誤差許容値に基づく適応的セグメンテーション
   - 滑らかで連続的なデータに最適

3. **BezierAlgorithm** - ベジェ曲線近似
   - 計算されたタンジェントでベジェセグメントを作成
   - Unity AnimationCurve システムと互換性
   - 滑らかさと精度の良いバランス

4. **HybridCompressor** - 統一圧縮オーケストレーター
   - CompressionMethod に基づいて適切なアルゴリズムにルーティング
   - すべての圧縮タイプをサポート：RDP 派生、直接 B-スプライン、直接ベジェ
   - データタイプ固有の最適化重みを提供

### 標準化されたAPI設計
すべての圧縮アルゴリズムは `ICompressionAlgorithm` インターフェースパターンに従います：
```csharp
// 標準インターフェース
CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
CompressedCurveData Compress(TimeValuePair[] points, float tolerance)

// 高レベル使用法
CompressionResult CurveCompressor.Compress(TimeValuePair[] points, CompressionParams parameters)
```

### データ構造
- **TimeValuePair[]** - 入力時系列データ
- **CompressionParams** - メソッド、許容値、重要度重み、データタイプを含む設定
- **CompressedCurveData** - 出力曲線セグメント (CurveSegment[])
- **CompressionResult** - メトリクス（圧縮率、誤差、タイミング）を含む
- **CurveSegment** - 個別の曲線部分（線形、B-スプライン、またはベジェ）

### デモ/テストアーキテクチャ
- **CurveCompressionDemo** - 包括的なテスト機能を持つメインデモコンポーネント
- **CurveVisualizer** - 圧縮結果のリアルタイム可視化
- **ControlPointEstimator** - 自動最適制御点推定
- テストデータ生成：複雑な波形、正弦波、ノイズパターン
- パフォーマンスメトリクス：圧縮率、最大/平均誤差、計算時間
- Unity 統合のための AnimationClip エクスポート機能

### 主要データフロー
1. `TimeValuePair[]` として時系列データを生成または読み込み
2. `CompressionParams` で圧縮を設定（メソッド、許容値、重み）
3. メトリクス付き高レベル圧縮には `CurveCompressor.Compress()` を呼び出し
4. または直接アクセス用に個別アルゴリズムを呼び出し
5. 結果には `CompressedCurveData` とパフォーマンスメトリクスの両方が含まれる
6. `CurveVisualizer` で可視化するか、Unity AnimationClip としてエクスポート

## 開発ノート

### Unity固有の考慮事項
- Universal Render Pipeline は PC と Mobile 用の別々のレンダーアセットで設定
- アセンブリ定義は `CurveCompression` 名前空間に制限
- `UnityCompressionUtils` クラス経由で Unity 統合を提供
- リアルタイム曲線表示のための LineRenderer ベース可視化
- シームレスな Unity ワークフローのための AnimationClip エクスポート/インポートサポート

### コアユーティリティクラス
- **ValidationUtils** - 包括的な入力検証とエラーチェック
- **MathUtils** - ゼロ除算保護を含む安全な数学演算
- **InterpolationUtils** - 最適化された補間メソッド（線形、ベジェ、B-スプライン、Catmull-Rom）
- **UnityCompressionUtils** - Unity固有の変換（AnimationCurve ↔ 圧縮データ）
- **TangentCalculator** - ベジェ曲線用の滑らかなタンジェント計算

### 圧縮メソッド (CompressionMethod enum)
- **RDP_Linear** - 線形セグメントでのRDP
- **RDP_BSpline** - B-スプライン曲線評価でのRDP
- **RDP_Bezier** - ベジェ曲線評価でのRDP
- **BSpline_Direct** - 直接B-スプライン近似
- **Bezier_Direct** - 直接ベジェ近似

### アルゴリズムパラメータ
現在の標準化されたAPIは `CompressionParams` を使用：
```csharp
var parameters = new CompressionParams
{
    tolerance = 0.01f,                    // 圧縮許容値
    compressionMethod = CompressionMethod.Bezier_Direct,
    dataType = CompressionDataType.Animation,
    importanceThreshold = 1.0f,           // 重要度重み付け
    importanceWeights = ImportanceWeights.Default
};
```

### 制御点推定
7つのアルゴリズムによる高度な制御点推定：
- **Elbow Method** - 誤差曲線解析による最適ポイント数検出
- **Curvature Analysis** - 局所曲率変動に基づく
- **Information Entropy** - データ複雑性メトリクスを使用
- **Douglas-Peucker Adaptive** - RDP ベースのプログレッシブリファインメント
- **Total Variation** - 特徴を保持しながら変動を最小化
- **Error Bound** - 上限決定
- **Statistical Analysis** - データ分布ベースの推定

### 現在の開発状況
- **本番対応** - 完全なエラーハンドリングを備えた包括的API
- **広範囲テスト済み** - 複数のテストシナリオを持つデモフレームワーク
- **パフォーマンス最適化済み** - 安全な数学演算と検証
- **クリーンアーキテクチャ** - 標準化されたインターフェースと関心の分離
- **Unity統合済み** - 完全な AnimationCurve サポートと可視化ツール

## 使用例

### 基本圧縮
```csharp
using CurveCompression.Core;
using CurveCompression.DataStructures;

// 許容値での簡単な圧縮
var result = CurveCompressor.Compress(timeValueData, 0.01f);
Debug.Log($"{result.originalCount} から {result.compressedCount} ポイントに圧縮");
Debug.Log($"最大誤差: {result.maxError:F6}");
```

### パラメータを使った高度な圧縮
```csharp
var parameters = new CompressionParams
{
    tolerance = 0.005f,
    compressionMethod = CompressionMethod.RDP_Bezier,
    dataType = CompressionDataType.Animation,
    importanceThreshold = 1.5f
};

var result = CurveCompressor.Compress(animationData, parameters);
```

### Unity AnimationCurve 統合
```csharp
using CurveCompression.Core;

// AnimationCurve を圧縮データに変換
var compressedData = UnityCompressionUtils.FromAnimationCurve(animCurve, 0.01f);

// 既存の時系列データを圧縮して AnimationCurve に戻す
var result = CurveCompressor.Compress(timeValueData, 0.01f);
var newAnimCurve = UnityCompressionUtils.ToAnimationCurve(result.compressedCurve);
```

### アルゴリズム直接アクセス
```csharp
using CurveCompression.Algorithms;

// 特定のアルゴリズムを直接使用
var rdpResult = RDPAlgorithm.Compress(data, compressionParams);
var bezierResult = BezierAlgorithm.Compress(data, tolerance);
var bsplineResult = BSplineAlgorithm.Compress(data, tolerance);
```

### 自動制御点推定
```csharp
using CurveCompression.Algorithms;

// 最適制御点を推定
var estimates = ControlPointEstimator.EstimateAll(data, tolerance, 2, 50);
int optimalPoints = estimates["Elbow"].optimalPoints;

// 推定ポイントを圧縮に使用
var fixedResult = BezierAlgorithm.CompressWithFixedControlPoints(data, optimalPoints);
```

## 共通開発パターン

### エラーハンドリング
すべての圧縮メソッドは入力検証に `ValidationUtils` を使用：
- null でない、空でないデータを自動的に検証
- 許容値が正の値であることをチェック
- CompressionParams 設定を検証
- 無効な入力に対して説明的な ArgumentException を投げる

### パフォーマンス考慮事項
- 安全な数学演算には `MathUtils` を使用
- `InterpolationUtils` は最適化された補間メソッドを提供
- 大きなデータセットは曲線近似前のRDP前処理が有効
- 最適な重みのためにデータタイプ（`Animation`、`SensorData`、`FinancialData`）を考慮

### テストとデバッグ
- インタラクティブテストには `CurveCompressionDemo` を使用
- リアルタイム視覚フィードバックのために `CurveVisualizer` を有効化
- 品質評価のために圧縮メトリクス（比率、最大/平均誤差）を監視
- 詳細なUnity検査のために結果をAnimationClipとしてエクスポート

## Claude Code ワークフロー
### 言語ガイドライン
- **常に英語で思考する**
- プロンプトへの応答やユーザーに表示されるコメントやその他のテキストを書く際は、日本語に翻訳してから書く。
- ウェブから情報を収集したり思考したりする際は日本語に限定せず、常に英語をベースとして複数の言語で作業できるようにする。
- コミットメッセージは日本語で書く。

### 以下のステップで進める：
- コードベースを探索して現在の実装を理解する。
- プロジェクト要件に基づいて改善エリアや新機能を特定する。
- アーキテクチャと設計パターンを考慮して変更実装のプランを考える。
- 変更を実装する。
- 既存のコードスタイル、アーキテクチャ、ドキュメントとの一貫性を確保する。
- 日本語で説明的なメッセージと共に変更をgit addとcommitする。
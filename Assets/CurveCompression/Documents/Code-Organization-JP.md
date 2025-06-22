# コード構成と構造

## プロジェクト構造

CurveCompressionプロジェクトは、明確な関心の分離と責任を持つ論理的モジュールに整理されています。

```
Assets/CurveCompression/
├── Documents/                          # ドキュメントファイル
│   ├── 現在のアーキテクチャ.md            # アーキテクチャ概要
│   ├── コード構成.md                     # このファイル
│   ├── アルゴリズム詳細.md               # アルゴリズム実装
│   ├── 現在のデータ構造.md               # データ構造ドキュメント
│   └── 開発者セットアップ.md             # セットアップと拡張ガイド
├── Scripts/
│   ├── Core/                          # コア機能
│   │   ├── CurveCompressor.cs         # メインAPIファサード
│   │   ├── HybridCompressor.cs        # アルゴリズムルーティング
│   │   ├── AlgorithmSelector.cs       # インテリジェントアルゴリズム選択
│   │   ├── AdaptiveTolerance.cs       # 適応的許容誤差計算
│   │   ├── ValidationUtils.cs         # 入力検証ユーティリティ
│   │   ├── MathUtils.cs              # 数学ユーティリティ
│   │   └── InterpolationUtils.cs      # 補間アルゴリズム
│   ├── DataStructures/               # データモデルとタイプ
│   │   ├── TimeValuePair.cs          # 基本データポイント
│   │   ├── CompressionParams.cs      # 設定パラメータ
│   │   ├── CompressionResult.cs      # 結果とメトリクス
│   │   ├── CompressedCurveData.cs    # 圧縮曲線表現
│   │   ├── CurveSegment.cs           # 個別曲線セグメント
│   │   └── ImportanceWeights.cs      # アルゴリズム重み付け
│   ├── Algorithms/                   # アルゴリズム実装
│   │   ├── RDPAlgorithm.cs          # Ramer-Douglas-Peucker
│   │   ├── BSplineAlgorithm.cs      # B-スプライン近似
│   │   ├── BezierAlgorithm.cs       # ベジェ曲線フィッティング
│   │   └── ControlPointEstimator.cs  # 最適ポイント推定
│   ├── Visualization/               # 可視化コンポーネント
│   │   ├── CurveVisualizer.cs       # 曲線レンダリング
│   │   └── CurveCompressionDemo.cs  # デモとテスト
│   └── Unity/                       # Unity固有統合
│       └── UnityCompressionUtils.cs # Unity統合ユーティリティ
└── CurveCompression.asmdef          # アセンブリ定義
```

## 名前空間構成

### コア名前空間

#### `CurveCompression.Core`
**目的**: 主要APIとコア機能
**主要クラス**:
- `CurveCompressor`: すべての圧縮操作のメインエントリーポイント
- `HybridCompressor`: アルゴリズム選択とルーティングロジック
- `AlgorithmSelector`: インテリジェントアルゴリズム推奨システム
- `AdaptiveTolerance`: データ駆動許容誤差計算
- `ValidationUtils`: 包括的入力検証
- `MathUtils`: 安全な数学演算
- `InterpolationUtils`: 最適化された補間アルゴリズム

#### `CurveCompression.DataStructures`
**目的**: データモデル、設定、結果
**主要クラス**:
- `TimeValuePair`: 基本的な時間-値データポイント
- `CompressionParams`: 圧縮設定と設定
- `CompressionResult`: メトリクス付き圧縮結果
- `CompressedCurveData`: 圧縮曲線表現
- `CurveSegment`: 個別曲線セグメント実装
- `ImportanceWeights`: アルゴリズム重要度重み付け

#### `CurveCompression.Algorithms`
**目的**: アルゴリズム実装と推定
**主要クラス**:
- `RDPAlgorithm`: Ramer-Douglas-Peucker線単純化
- `BSplineAlgorithm`: B-スプライン曲線近似
- `BezierAlgorithm`: ベジェ曲線フィッティング
- `ControlPointEstimator`: 最適制御点推定

#### `CurveCompression.Visualization`
**目的**: ビジュアルデバッグとデモンストレーション
**主要クラス**:
- `CurveVisualizer`: リアルタイム曲線可視化
- `CurveCompressionDemo`: インタラクティブデモンストレーション

## クラス責任

### コアクラス

#### CurveCompressor
```csharp
namespace CurveCompression.Core
{
    public static class CurveCompressor
    {
        // 標準圧縮インターフェース
        public static CompressionResult Compress(TimeValuePair[], CompressionParams)
        public static CompressionResult Compress(TimeValuePair[], float tolerance)
        
        // インテリジェント圧縮メソッド
        public static CompressionResult CompressWithAutoSelection(TimeValuePair[], float)
        public static CompressionResult CompressWithQualityLevel(TimeValuePair[], QualityLevel)
        public static CompressionResult CompressWithTargetRatio(TimeValuePair[], float)
        
        // 分析と推奨
        public static AlgorithmRecommendation GetAlgorithmRecommendation(TimeValuePair[], CompressionParams)
        public static DataAnalysis AnalyzeData(TimeValuePair[])
        public static AdaptiveToleranceResult GetAdaptiveTolerance(TimeValuePair[], QualityLevel)
    }
}
```

**責任**:
- すべての圧縮操作の統一APIファサード
- 入力検証とエラーハンドリング
- インテリジェントシステムの統合
- パフォーマンス最適化調整

#### AlgorithmSelector
```csharp
namespace CurveCompression.Core
{
    public static class AlgorithmSelector
    {
        // データ分析
        public static DataAnalysis AnalyzeDataCharacteristics(TimeValuePair[])
        
        // アルゴリズム推奨
        public static AlgorithmRecommendation SelectBestAlgorithm(TimeValuePair[], CompressionParams)
        
        // サポート構造
        public struct DataAnalysis { /* 滑らかさ、複雑度、ノイズなど */ }
        public struct AlgorithmRecommendation { /* メソッド、信頼度、推論 */ }
    }
}
```

**責任**:
- データ特性分析（滑らかさ、複雑度、ノイズ）
- アルゴリズムパフォーマンススコアリング
- インテリジェントアルゴリズム推奨
- 詳細推論生成

#### AdaptiveTolerance
```csharp
namespace CurveCompression.Core
{
    public static class AdaptiveTolerance
    {
        // 許容誤差計算
        public static AdaptiveToleranceResult CalculateAdaptiveTolerance(TimeValuePair[], QualityLevel, float?)
        public static float CalculateToleranceForCompressionRatio(TimeValuePair[], float, CompressionMethod)
        
        // 品質ベース圧縮
        public static CompressionResult CompressWithQualityLevel(TimeValuePair[], QualityLevel, CompressionMethod?)
        
        // サポート列挙型と構造
        public enum QualityLevel { Low, Medium, High, Lossless }
        public struct AdaptiveToleranceResult { /* 許容誤差、推論、メトリクス */ }
    }
}
```

**責任**:
- データ駆動許容誤差計算
- 品質レベル抽象化
- 圧縮率ターゲティング
- パフォーマンス対品質最適化

### ユーティリティクラス

#### ValidationUtils
```csharp
namespace CurveCompression.Core
{
    public static class ValidationUtils
    {
        // ポイント検証
        public static void ValidatePoints(TimeValuePair[], string, int minRequired = 2)
        
        // パラメータ検証
        public static void ValidateTolerance(float, string)
        public static void ValidateRange(float, float, float, string)
        public static void ValidateCompressionParams(CompressionParams)
        public static void ValidateControlPointCount(int, int, string)
    }
}
```

**責任**:
- 包括的入力検証
- 明確なエラーメッセージング
- 一貫した検証パターン
- パラメータ境界チェック

#### MathUtils
```csharp
namespace CurveCompression.Core
{
    public static class MathUtils
    {
        // 安全な数学演算
        public static float SafeDivide(float, float, float defaultValue = 0f)
        public static float SafeSlope(float x1, float y1, float x2, float y2)
        public static float SafeLerpParameter(float value, float start, float end)
        
        // 幾何学計算
        public static float DistanceSquared(Vector2, Vector2)
        public static float PerpendicularDistance(Vector2, Vector2, Vector2)
    }
}
```

**責任**:
- ゼロ除算保護
- 浮動小数点安全性
- 幾何学計算
- 数値安定性

#### InterpolationUtils
```csharp
namespace CurveCompression.Core
{
    public static class InterpolationUtils
    {
        // 線形補間
        public static float LinearInterpolate(TimeValuePair[], float time)
        
        // ベジェ曲線
        public static Vector2 QuadraticBezier(Vector2, Vector2, Vector2, float)
        public static Vector2 CubicBezier(Vector2, Vector2, Vector2, Vector2, float)
        
        // 高度補間
        public static float HermiteInterpolate(float, float, float, float, float)
        public static float MonotonicCubicInterpolate(TimeValuePair[], float)
    }
}
```

**責任**:
- 高性能補間
- 複数補間メソッド
- 二分探索最適化
- 単調補間保持

## アルゴリズム実装構造

### 共通アルゴリズムインターフェース

すべての圧縮アルゴリズムは標準化されたインターフェースパターンに従います：

```csharp
public static class [Algorithm]Algorithm
{
    // 標準インターフェース
    public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
    public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
    
    // アルゴリズム固有メソッド
    public static CompressedCurveData CompressWithSpecificFeature(...)
    
    // レガシー互換性（必要に応じて）
    public static TimeValuePair[] LegacyMethod(...)
}
```

### アルゴリズム実装

#### RDPAlgorithm
```csharp
namespace CurveCompression.Algorithms
{
    public static class RDPAlgorithm
    {
        // 標準インターフェース
        public static CompressedCurveData Compress(TimeValuePair[], CompressionParams)
        public static CompressedCurveData Compress(TimeValuePair[], float)
        
        // 高度機能
        public static CompressedCurveData CompressWithCurveEvaluation(
            TimeValuePair[], float, CurveType, float, ImportanceWeights)
        
        // 内部メソッド
        private static List<int> SimplifyIndices(TimeValuePair[], float, int, int, ImportanceWeights)
        private static float PerpendicularDistance(TimeValuePair[], int, int, int)
        private static float CalculateImportance(TimeValuePair[], int, ImportanceWeights)
    }
}
```

#### BSplineAlgorithm
```csharp
namespace CurveCompression.Algorithms
{
    public static class BSplineAlgorithm
    {
        // 標準インターフェース
        public static CompressedCurveData Compress(TimeValuePair[], CompressionParams)
        public static CompressedCurveData Compress(TimeValuePair[], float)
        
        // 固定制御点
        public static CompressedCurveData CompressWithFixedControlPoints(TimeValuePair[], int)
        
        // レガシー互換性
        public static TimeValuePair[] ApproximateWithFixedPoints(TimeValuePair[], int)
        
        // 内部メソッド
        private static Vector2[] SelectControlPoints(TimeValuePair[], int)
        private static Vector2[] OptimizeControlPoints(TimeValuePair[], Vector2[])
        private static float EvaluateBSpline(Vector2[], float)
    }
}
```

#### ControlPointEstimator
```csharp
namespace CurveCompression.Algorithms
{
    public static class ControlPointEstimator
    {
        // メイン推定インターフェース
        public static Dictionary<string, EstimationResult> EstimateAll(TimeValuePair[], float, int, int)
        
        // 個別推定メソッド
        public static EstimationResult EstimateByElbowMethod(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByCurvature(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByInformationEntropy(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByDouglasPeuckerAdaptive(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByTotalVariation(TimeValuePair[], float, int, int)
        public static EstimationResult DetermineByErrorBound(TimeValuePair[], float)
        public static EstimationResult DetermineByStatistical(TimeValuePair[], float)
        
        // サポートクラス
        public class EstimationResult { /* 最適ポイント、スコア、メソッド、メトリクス */ }
    }
}
```

## データフローアーキテクチャ

### 圧縮プロセスフロー

```
1. 入力検証
   ├── ValidationUtils.ValidatePoints()
   ├── ValidationUtils.ValidateTolerance()
   └── ValidationUtils.ValidateCompressionParams()

2. インテリジェント分析（自動選択が有効な場合）
   ├── AlgorithmSelector.AnalyzeDataCharacteristics()
   ├── AlgorithmSelector.SelectBestAlgorithm()
   └── AdaptiveTolerance.CalculateAdaptiveTolerance()

3. アルゴリズムルーティング
   ├── HybridCompressor.Compress()
   └── [特定アルゴリズム].Compress()

4. 結果アセンブリ
   ├── CompressionResult構築
   ├── エラーメトリクス計算
   └── パフォーマンスメトリクス収集
```

### エラーハンドリングフロー

```
1. 入力検証エラー
   ├── ArgumentNullException（null入力）
   ├── ArgumentException（無効データ）
   └── ArgumentOutOfRangeException（無効範囲）

2. アルゴリズム実行エラー
   ├── より簡単なアルゴリズムへの優雅な劣化
   ├── フォールバック許容誤差調整
   └── 緊急線形近似

3. 結果検証
   ├── 出力品質検証
   ├── メトリクス一貫性チェック
   └── パフォーマンス境界検証
```

## 拡張ポイント

### 新しいアルゴリズムの追加

1. **アルゴリズムクラスの作成**:
   ```csharp
   namespace CurveCompression.Algorithms
   {
       public static class NewAlgorithm
       {
           public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
           public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
       }
   }
   ```

2. **列挙型の更新**:
   ```csharp
   public enum CompressionMethod
   {
       // ... 既存メソッド
       NewAlgorithm_Variant
   }
   ```

3. **HybridCompressorの更新**:
   ```csharp
   CompressionMethod.NewAlgorithm_Variant => NewAlgorithm.Compress(points, parameters)
   ```

4. **AlgorithmSelectorの更新**:
   ```csharp
   scores[CompressionMethod.NewAlgorithm_Variant] = CalculateNewAlgorithmScore(analysis);
   ```

### 新しい品質メトリクスの追加

1. **CompressionResultの拡張**:
   ```csharp
   public class CompressionResult
   {
       // ... 既存プロパティ
       public float newMetric;
   }
   ```

2. **計算の実装**:
   ```csharp
   private void CalculateNewMetric(TimeValuePair[] original, CompressedCurveData compressed)
   {
       newMetric = /* 計算ロジック */;
   }
   ```

### 新しいデータ分析の追加

1. **DataAnalysis構造の拡張**:
   ```csharp
   public struct DataAnalysis
   {
       // ... 既存フィールド
       public float newCharacteristic;
   }
   ```

2. **分析ロジックの実装**:
   ```csharp
   private static float CalculateNewCharacteristic(TimeValuePair[] data)
   {
       return /* 分析ロジック */;
   }
   ```

この構成は、明確な関心の分離、保守可能なコード構造、将来の拡張への優れた拡張性を提供します。
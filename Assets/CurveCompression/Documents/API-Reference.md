# API リファレンス

## 概要

CurveCompressionライブラリの完全なAPI仕様書です。すべての公開インターフェース、メソッド、プロパティについて詳細に説明します。

## 名前空間

```csharp
using CurveCompression.Core;          // メインAPI
using CurveCompression.DataStructures; // データ型
using CurveCompression.Algorithms;     // アルゴリズム直接アクセス
using CurveCompression.Visualization;  // 可視化機能
```

## 高レベルAPI

### CurveCompressor (静的クラス)
**説明**: メインのAPIエントリーポイント

#### メソッド

##### Compress (パラメータ版)
```csharp
public static CompressionResult Compress(
    TimeValuePair[] originalData, 
    CompressionParams parameters)
```
**説明**: パラメータ設定による包括的な圧縮
**パラメータ**:
- `originalData`: 圧縮対象の時系列データ
- `parameters`: 圧縮設定

**戻り値**: `CompressionResult` - 圧縮結果とメトリクス

**例外**:
- `ArgumentNullException`: データまたはパラメータがnull
- `ArgumentException`: データが空または無効

**使用例**:
```csharp
var params = new CompressionParams
{
    tolerance = 0.01f,
    compressionMethod = CompressionMethod.Bezier_Direct
};
var result = CurveCompressor.Compress(timeValueData, params);
```

##### Compress (許容値版)
```csharp
public static CompressionResult Compress(
    TimeValuePair[] originalData, 
    float tolerance)
```
**説明**: 許容値のみを指定するシンプルな圧縮
**パラメータ**:
- `originalData`: 圧縮対象の時系列データ  
- `tolerance`: 圧縮許容誤差（正の値）

**戻り値**: `CompressionResult`

**内部動作**: デフォルト設定のCompressionParamsを作成して委譲

### HybridCompressor (静的クラス)
**説明**: アルゴリズム選択とルーティング

#### メソッド

##### Compress (パラメータ版)
```csharp
public static CompressedCurveData Compress(
    TimeValuePair[] points, 
    CompressionParams parameters)
```
**説明**: 指定された圧縮手法で圧縮を実行
**戻り値**: `CompressedCurveData` - 圧縮された曲線データ

##### Compress (許容値版)  
```csharp
public static CompressedCurveData Compress(
    TimeValuePair[] points, 
    float tolerance)
```

##### GetOptimalWeights
```csharp
public static ImportanceWeights GetOptimalWeights(
    CompressionDataType dataType, 
    ImportanceWeights userWeights)
```
**説明**: データタイプに基づく最適な重要度重みを取得
**パラメータ**:
- `dataType`: データの種別
- `userWeights`: ユーザー指定重み（nullの場合はデフォルト使用）

**戻り値**: `ImportanceWeights` - 最適化された重み

## アルゴリズム別API

### RDPAlgorithm (静的クラス)
**説明**: Ramer-Douglas-Peucker線単純化アルゴリズム

#### メソッド

##### Compress (標準インターフェース)
```csharp
public static CompressedCurveData Compress(
    TimeValuePair[] points, 
    CompressionParams parameters)
```

##### Compress (シンプルインターフェース)
```csharp
public static CompressedCurveData Compress(
    TimeValuePair[] points, 
    float tolerance)
```

##### CompressWithCurveEvaluation
```csharp
public static CompressedCurveData CompressWithCurveEvaluation(
    TimeValuePair[] points, 
    float tolerance, 
    CurveType curveType, 
    float importanceThreshold = 1.0f, 
    ImportanceWeights weights = null)
```
**説明**: 曲線タイプ指定での高度な圧縮
**パラメータ**:
- `curveType`: 出力曲線タイプ (Linear/BSpline/Bezier)
- `importanceThreshold`: 重要度計算の閾値
- `weights`: カスタム重要度重み

### BSplineAlgorithm (静的クラス)
**説明**: B-スプライン曲線近似アルゴリズム

#### メソッド

##### Compress (標準インターフェース)
```csharp
public static CompressedCurveData Compress(
    TimeValuePair[] points, 
    CompressionParams parameters)
```

##### Compress (シンプルインターフェース)
```csharp
public static CompressedCurveData Compress(
    TimeValuePair[] points, 
    float tolerance)
```

##### CompressWithFixedControlPoints
```csharp
public static CompressedCurveData CompressWithFixedControlPoints(
    TimeValuePair[] points, 
    int numControlPoints)
```
**説明**: 制御点数を固定してのB-スプライン近似
**パラメータ**:
- `numControlPoints`: 使用する制御点数（2以上）

**戻り値**: `CompressedCurveData` - B-スプラインセグメント

##### ApproximateWithFixedPoints (レガシー互換)
```csharp
public static TimeValuePair[] ApproximateWithFixedPoints(
    TimeValuePair[] points, 
    int numControlPoints)
```
**説明**: レガシー形式での固定点数近似
**戻り値**: `TimeValuePair[]` - 近似結果の点列

### BezierAlgorithm (静的クラス)
**説明**: ベジェ曲線近似アルゴリズム

#### メソッド構成
BSplineAlgorithmと同様のインターフェースを提供:
- `Compress(points, parameters)`
- `Compress(points, tolerance)`  
- `CompressWithFixedControlPoints(points, numControlPoints)`
- `ApproximateWithFixedPoints(points, numControlPoints)`

#### 特徴
- Unity AnimationCurve互換
- 自動タンジェント計算
- 滑らかな補間

### ControlPointEstimator (静的クラス)
**説明**: 最適制御点数の自動推定

#### メソッド

##### EstimateAll
```csharp
public static Dictionary<string, EstimationResult> EstimateAll(
    TimeValuePair[] data, 
    float tolerance, 
    int minPoints = 2, 
    int maxPoints = 50)
```
**説明**: 全推定アルゴリズムを実行
**パラメータ**:
- `data`: 分析対象データ
- `tolerance`: 圧縮許容誤差
- `minPoints`: 最小制御点数
- `maxPoints`: 最大制御点数

**戻り値**: `Dictionary<string, EstimationResult>` - 推定結果マップ

#### 推定アルゴリズム
- **"Elbow"**: エルボー法による最適点検出
- **"Curvature"**: 曲率解析ベース
- **"Entropy"**: 情報エントロピーベース  
- **"DouglasPeucker"**: RDP適応的手法
- **"TotalVariation"**: 全変動最小化
- **"ErrorBound"**: 誤差境界決定
- **"Statistical"**: 統計解析ベース

#### EstimationResult クラス
```csharp
public class EstimationResult
{
    public int optimalPoints;                    // 最適制御点数
    public float score;                          // 評価スコア
    public string method;                        // 推定手法名
    public Dictionary<string, float> metrics;    // 詳細メトリクス
}
```

## ユーティリティAPI

### ValidationUtils (静的クラス)
**説明**: 入力検証とエラーチェック

#### 主要メソッド

##### ValidatePoints
```csharp
public static void ValidatePoints(
    TimeValuePair[] points, 
    string paramName = "points", 
    int minRequired = 2)
```

##### ValidateTolerance
```csharp
public static void ValidateTolerance(
    float tolerance, 
    string paramName = "tolerance")
```

##### ValidateCompressionParams
```csharp
public static void ValidateCompressionParams(
    CompressionParams parameters)
```

##### ValidateRange
```csharp
public static void ValidateRange(
    float value, 
    float min, 
    float max, 
    string paramName = "value")
```

### MathUtils (静的クラス)
**説明**: 安全な数学演算

#### 主要メソッド

##### SafeDivide
```csharp
public static float SafeDivide(
    float numerator, 
    float denominator, 
    float defaultValue = 0f)
```
**説明**: ゼロ除算安全な除算

##### SafeSlope
```csharp
public static float SafeSlope(
    float x1, float y1, 
    float x2, float y2)
```
**説明**: 安全な傾き計算

##### SafeLerpParameter
```csharp
public static float SafeLerpParameter(
    float value, 
    float start, 
    float end)
```
**説明**: 補間パラメータの安全計算

### InterpolationUtils (静的クラス)
**説明**: 最適化された補間アルゴリズム

#### 主要メソッド

##### LinearInterpolate
```csharp
public static float LinearInterpolate(
    TimeValuePair[] data, 
    float time)
```
**説明**: 高性能線形補間（バイナリサーチ使用）

##### QuadraticBezier
```csharp
public static Vector2 QuadraticBezier(
    Vector2 p0, Vector2 p1, Vector2 p2, 
    float t)
```

##### CubicBezier
```csharp
public static Vector2 CubicBezier(
    Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, 
    float t)
```

##### HermiteInterpolate
```csharp
public static float HermiteInterpolate(
    float startValue, float endValue, 
    float startTangent, float endTangent, 
    float t)
```

##### MonotonicCubicInterpolate
```csharp
public static float MonotonicCubicInterpolate(
    TimeValuePair[] points, 
    float time)
```
**説明**: 単調性を保つ3次補間

## Unity統合API

### UnityCompressionUtils (静的クラス)
**説明**: Unity固有機能との統合

#### AnimationCurve変換

##### FromAnimationCurve (データ変換)
```csharp
public static TimeValuePair[] FromAnimationCurve(
    AnimationCurve curve, 
    int sampleCount)
```

##### FromAnimationCurve (圧縮)
```csharp
public static CompressedCurveData FromAnimationCurve(
    AnimationCurve curve, 
    float tolerance = 0.01f)
```

##### ToAnimationCurve (基本)
```csharp
public static AnimationCurve ToAnimationCurve(
    TimeValuePair[] data)
```

##### ToAnimationCurve (圧縮データ)
```csharp
public static AnimationCurve ToAnimationCurve(
    CompressedCurveData compressedData, 
    int sampleCount = 100)
```

#### 統合処理

##### CompressAnimationCurve
```csharp
public static CompressionResult CompressAnimationCurve(
    AnimationCurve curve, 
    CompressionParams parameters, 
    int sampleCount = 1000)
```
**説明**: AnimationCurveの直接圧縮

## インターフェース

### ICompressionAlgorithm
```csharp
public interface ICompressionAlgorithm
{
    CompressedCurveData Compress(
        TimeValuePair[] points, 
        CompressionParams parameters);
    
    string AlgorithmName { get; }
    CompressionMethod SupportedMethod { get; }
}
```

## エラーハンドリング

### 例外の種類

#### ArgumentNullException
- パラメータがnullの場合
- 必須配列がnullの場合

#### ArgumentException  
- 配列が空の場合
- 最小要件を満たさない場合
- 時間順序が不正な場合

#### ArgumentOutOfRangeException
- 許容値が負または零の場合
- インデックスが範囲外の場合
- 数値が有効範囲外の場合

### エラーメッセージ例
```csharp
// ValidationUtils.ValidatePoints
throw new ArgumentNullException("points", "Point array cannot be null");
throw new ArgumentException("At least 2 points are required, but got 1", "points");

// ValidationUtils.ValidateTolerance  
throw new ArgumentOutOfRangeException("tolerance", "Tolerance must be positive, but got -0.1");
```

## パフォーマンス考慮事項

### 計算量
- **RDP**: O(n log n) ～ O(n²)
- **B-スプライン**: O(n)
- **ベジェ**: O(n)
- **制御点推定**: O(n log n)

### メモリ使用量
- **入力データ**: 8n bytes (TimeValuePair × n)
- **圧縮結果**: 40s bytes (CurveSegment × s)
- **作業領域**: アルゴリズム依存

### 最適化のヒント
1. **大きなデータセット**: RDP前処理を検討
2. **リアルタイム**: 許容値を調整してトレードオフ
3. **メモリ制約**: 固定制御点数手法を使用

この API リファレンスにより、CurveCompressionライブラリの全機能を効率的に活用できます。
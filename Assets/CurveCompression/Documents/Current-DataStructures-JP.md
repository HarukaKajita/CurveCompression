# 現在のデータ構造

## 概要

このドキュメントでは、包括的なリファクタリング後のCurveCompressionライブラリの現在のデータ構造について説明します。これらの構造は、型安全性、パフォーマンス最適化、明確なデータフローパターンを提供します。

## コアデータタイプ

### TimeValuePair
**場所**: `CurveCompression.DataStructures.TimeValuePair`
**タイプ**: `struct`（値型）

```csharp
[Serializable]
public struct TimeValuePair : IComparable<TimeValuePair>
{
    public float time;
    public float value;
    
    public TimeValuePair(float time, float value);
    public int CompareTo(TimeValuePair other);
    public override string ToString();
    public override bool Equals(object obj);
    public override int GetHashCode();
}
```

**設計特性**:
- **値型**: メモリ割り当てとGCプレッシャーを最小化
- **不変**: 作成後のデータ変更不可（データ整合性）
- **比較可能**: データ処理のための時間ソートサポート
- **シリアライズ可能**: Unity Inspectorとアセットシリアライゼーションサポート
- **メモリ効率**: インスタンスあたり8バイト（float × 2）

**使用パターン**:
```csharp
// 作成
var point = new TimeValuePair(1.5f, 0.8f);

// 配列操作
var dataArray = new TimeValuePair[1000];
Array.Sort(dataArray); // 自動時間ベースソート

// 比較
bool isEarlier = point1.CompareTo(point2) < 0;
```

### CompressionParams
**場所**: `CurveCompression.DataStructures.CompressionParams`
**タイプ**: `class`（参照型）

```csharp
[Serializable]
public class CompressionParams
{
    // 検証済みプロパティ
    public float tolerance { get; set; }                    // 0.0001f to 1.0f
    public float importanceThreshold { get; set; }          // 0.1f to 10.0f
    
    // 直接フィールド
    public CompressionMethod compressionMethod;             // アルゴリズム選択
    public CompressionDataType dataType;                   // データ分類
    public ImportanceWeights importanceWeights;            // アルゴリズム重み付け
    
    // メソッド
    public CompressionParams();
    public CompressionParams Clone();
    public bool Equals(CompressionParams other);
    public override int GetHashCode();
}
```

**検証システム**:
```csharp
public float tolerance
{
    get => _tolerance;
    set
    {
        ValidationUtils.ValidateTolerance(value, nameof(tolerance));
        _tolerance = value;
    }
}

public float importanceThreshold
{
    get => _importanceThreshold;
    set
    {
        ValidationUtils.ValidateRange(value, 0.1f, 10.0f, nameof(importanceThreshold));
        _importanceThreshold = value;
    }
}
```

**設計特徴**:
- **プロパティ検証**: 割り当て時の自動検証
- **シリアライゼーション**: Unity Inspector編集サポート
- **クローン可能**: パラメータバリエーション用ディープコピー機能
- **等価性**: キャッシュと最適化用比較サポート

### CompressionResult
**場所**: `CurveCompression.DataStructures.CompressionResult`
**タイプ**: `class`（参照型）

```csharp
public class CompressionResult
{
    // コア結果
    public TimeValuePair[] compressedData;      // レガシー形式互換性
    public CompressedCurveData compressedCurve; // モダン曲線表現
    
    // 品質メトリクス
    public float compressionRatio;              // 圧縮効果（0-1）
    public float maxError;                      // 最大偏差
    public float avgError;                      // 平均偏差
    public float rmseError;                     // 二乗平均平方根誤差
    
    // カウントメトリクス
    public int originalCount;                   // 元データポイント数
    public int compressedCount;                 // 圧縮セグメント/ポイント数
    
    // パフォーマンスメトリクス
    public TimeSpan compressionTime;            // 処理時間
    public long memoryUsed;                     // メモリ消費量
    
    // コンストラクタ
    public CompressionResult(TimeValuePair[] original, TimeValuePair[] compressed);
    public CompressionResult(TimeValuePair[] original, CompressedCurveData compressed);
    
    // ユーティリティメソッド
    public float InterpolateValue(float time);
    public bool IsWithinTolerance(float tolerance);
}
```

**メトリクス計算**:
```csharp
private void CalculateErrorsWithCurve(TimeValuePair[] original, CompressedCurveData compressed)
{
    float totalError = 0f;
    float totalSquaredError = 0f;
    maxError = 0f;
    
    for (int i = 0; i < original.Length; i++)
    {
        float curveValue = compressed.Evaluate(original[i].time);
        float error = Mathf.Abs(original[i].value - curveValue);
        
        totalError += error;
        totalSquaredError += error * error;
        maxError = Mathf.Max(maxError, error);
    }
    
    avgError = totalError / original.Length;
    rmseError = Mathf.Sqrt(totalSquaredError / original.Length);
}
```

### CompressedCurveData
**場所**: `CurveCompression.DataStructures.CompressedCurveData`
**タイプ**: `class`（参照型）

```csharp
[Serializable]
public class CompressedCurveData
{
    public CurveSegment[] segments;
    
    // コンストラクタ
    public CompressedCurveData(CurveSegment[] segments);
    
    // 評価メソッド
    public float Evaluate(float time);
    public Vector2 EvaluateAsVector2(float time);
    
    // 変換メソッド
    public TimeValuePair[] ToTimeValuePairs(int sampleCount);
    public Vector2[] ToVector2Array(int sampleCount);
    public AnimationCurve ToAnimationCurve();
    
    // 分析メソッド
    public float GetMinTime();
    public float GetMaxTime();
    public float GetValueRange();
    public int GetSegmentCount();
}
```

**評価ロジック**:
```csharp
public float Evaluate(float time)
{
    // 適切なセグメントを検索
    foreach (var segment in segments)
    {
        if (time >= segment.startTime && time <= segment.endTime)
        {
            return segment.Evaluate(time);
        }
    }
    
    // 外挿処理
    if (time < segments[0].startTime)
        return segments[0].startValue;
    else
        return segments[segments.Length - 1].endValue;
}
```

### CurveSegment
**場所**: `CurveCompression.DataStructures.CurveSegment`
**タイプ**: `struct`（値型）

```csharp
[Serializable]
public struct CurveSegment
{
    // 共通プロパティ
    public CurveType curveType;
    public float startTime, startValue;
    public float endTime, endValue;
    
    // ベジェ固有
    public float inTangent, outTangent;
    
    // B-スプライン固有
    public Vector2[] bsplineControlPoints;
    
    // ファクトリーメソッド
    public static CurveSegment CreateLinear(float startTime, float startValue, 
                                           float endTime, float endValue);
    public static CurveSegment CreateBezier(float startTime, float startValue, 
                                           float endTime, float endValue,
                                           float inTangent, float outTangent);
    public static CurveSegment CreateBSpline(Vector2[] controlPoints);
    
    // 評価
    public float Evaluate(float time);
    
    // ユーティリティ
    public bool ContainsTime(float time);
    public float GetDuration();
    public Vector2 GetStartPoint();
    public Vector2 GetEndPoint();
}
```

**曲線タイプ評価**:
```csharp
public float Evaluate(float time)
{
    float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
    
    return curveType switch
    {
        CurveType.Linear => EvaluateLinear(t),
        CurveType.Bezier => EvaluateBezier(t),
        CurveType.BSpline => EvaluateBSpline(t),
        _ => Mathf.Lerp(startValue, endValue, t)
    };
}

private float EvaluateLinear(float t)
{
    return Mathf.Lerp(startValue, endValue, t);
}

private float EvaluateBezier(float t)
{
    float dt = endTime - startTime;
    return InterpolationUtils.HermiteInterpolate(
        startValue, endValue,
        inTangent * dt, outTangent * dt, t);
}

private float EvaluateBSpline(float t)
{
    if (bsplineControlPoints == null || bsplineControlPoints.Length < 2)
        return Mathf.Lerp(startValue, endValue, t);
    
    return EvaluateBSplineInternal(bsplineControlPoints, t);
}
```

## 設定と列挙タイプ

### CompressionMethod
```csharp
public enum CompressionMethod
{
    RDP_Linear,      // 線形セグメント付きRDP
    RDP_BSpline,     // B-スプライン評価付きRDP
    RDP_Bezier,      // ベジェ評価付きRDP
    BSpline_Direct,  // 直接B-スプライン近似
    Bezier_Direct    // 直接ベジェ近似
}
```

### CompressionDataType
```csharp
public enum CompressionDataType
{
    Animation,      // 滑らかなアニメーション曲線
    SensorData,     // ノイズの多いセンサー読み値
    FinancialData   // 金融時系列
}
```

### CurveType
```csharp
public enum CurveType
{
    Linear,    // 線形補間
    BSpline,   // B-スプライン曲線
    Bezier     // ベジェ曲線
}
```

### ImportanceWeights
**場所**: `CurveCompression.DataStructures.ImportanceWeights`
**タイプ**: `class`（参照型）

```csharp
[Serializable]
public class ImportanceWeights
{
    public float curvatureWeight = 1.0f;      // 曲率重要度
    public float velocityWeight = 1.0f;       // 速度変化重要度
    public float accelerationWeight = 1.0f;   // 加速度重要度
    public float temporalWeight = 1.0f;       // 時間間隔重要度
    
    // 事前定義設定
    public static ImportanceWeights Default => new ImportanceWeights();
    public static ImportanceWeights ForAnimation => new ImportanceWeights
    {
        curvatureWeight = 2.0f,
        velocityWeight = 1.5f,
        accelerationWeight = 1.0f,
        temporalWeight = 0.8f
    };
    public static ImportanceWeights ForSensorData => new ImportanceWeights
    {
        curvatureWeight = 1.0f,
        velocityWeight = 2.0f,
        accelerationWeight = 1.5f,
        temporalWeight = 1.2f
    };
    public static ImportanceWeights ForFinancialData => new ImportanceWeights
    {
        curvatureWeight = 3.0f,
        velocityWeight = 2.0f,
        accelerationWeight = 2.5f,
        temporalWeight = 1.0f
    };
    
    // メソッド
    public ImportanceWeights Clone();
    public bool Equals(ImportanceWeights other);
}
```

## 分析と推奨タイプ

### AlgorithmSelector.DataAnalysis
```csharp
public struct DataAnalysis
{
    public float smoothness;         // 滑らかさ測定（0-1）
    public float complexity;         // データ複雑度（0-1）
    public float noiseLevel;         // ノイズ推定（0-1）
    public float variability;        // 値変動性（0+）
    public float temporalDensity;    // 時間単位あたりポイント数
    public CompressionDataType recommendedDataType; // 分類されたデータタイプ
}
```

### AlgorithmSelector.AlgorithmRecommendation
```csharp
public struct AlgorithmRecommendation
{
    public CompressionMethod primaryMethod;    // 最適アルゴリズム
    public CompressionMethod fallbackMethod;  // 代替アルゴリズム
    public float confidence;                   // 信頼度（0-1）
    public string reasoning;                   // 選択説明
    public Dictionary<string, float> scores;  // 全アルゴリズムスコア
}
```

### AdaptiveTolerance.QualityLevel
```csharp
public enum QualityLevel
{
    Low,        // 高速圧縮、低品質
    Medium,     // バランス速度と品質
    High,       // 高品質圧縮
    Lossless    // 最大品質保持
}
```

### AdaptiveTolerance.AdaptiveToleranceResult
```csharp
public struct AdaptiveToleranceResult
{
    public float recommendedTolerance;    // 計算された許容誤差
    public float minTolerance;           // 最小推奨
    public float maxTolerance;           // 最大推奨
    public float dataRange;              // 入力データ範囲
    public float noiseLevel;             // 推定ノイズ
    public string reasoning;             // 計算説明
    public Dictionary<string, float> metrics; // 詳細メトリクス
}
```

### ControlPointEstimator.EstimationResult
```csharp
public class EstimationResult
{
    public int optimalPoints;                    // 推奨ポイント数
    public float score;                          // 品質スコア
    public string method;                        // 推定メソッド
    public Dictionary<string, float> metrics;    // メソッド固有メトリクス
    
    public EstimationResult(int points, float score, string method);
}
```

## メモリレイアウトとパフォーマンス

### メモリ特性
```
データ構造          サイズ        タイプ        使用パターン
TimeValuePair      8 bytes      値型         高頻度作成
CurveSegment      ~40 bytes     値型         中程度作成
CompressionParams ~64 bytes     参照型       低頻度作成
CompressionResult ~100+ bytes   参照型       圧縮毎
CompressedCurveData Variable     参照型       結果保存
```

### パフォーマンス考慮事項

#### 値型対参照型
- **値型（struct）**: 小さく、頻繁にコピーされるデータに使用
  - `TimeValuePair`: コアデータ単位
  - `CurveSegment`: 曲線表現単位
  - 分析結果構造

- **参照型（class）**: より大きく、共有されるデータに使用
  - `CompressionParams`: 設定オブジェクト
  - `CompressionResult`: 複雑な結果データ
  - `CompressedCurveData`: 可変サイズ曲線データ

#### メモリ最適化戦略
```csharp
// 一時配列の配列プーリング
var tempArray = ArrayPool<TimeValuePair>.Shared.Rent(size);
try
{
    // 配列使用
}
finally
{
    ArrayPool<TimeValuePair>.Shared.Return(tempArray);
}

// パラメータオブジェクトの構造体再利用
var segment = CurveSegment.CreateLinear(start.time, start.value, end.time, end.value);

// 設定の参照共有
var sharedParams = new CompressionParams { tolerance = 0.01f };
// 複数圧縮でsharedParamsを使用
```

## シリアライゼーションとUnity統合

### Unityシリアライゼーションサポート
```csharp
[Serializable]  // Unityシリアライゼーション有効化
public struct TimeValuePair { /* ... */ }

[Serializable]
public class CompressionParams { /* ... */ }

// カスタムPropertyDrawerサポート
[CustomPropertyDrawer(typeof(CompressionParams))]
public class CompressionParamsDrawer : PropertyDrawer { /* ... */ }
```

### Inspector統合
```csharp
public class CompressionDemo : MonoBehaviour
{
    [SerializeField] private CompressionParams parameters;
    [SerializeField] private TimeValuePair[] testData;
    [SerializeField] private AdaptiveTolerance.QualityLevel quality;
    
    // 自動Inspector編集サポート
}
```

## 拡張パターン

### 新しい曲線タイプの追加
1. **CurveType列挙型への追加**:
   ```csharp
   public enum CurveType
   {
       Linear, BSpline, Bezier,
       NewCurveType  // 新しいタイプ追加
   }
   ```

2. **CurveSegment評価の拡張**:
   ```csharp
   public float Evaluate(float time)
   {
       return curveType switch
       {
           // ... 既存ケース
           CurveType.NewCurveType => EvaluateNewCurveType(time),
           _ => defaultEvaluation
       };
   }
   ```

3. **ファクトリーメソッドの追加**:
   ```csharp
   public static CurveSegment CreateNewCurveType(/* パラメータ */)
   {
       return new CurveSegment
       {
           curveType = CurveType.NewCurveType,
           // ... 他のフィールド設定
       };
   }
   ```

### 新しいメトリクスの追加
1. **CompressionResultの拡張**:
   ```csharp
   public class CompressionResult
   {
       // ... 既存メトリクス
       public float newMetric;
   }
   ```

2. **計算の実装**:
   ```csharp
   private void CalculateNewMetric(TimeValuePair[] original, CompressedCurveData compressed)
   {
       // 計算ロジック
       newMetric = calculatedValue;
   }
   ```

このデータ構造設計は、優れたパフォーマンス、型安全性、拡張性を提供すると同時に、明確な関心の分離とUnity統合互換性を維持しています。
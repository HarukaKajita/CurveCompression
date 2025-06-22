# データ構造詳細仕様

## 概要

CurveCompressionライブラリの全データ構造について、設計思想、使用方法、内部実装を詳しく解説します。

## 基本データ型

### TimeValuePair
**役割**: 時系列データの基本単位

```csharp
[Serializable]
public struct TimeValuePair : IComparable<TimeValuePair>
{
    public float time;
    public float value;
    
    public TimeValuePair(float time, float value);
    public int CompareTo(TimeValuePair other);
    public override string ToString();
}
```

**設計上の特徴**:
- **値型（struct）**: メモリ効率とコピーコスト最小化
- **IComparable実装**: 時間順ソートのサポート
- **Serializable**: Unity Inspector表示対応
- **不変性**: 作成後の変更不可（データ整合性保証）

**使用例**:
```csharp
var dataPoint = new TimeValuePair(1.5f, 0.8f);
var dataArray = new TimeValuePair[] {
    new TimeValuePair(0.0f, 0.0f),
    new TimeValuePair(1.0f, 1.0f),
    new TimeValuePair(2.0f, 0.5f)
};
Array.Sort(dataArray); // 時間順ソート
```

## 圧縮設定データ

### CompressionParams
**役割**: 圧縮処理の設定パラメータ

```csharp
[Serializable]
public class CompressionParams
{
    // プロパティ（検証付き）
    public float tolerance { get; set; }                    // 許容誤差
    public float importanceThreshold { get; set; }          // 重要度閾値
    
    // フィールド
    public CompressionMethod compressionMethod;             // 圧縮手法
    public CompressionDataType dataType;                   // データ種別
    public ImportanceWeights importanceWeights;            // 重要度重み
    
    public CompressionParams Clone();
    public bool Equals(CompressionParams other);
}
```

**検証機能**:
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
```

**設計思想**:
- **プロパティベース検証**: 不正値の事前防止
- **シリアライゼーション対応**: Unity Inspector編集可能
- **複製とイコール**: 設定の比較・保存機能

### 列挙型定義

#### CompressionMethod
```csharp
public enum CompressionMethod
{
    RDP_Linear,      // RDP + 線形セグメント
    RDP_BSpline,     // RDP + B-スプライン評価
    RDP_Bezier,      // RDP + ベジェ評価
    BSpline_Direct,  // 直接B-スプライン近似
    Bezier_Direct    // 直接ベジェ近似
}
```

#### CompressionDataType
```csharp
public enum CompressionDataType
{
    Animation,      // アニメーションデータ
    SensorData,     // センサーデータ
    FinancialData   // 金融データ
}
```

#### CurveType
```csharp
public enum CurveType
{
    Linear,    // 線形補間
    BSpline,   // B-スプライン
    Bezier     // ベジェ曲線
}
```

### ImportanceWeights
**役割**: データ種別ごとの重要度重み

```csharp
[Serializable]
public class ImportanceWeights
{
    public float curvatureWeight = 1.0f;      // 曲率重み
    public float velocityWeight = 1.0f;       // 速度重み
    public float accelerationWeight = 1.0f;   // 加速度重み
    public float temporalWeight = 1.0f;       // 時間重み
    
    // 定義済み設定
    public static ImportanceWeights Default { get; }
    public static ImportanceWeights ForAnimation { get; }
    public static ImportanceWeights ForSensorData { get; }
    public static ImportanceWeights ForFinancialData { get; }
}
```

## 圧縮結果データ

### CompressedCurveData
**役割**: 圧縮された曲線データの表現

```csharp
[Serializable]
public class CompressedCurveData
{
    public CurveSegment[] segments;
    
    public CompressedCurveData(CurveSegment[] segments);
    public float Evaluate(float time);
    public TimeValuePair[] ToTimeValuePairs(int sampleCount);
    public Vector2[] ToVector2Array(int sampleCount);
}
```

**主要機能**:

#### 1. 時間評価
```csharp
public float Evaluate(float time)
{
    // 該当するセグメントを探索
    foreach (var segment in segments)
    {
        if (time >= segment.startTime && time <= segment.endTime)
        {
            return segment.Evaluate(time);
        }
    }
    // 範囲外処理
    return time < segments[0].startTime ? segments[0].startValue : segments[^1].endValue;
}
```

#### 2. サンプリング
```csharp
public TimeValuePair[] ToTimeValuePairs(int sampleCount)
{
    var result = new TimeValuePair[sampleCount];
    float startTime = segments[0].startTime;
    float endTime = segments[^1].endTime;
    
    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / (sampleCount - 1);
        float time = startTime + t * (endTime - startTime);
        result[i] = new TimeValuePair(time, Evaluate(time));
    }
    return result;
}
```

### CurveSegment
**役割**: 個々の曲線セグメント

```csharp
[Serializable]
public struct CurveSegment
{
    // 共通フィールド
    public CurveType curveType;
    public float startTime, startValue;
    public float endTime, endValue;
    
    // ベジェ固有
    public float inTangent, outTangent;
    
    // B-スプライン固有
    public Vector2[] bsplineControlPoints;
    
    // ファクトリーメソッド
    public static CurveSegment CreateLinear(float startTime, float startValue, float endTime, float endValue);
    public static CurveSegment CreateBezier(float startTime, float startValue, float endTime, float endValue, float inTangent, float outTangent);
    public static CurveSegment CreateBSpline(Vector2[] controlPoints);
    
    // 評価
    public float Evaluate(float time);
}
```

**曲線タイプ別評価**:

#### 線形評価
```csharp
private float EvaluateLinear(float time)
{
    float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
    return Mathf.Lerp(startValue, endValue, t);
}
```

#### ベジェ評価
```csharp
private float EvaluateBezier(float time)
{
    float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
    float dt = endTime - startTime;
    
    // エルミート補間
    return InterpolationUtils.HermiteInterpolate(
        startValue, endValue,
        inTangent * dt, outTangent * dt, t);
}
```

#### B-スプライン評価
```csharp
private float EvaluateBSpline(float time)
{
    if (bsplineControlPoints.Length < 2) return startValue;
    
    float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
    return EvaluateBSplineInternal(bsplineControlPoints, t);
}
```

### CompressionResult
**役割**: 圧縮処理の結果とメトリクス

```csharp
public class CompressionResult
{
    // 結果データ
    public TimeValuePair[] compressedData;      // レガシー形式
    public CompressedCurveData compressedCurve; // 新形式
    
    // メトリクス
    public float compressionRatio;              // 圧縮率
    public float maxError;                      // 最大誤差
    public float avgError;                      // 平均誤差
    public int originalCount;                   // 元データ点数
    public int compressedCount;                 // 圧縮後点数/セグメント数
    
    // コンストラクタ
    public CompressionResult(TimeValuePair[] original, TimeValuePair[] compressed);
    public CompressionResult(TimeValuePair[] original, CompressedCurveData compressed);
}
```

**メトリクス計算**:
```csharp
private void CalculateErrorsWithCurve(TimeValuePair[] original, CompressedCurveData compressed)
{
    float totalError = 0f;
    maxError = 0f;
    
    for (int i = 0; i < original.Length; i++)
    {
        float curveValue = compressed.Evaluate(original[i].time);
        float error = Mathf.Abs(original[i].value - curveValue);
        totalError += error;
        maxError = Mathf.Max(maxError, error);
    }
    
    avgError = totalError / original.Length;
}
```

## メモリレイアウトと最適化

### 構造体 vs クラス設計判断

#### 構造体採用 (値型)
- **TimeValuePair**: 小さく、頻繁にコピーされる
- **CurveSegment**: 設定後不変、配列で管理

#### クラス採用 (参照型)
- **CompressionParams**: 設定変更可能、共有される
- **CompressedCurveData**: 大きなデータ、ポリモーフィズム
- **CompressionResult**: 複合データ、拡張性

### パフォーマンス特性

#### メモリ効率
```
TimeValuePair:      8 bytes (float×2)
CurveSegment:      ~40 bytes (型依存)
CompressedCurveData: 動的 (セグメント数×40)
```

#### アクセスパターン
- **順次アクセス**: 配列の線形スキャン最適化
- **バイナリサーチ**: 時間検索の O(log n) 実現
- **キャッシュ効率**: データ局所性を考慮した配置

## 型安全性と検証

### コンパイル時検証
- **型システム**: enum による選択肢制限
- **インターフェース**: 契約による設計
- **ジェネリクス**: 型パラメータ安全性

### 実行時検証
```csharp
public static void ValidatePoints(TimeValuePair[] points, string paramName = "points", int minRequired = 2)
{
    if (points == null)
        throw new ArgumentNullException(paramName);
    if (points.Length < minRequired)
        throw new ArgumentException($"At least {minRequired} points required");
}
```

## 拡張とカスタマイゼーション

### 新しい曲線タイプの追加
1. `CurveType` 列挙型に追加
2. `CurveSegment.Evaluate()` に評価ロジック追加
3. `CurveSegment.Create...()` ファクトリーメソッド追加

### カスタムメトリクスの追加
1. `CompressionResult` にフィールド追加
2. 計算ロジックをコンストラクタに実装
3. 関連するユーティリティメソッド追加

この設計により、型安全で高性能、拡張可能なデータ構造を実現し、Unity環境での効率的な曲線圧縮を可能にしています。
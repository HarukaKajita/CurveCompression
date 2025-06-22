# アルゴリズム実装詳細

## 概要

このドキュメントでは、CurveCompressionライブラリの現在のアルゴリズム実装について、数学的基礎、実装詳細、パフォーマンス特性を含む詳細情報を提供します。

## 圧縮アルゴリズム

### 1. Ramer-Douglas-Peucker (RDP) アルゴリズム

#### 数学的基礎
RDPアルゴリズムは、線分からの最大距離を持つ点を再帰的に見つけ、許容誤差閾値に基づいて細分化することで曲線を単純化します。

**距離計算**:
```
点Pと線分ABに対して:
distance = |((B.y - A.y) * P.x - (B.x - A.x) * P.y + B.x * A.y - B.y * A.x)| / 
           sqrt((B.y - A.y)² + (B.x - A.x)²)
```

#### 実装詳細

**コアアルゴリズム**:
```csharp
private static List<int> SimplifyIndices(TimeValuePair[] points, float tolerance, 
                                        int startIndex, int endIndex, 
                                        ImportanceWeights weights)
{
    float maxDistance = 0;
    int maxIndex = 0;
    
    // 最大距離の点を見つける
    for (int i = startIndex + 1; i < endIndex; i++)
    {
        float distance = PerpendicularDistance(points, i, startIndex, endIndex);
        
        // 重要度重み付けを適用
        float importance = CalculateImportance(points, i, weights);
        distance *= (1.0f + importance * importanceThreshold);
        
        if (distance > maxDistance)
        {
            maxDistance = distance;
            maxIndex = i;
        }
    }
    
    var result = new List<int>();
    
    if (maxDistance > tolerance)
    {
        // 再帰細分化
        var leftResults = SimplifyIndices(points, tolerance, startIndex, maxIndex, weights);
        var rightResults = SimplifyIndices(points, tolerance, maxIndex, endIndex, weights);
        
        result.AddRange(leftResults);
        result.Add(maxIndex);
        result.AddRange(rightResults);
    }
    
    return result;
}
```

**重要度計算**:
```csharp
private static float CalculateImportance(TimeValuePair[] points, int index, ImportanceWeights weights)
{
    if (index <= 0 || index >= points.Length - 1 || weights == null)
        return 0f;
    
    float importance = 0f;
    
    // 曲率重要度
    float curvature = CalculateLocalCurvature(points, index);
    importance += curvature * weights.curvatureWeight;
    
    // 速度重要度
    float velocity = CalculateVelocityChange(points, index);
    importance += velocity * weights.velocityWeight;
    
    // 加速度重要度
    float acceleration = CalculateAcceleration(points, index);
    importance += acceleration * weights.accelerationWeight;
    
    // 時間重要度
    float temporalSpacing = CalculateTemporalSpacing(points, index);
    importance += temporalSpacing * weights.temporalWeight;
    
    return Mathf.Clamp01(importance);
}
```

**パフォーマンス特性**:
- 時間計算量: 平均O(n log n)、最悪O(n²)
- 空間計算量: 再帰スタック用O(log n)
- メモリ使用量: 最小限の追加割り当て

#### 曲線タイプサポート

**線形出力**:
```csharp
var segments = new List<CurveSegment>();
for (int i = 0; i < keyIndices.Count - 1; i++)
{
    var segment = CurveSegment.CreateLinear(
        points[keyIndices[i]].time, points[keyIndices[i]].value,
        points[keyIndices[i + 1]].time, points[keyIndices[i + 1]].value);
    segments.Add(segment);
}
```

**ベジェ/B-スプライン出力**:
RDPアルゴリズムは最初にキーポイントを特定し、指定された曲線タイプを使用してそれらの間に曲線を適合させます。

### 2. B-スプラインアルゴリズム

#### 数学的基礎
B-スプライン曲線は基底関数を使用して制御点を通る滑らかな補間を提供します。

**B-スプライン基底関数（3次）**:
```
N₀,₃(t) = (1-t)³/6
N₁,₃(t) = (3t³ - 6t² + 4)/6
N₂,₃(t) = (-3t³ + 3t² + 3t + 1)/6
N₃,₃(t) = t³/6
```

#### 実装詳細

**制御点選択**:
```csharp
private static Vector2[] SelectControlPoints(TimeValuePair[] points, int numControlPoints)
{
    var controlPoints = new Vector2[numControlPoints];
    
    // 均等パラメータ分布
    for (int i = 0; i < numControlPoints; i++)
    {
        float t = (float)i / (numControlPoints - 1);
        int index = Mathf.RoundToInt(t * (points.Length - 1));
        controlPoints[i] = new Vector2(points[index].time, points[index].value);
    }
    
    return controlPoints;
}
```

**制御点最適化**:
```csharp
private static Vector2[] OptimizeControlPoints(TimeValuePair[] originalPoints, Vector2[] initialControlPoints)
{
    const int maxIterations = 10;
    const float convergenceThreshold = 0.0001f;
    
    var optimizedPoints = new Vector2[initialControlPoints.Length];
    Array.Copy(initialControlPoints, optimizedPoints, initialControlPoints.Length);
    
    for (int iteration = 0; iteration < maxIterations; iteration++)
    {
        var gradients = CalculateGradients(originalPoints, optimizedPoints);
        
        // 勾配降下法を適用
        float stepSize = 0.1f / (iteration + 1);
        bool converged = true;
        
        for (int i = 1; i < optimizedPoints.Length - 1; i++) // 端点は移動しない
        {
            var newPosition = optimizedPoints[i] - gradients[i] * stepSize;
            
            if (Vector2.Distance(newPosition, optimizedPoints[i]) > convergenceThreshold)
                converged = false;
                
            optimizedPoints[i] = newPosition;
        }
        
        if (converged) break;
    }
    
    return optimizedPoints;
}
```

**B-スプライン評価**:
```csharp
private static float EvaluateBSplineInternal(Vector2[] controlPoints, float t)
{
    if (controlPoints.Length < 2) return 0f;
    
    // パラメータを有効範囲にクランプ
    t = Mathf.Clamp01(t);
    
    // n個の制御点を持つ3次B-スプライン
    int n = controlPoints.Length;
    float scaledT = t * (n - 3); // 制御点範囲にスケール
    int baseIndex = Mathf.FloorToInt(scaledT);
    float localT = scaledT - baseIndex;
    
    // 十分な制御点があることを確認
    baseIndex = Mathf.Clamp(baseIndex, 0, n - 4);
    
    // 3次B-スプライン基底関数
    float b0 = (1 - localT) * (1 - localT) * (1 - localT) / 6f;
    float b1 = (3 * localT * localT * localT - 6 * localT * localT + 4) / 6f;
    float b2 = (-3 * localT * localT * localT + 3 * localT * localT + 3 * localT + 1) / 6f;
    float b3 = localT * localT * localT / 6f;
    
    // 値の補間
    return b0 * controlPoints[baseIndex].y +
           b1 * controlPoints[baseIndex + 1].y +
           b2 * controlPoints[baseIndex + 2].y +
           b3 * controlPoints[baseIndex + 3].y;
}
```

**パフォーマンス特性**:
- 時間計算量: フィッティングO(n)、評価O(1)
- 空間計算量: O(k) ここでkは制御点数
- メモリ使用量: 制御点配列ストレージ

### 3. ベジェアルゴリズム

#### 数学的基礎
ベジェ曲線は制御点と接線を使用して滑らかな曲線を作成します。3次ベジェ曲線の場合：

**3次ベジェ公式**:
```
B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃

ここで:
P₀, P₃ = 端点
P₁, P₂ = 接線から導出される制御点
```

#### 実装詳細

**接線計算**:
```csharp
private static float CalculateInTangent(TimeValuePair[] points, int start, int end)
{
    const int sampleCount = 3;
    float totalWeight = 0f;
    float weightedSlope = 0f;
    
    for (int i = 0; i < sampleCount && start + i + 1 < end; i++)
    {
        int idx1 = start + i;
        int idx2 = start + i + 1;
        
        if (idx2 < points.Length)
        {
            float dt = points[idx2].time - points[idx1].time;
            if (dt > 0.0001f)
            {
                float weight = 1.0f / (i + 1);
                float slope = (points[idx2].value - points[idx1].value) / dt;
                
                weightedSlope += slope * weight;
                totalWeight += weight;
            }
        }
    }
    
    return totalWeight > 0 ? weightedSlope / totalWeight : 0f;
}

private static float CalculateOutTangent(TimeValuePair[] points, int start, int end)
{
    const int sampleCount = 3;
    float totalWeight = 0f;
    float weightedSlope = 0f;
    
    for (int i = 0; i < sampleCount && end - i - 1 > start; i++)
    {
        int idx1 = end - i - 1;
        int idx2 = end - i;
        
        if (idx2 < points.Length)
        {
            float dt = points[idx2].time - points[idx1].time;
            if (dt > 0.0001f)
            {
                float weight = 1.0f / (i + 1);
                float slope = (points[idx2].value - points[idx1].value) / dt;
                
                weightedSlope += slope * weight;
                totalWeight += weight;
            }
        }
    }
    
    return totalWeight > 0 ? weightedSlope / totalWeight : 0f;
}
```

**セグメント作成**:
```csharp
public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
{
    var segments = new List<CurveSegment>();
    
    // 曲率に基づく適応的セグメンテーション
    var segmentIndices = FindOptimalSegmentPoints(points, tolerance);
    
    for (int i = 0; i < segmentIndices.Count - 1; i++)
    {
        int startIdx = segmentIndices[i];
        int endIdx = segmentIndices[i + 1];
        
        float inTangent = CalculateInTangent(points, startIdx, endIdx);
        float outTangent = CalculateOutTangent(points, startIdx, endIdx);
        
        var segment = CurveSegment.CreateBezier(
            points[startIdx].time, points[startIdx].value,
            points[endIdx].time, points[endIdx].value,
            inTangent, outTangent);
            
        segments.Add(segment);
    }
    
    return new CompressedCurveData(segments.ToArray());
}
```

**パフォーマンス特性**:
- 時間計算量: セグメント作成O(n)、評価O(1)
- 空間計算量: O(s) ここでsはセグメント数
- メモリ使用量: セグメントあたり4float（開始、終了、入力/出力接線）

## 制御点推定アルゴリズム

### 1. エルボー法

#### 数学的基礎
二階微分分析を使用してエラー対制御点数曲線の「エルボー」点を見つけます。

**二階微分計算**:
```csharp
private static EstimationResult EstimateByElbowMethod(TimeValuePair[] data, float tolerance, 
                                                     int minPoints, int maxPoints)
{
    var errors = new List<float>();
    
    // 各制御点数でのエラーを計算
    for (int n = minPoints; n <= maxPoints; n++)
    {
        var compressed = BSplineAlgorithm.ApproximateWithFixedPoints(data, n);
        float error = CalculateMeanSquaredError(data, compressed);
        errors.Add(error);
    }
    
    // 二階微分を計算
    var secondDerivatives = new List<float>();
    for (int i = 1; i < errors.Count - 1; i++)
    {
        float d2 = errors[i + 1] - 2 * errors[i] + errors[i - 1];
        secondDerivatives.Add(Mathf.Abs(d2));
    }
    
    // 最大曲率点を見つける
    int elbowIndex = 0;
    float maxCurvature = 0;
    for (int i = 0; i < secondDerivatives.Count; i++)
    {
        if (secondDerivatives[i] > maxCurvature)
        {
            maxCurvature = secondDerivatives[i];
            elbowIndex = i + 1;
        }
    }
    
    return new EstimationResult(minPoints + elbowIndex, errors[elbowIndex], "Elbow Method");
}
```

### 2. 曲率解析

#### 数学的基礎
必要な制御点を推定するために局所曲率分布を分析します。

**曲率計算**:
```csharp
private static float CalculateLocalCurvature(TimeValuePair[] data, int index)
{
    if (index <= 0 || index >= data.Length - 1) return 0;
    
    Vector2 prev = new Vector2(data[index - 1].time, data[index - 1].value);
    Vector2 curr = new Vector2(data[index].time, data[index].value);
    Vector2 next = new Vector2(data[index + 1].time, data[index + 1].value);
    
    Vector2 v1 = (curr - prev).normalized;
    Vector2 v2 = (next - curr).normalized;
    
    float dot = Vector2.Dot(v1, v2);
    float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
    
    return angle;
}
```

**分布解析**:
```csharp
private static EstimationResult EstimateByCurvature(TimeValuePair[] data, float tolerance, 
                                                   int minPoints, int maxPoints)
{
    float totalCurvature = 0;
    var curvatures = new List<float>();
    
    // すべての曲率を計算
    for (int i = 1; i < data.Length - 1; i++)
    {
        float curvature = CalculateLocalCurvature(data, i);
        curvatures.Add(curvature);
        totalCurvature += curvature;
    }
    
    // 曲率を降順でソート
    curvatures.Sort((a, b) => b.CompareTo(a));
    
    // 総曲率の90%を占める点を見つける
    float cumulativeCurvature = 0;
    int significantPoints = 0;
    float threshold = totalCurvature * 0.9f;
    
    foreach (float c in curvatures)
    {
        cumulativeCurvature += c;
        significantPoints++;
        if (cumulativeCurvature >= threshold)
            break;
    }
    
    // 合理的範囲にスケール
    int optimalPoints = Mathf.Clamp(
        Mathf.RoundToInt(significantPoints * 0.5f + minPoints),
        minPoints, maxPoints);
    
    return new EstimationResult(optimalPoints, totalCurvature, "Curvature Based");
}
```

### 3. 情報エントロピー

#### 数学的基礎
情報理論を使用してデータの複雑さを測定し、必要な制御点を決定します。

**エントロピー計算**:
```csharp
private static float CalculateEntropy(TimeValuePair[] data)
{
    if (data.Length <= 1) return 0;
    
    // ヒストグラムを作成
    int bins = Mathf.Max(1, Mathf.Min(20, data.Length / 5));
    float min = data.Min(p => p.value);
    float max = data.Max(p => p.value);
    float range = max - min;
    
    if (range < 0.0001f) return 0;
    
    var histogram = new int[bins];
    foreach (var point in data)
    {
        int bin = Mathf.Clamp((int)((point.value - min) / range * (bins - 1)), 0, bins - 1);
        histogram[bin]++;
    }
    
    // シャノンエントロピーを計算
    float entropy = 0;
    float total = data.Length;
    
    foreach (int count in histogram)
    {
        if (count > 0)
        {
            float p = count / total;
            entropy -= p * Mathf.Log(p, 2);
        }
    }
    
    return entropy;
}
```

### 4. 統計解析

#### 数学的基礎
信号対雑音比と分散解析を使用して最適な制御点数を決定します。

**SNR計算**:
```csharp
private static EstimationResult DetermineByStatistical(TimeValuePair[] data, float tolerance)
{
    // 基本統計
    float mean = data.Average(p => p.value);
    float variance = data.Average(p => (p.value - mean) * (p.value - mean));
    float stdDev = Mathf.Sqrt(variance);
    
    // ノイズレベル推定
    float noiseLevel = EstimateNoiseLevel(data);
    
    // 信号対雑音比
    float snr = stdDev / (noiseLevel + 0.0001f);
    
    // SNRに基づく適応的上限
    int maxPoints = Mathf.Clamp(
        Mathf.RoundToInt(10 + snr * 5),
        10,
        Mathf.Min(data.Length / 2, 200));
    
    var result = new EstimationResult(maxPoints, snr, "Statistical");
    result.metrics["variance"] = variance;
    result.metrics["noise_level"] = noiseLevel;
    result.metrics["snr"] = snr;
    
    return result;
}
```

## インテリジェント選択システム

### アルゴリズム選択ロジック

#### データ解析パイプライン
```csharp
public static DataAnalysis AnalyzeDataCharacteristics(TimeValuePair[] data)
{
    var analysis = new DataAnalysis();
    
    // 1. 滑らかさ計算
    analysis.smoothness = CalculateSmoothness(data);
    
    // 2. 複雑度測定
    analysis.complexity = CalculateComplexity(data);
    
    // 3. ノイズレベル推定
    analysis.noiseLevel = EstimateNoiseLevel(data);
    
    // 4. 変動性計算
    float mean = CalculateMean(data);
    float stdDev = Mathf.Sqrt(CalculateVariance(data, mean));
    analysis.variability = stdDev / (Mathf.Abs(mean) + 0.001f);
    
    // 5. 時間密度
    analysis.temporalDensity = CalculateTemporalDensity(data);
    
    // 6. データタイプ分類
    analysis.recommendedDataType = ClassifyDataType(analysis);
    
    return analysis;
}
```

#### アルゴリズムスコアリング
```csharp
private static Dictionary<CompressionMethod, float> CalculateAlgorithmScores(
    DataAnalysis analysis, CompressionParams parameters)
{
    var scores = new Dictionary<CompressionMethod, float>();
    
    // RDP_Linear: 高速、ノイズ耐性
    scores[CompressionMethod.RDP_Linear] = 
        0.8f - analysis.complexity * 0.4f +      // より単純なデータが好ましい
        analysis.noiseLevel * 0.3f +             // ノイズが多いほど有利
        (1f - analysis.smoothness) * 0.2f;       // 非滑らかなデータに適している
    
    // Bezier_Direct: 高品質、滑らかなデータ
    scores[CompressionMethod.Bezier_Direct] = 
        0.8f + analysis.smoothness * 0.5f +      // 滑らかなデータに優秀
        (analysis.recommendedDataType == CompressionDataType.Animation ? 0.4f : 0f) +
        (1f - analysis.noiseLevel) * 0.3f;       // 低ノイズが必要
    
    // パフォーマンス調整を適用
    if (parameters != null)
    {
        AdjustScoresForPerformance(scores, analysis, parameters);
    }
    
    // スコアを正規化
    NormalizeScores(scores);
    
    return scores;
}
```

### 適応的許容誤差システム

#### 品質ベース許容誤差
```csharp
private static float CalculateBaseTolerance(DataToleranceAnalysis analysis, QualityLevel quality)
{
    // データ範囲に対する基本比率
    float baseRatio = quality switch
    {
        QualityLevel.Low => 0.05f,      // 範囲の5%
        QualityLevel.Medium => 0.01f,   // 範囲の1%
        QualityLevel.High => 0.002f,    // 範囲の0.2%
        QualityLevel.Lossless => 0.0001f, // 範囲の0.01%
        _ => 0.01f
    };
    
    // 最小絶対許容誤差
    float minAbsoluteTolerance = quality switch
    {
        QualityLevel.Low => 0.01f,
        QualityLevel.Medium => 0.001f,
        QualityLevel.High => 0.0001f,
        QualityLevel.Lossless => 0.00001f,
        _ => 0.001f
    };
    
    float relativeTolerance = analysis.range * baseRatio;
    return Mathf.Max(relativeTolerance, minAbsoluteTolerance);
}
```

#### ノイズベース調整
```csharp
private static float AdjustToleranceForNoise(float baseTolerance, float noiseLevel)
{
    if (noiseLevel < 0.001f)
        return baseTolerance * 0.5f;      // 非常に低ノイズ：より厳しい許容誤差
    else if (noiseLevel < 0.01f)
        return baseTolerance * 0.75f;     // 低ノイズ：やや厳しい
    else if (noiseLevel > 0.1f)
        return baseTolerance * 2.0f;      // 高ノイズ：より緩い許容誤差
    else if (noiseLevel > 0.05f)
        return baseTolerance * 1.5f;      // 中程度ノイズ：やや緩い
    
    return baseTolerance;
}
```

## パフォーマンス最適化技術

### メモリ管理
- **構造体使用**: 頻繁にコピーされるデータの値型
- **配列プーリング**: 一時配列の再利用
- **インプレース操作**: 割り当ての最小化
- **参照カウント**: 不要なオブジェクト作成の回避

### 計算最適化
- **二分探索**: InterpolationUtilsでのO(log n)ルックアップ
- **早期終了**: 不要計算のスキップ
- **ベクトル化操作**: 可能な場所でのSIMDフレンドリー計算
- **適応サンプリング**: 複雑度ベース可変解像度

### キャッシュ戦略
- **結果キャッシュ**: 同一入力での圧縮結果キャッシュ
- **メトリクスキャッシュ**: 高価な解析計算のキャッシュ
- **アルゴリズム選択キャッシュ**: 推奨結果のキャッシュ

この包括的なアルゴリズム実装は、データ特性への知的適応を伴う堅牢で高性能な曲線圧縮を提供します。
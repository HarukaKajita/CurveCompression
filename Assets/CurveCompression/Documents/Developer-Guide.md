# 開発者ガイド

## はじめに

CurveCompressionライブラリを効果的に使用し、拡張するための包括的ガイドです。基本的な使用方法から高度なカスタマイゼーションまでを解説します。

## セットアップと基本使用法

### 1. 名前空間のインポート
```csharp
using CurveCompression.Core;
using CurveCompression.DataStructures;
using CurveCompression.Algorithms;
using CurveCompression.Visualization;
```

### 2. 基本的な圧縮フロー

#### ステップ1: データ準備
```csharp
// サンプルデータの生成
var dataPoints = new TimeValuePair[1000];
for (int i = 0; i < dataPoints.Length; i++)
{
    float time = (float)i / 999f * 10f; // 0-10秒
    float value = Mathf.Sin(time * 2f) + 0.1f * Mathf.Sin(time * 20f); // ノイズ付きsin波
    dataPoints[i] = new TimeValuePair(time, value);
}
```

#### ステップ2: 圧縮パラメータ設定
```csharp
var parameters = new CompressionParams
{
    tolerance = 0.01f,                                  // 1%の誤差許容
    compressionMethod = CompressionMethod.Bezier_Direct, // ベジェ曲線使用
    dataType = CompressionDataType.Animation,           // アニメーションデータ
    importanceThreshold = 1.0f,                         // 標準的な重要度閾値
    importanceWeights = ImportanceWeights.Default       // デフォルト重み
};
```

#### ステップ3: 圧縮実行
```csharp
var result = CurveCompressor.Compress(dataPoints, parameters);

// 結果の確認
Debug.Log($"圧縮率: {result.compressionRatio:F3}");
Debug.Log($"最大誤差: {result.maxError:F6}");
Debug.Log($"平均誤差: {result.avgError:F6}");
```

### 3. Unity統合での使用

#### AnimationCurveからの圧縮
```csharp
// 既存のAnimationCurveを圧縮
public AnimationCurve CompressAnimationCurve(AnimationCurve originalCurve, float tolerance)
{
    // 1. AnimationCurveをTimeValuePairに変換
    var timeValuePairs = UnityCompressionUtils.FromAnimationCurve(originalCurve, 1000);
    
    // 2. 圧縮実行
    var result = CurveCompressor.Compress(timeValuePairs, tolerance);
    
    // 3. 圧縮結果をAnimationCurveに戻す
    return UnityCompressionUtils.ToAnimationCurve(result.compressedCurve);
}
```

#### リアルタイム可視化
```csharp
public class RealtimeCompressionVisualizer : MonoBehaviour
{
    [SerializeField] private CompressionParams compressionParams;
    [SerializeField] private bool autoUpdate = true;
    
    private CurveVisualizer visualizer;
    private TimeValuePair[] currentData;
    
    void Start()
    {
        visualizer = GetComponent<CurveVisualizer>();
        GenerateTestData();
        UpdateVisualization();
    }
    
    void Update()
    {
        if (autoUpdate && Time.frameCount % 60 == 0) // 1秒毎に更新
        {
            UpdateVisualization();
        }
    }
    
    private void UpdateVisualization()
    {
        var result = CurveCompressor.Compress(currentData, compressionParams);
        visualizer.VisualizeData(currentData, result);
    }
}
```

## 高度な使用方法

### 1. カスタム重要度重みの設定

#### 金融データ用カスタム重み
```csharp
var financialWeights = new ImportanceWeights
{
    curvatureWeight = 2.0f,      // 急激な変化を重視
    velocityWeight = 1.5f,       // 変化率を重視
    accelerationWeight = 3.0f,   // 加速度変化を最重視
    temporalWeight = 0.5f        // 時間間隔は軽視
};

var params = new CompressionParams
{
    tolerance = 0.001f,                          // 高精度
    compressionMethod = CompressionMethod.RDP_Linear,
    dataType = CompressionDataType.FinancialData,
    importanceWeights = financialWeights         // カスタム重み使用
};
```

#### 動的重み調整
```csharp
public ImportanceWeights CalculateDynamicWeights(TimeValuePair[] data)
{
    // データの特性分析
    float averageVariation = CalculateVariation(data);
    float noiseLevel = EstimateNoiseLevel(data);
    
    // 特性に基づく重み調整
    return new ImportanceWeights
    {
        curvatureWeight = averageVariation > 0.1f ? 2.0f : 1.0f,
        velocityWeight = noiseLevel < 0.01f ? 1.5f : 0.8f,
        accelerationWeight = 1.0f,
        temporalWeight = data.Length > 1000 ? 0.5f : 1.0f
    };
}
```

### 2. 自動制御点推定の活用

#### 複数推定手法の比較
```csharp
public int FindOptimalControlPoints(TimeValuePair[] data, float tolerance)
{
    // 全推定アルゴリズムを実行
    var estimates = ControlPointEstimator.EstimateAll(data, tolerance, 2, 50);
    
    // 結果の分析と選択
    var elbow = estimates["Elbow"];
    var curvature = estimates["Curvature"];
    var entropy = estimates["Entropy"];
    
    // 複数手法の結果を統合
    var candidates = new[] { elbow.optimalPoints, curvature.optimalPoints, entropy.optimalPoints };
    
    // 中央値を採用（外れ値に強い）
    Array.Sort(candidates);
    return candidates[1];
}
```

#### データ特性に応じた推定手法選択
```csharp
public string SelectBestEstimationMethod(TimeValuePair[] data)
{
    float dataComplexity = CalculateComplexity(data);
    float noiseLLvel = EstimateNoiseLevel(data);
    
    if (noiseLevel > 0.1f)
        return "Statistical";      // ノイズが多い場合は統計手法
    else if (dataComplexity > 0.5f)
        return "Curvature";       // 複雑な場合は曲率解析
    else
        return "Elbow";           // 一般的な場合はエルボー法
}
```

### 3. アルゴリズム直接使用

#### 特定アルゴリズムの詳細制御
```csharp
// RDPアルゴリズムの直接使用（詳細制御）
public CompressedCurveData CustomRDPCompression(TimeValuePair[] data)
{
    var customWeights = new ImportanceWeights
    {
        curvatureWeight = 3.0f,    // 曲率を最重視
        velocityWeight = 1.0f,
        accelerationWeight = 0.5f,
        temporalWeight = 1.0f
    };
    
    return RDPAlgorithm.CompressWithCurveEvaluation(
        data, 
        0.005f,                    // 高精度
        CurveType.Bezier,          // ベジェ出力
        2.0f,                      // 高い重要度閾値
        customWeights
    );
}

// B-スプラインの固定制御点数圧縮
public CompressedCurveData FixedPointBSplineCompression(TimeValuePair[] data, int targetPoints)
{
    // 自動推定により最適点数を取得
    int estimatedPoints = ControlPointEstimator.EstimateAll(data, 0.01f, 2, 50)["Elbow"].optimalPoints;
    
    // ターゲットと推定値の調整
    int finalPoints = Mathf.Clamp(targetPoints, estimatedPoints / 2, estimatedPoints * 2);
    
    return BSplineAlgorithm.CompressWithFixedControlPoints(data, finalPoints);
}
```

## パフォーマンス最適化

### 1. メモリ効率の改善

#### オブジェクトプールの使用
```csharp
public class CompressionObjectPool
{
    private static readonly Queue<TimeValuePair[]> _dataPool = new Queue<TimeValuePair[]>();
    private static readonly Queue<List<CurveSegment>> _segmentListPool = new Queue<List<CurveSegment>>();
    
    public static TimeValuePair[] RentDataArray(int size)
    {
        if (_dataPool.Count > 0 && _dataPool.Peek().Length >= size)
        {
            return _dataPool.Dequeue();
        }
        return new TimeValuePair[size];
    }
    
    public static void ReturnDataArray(TimeValuePair[] array)
    {
        if (array.Length <= 10000) // 大きすぎる配列は破棄
        {
            _dataPool.Enqueue(array);
        }
    }
}
```

#### インプレース処理
```csharp
// 既存配列の再利用
public void OptimizedCompression(ref TimeValuePair[] data, CompressionParams parameters)
{
    // インプレース前処理
    PreprocessDataInPlace(ref data);
    
    // 圧縮実行
    var result = CurveCompressor.Compress(data, parameters);
    
    // 結果を元配列に書き戻し（サイズ調整）
    if (result.compressedData.Length != data.Length)
    {
        Array.Resize(ref data, result.compressedData.Length);
    }
    Array.Copy(result.compressedData, data, result.compressedData.Length);
}
```

### 2. 並列処理の活用

#### 大規模データの分割処理
```csharp
public async Task<CompressionResult> ParallelCompression(TimeValuePair[] largeData, CompressionParams parameters)
{
    const int chunkSize = 1000;
    var chunks = SplitDataIntoChunks(largeData, chunkSize);
    
    // 並列圧縮
    var tasks = chunks.Select(chunk => Task.Run(() => 
        CurveCompressor.Compress(chunk, parameters)
    ));
    
    var results = await Task.WhenAll(tasks);
    
    // 結果の統合
    return MergeCompressionResults(results);
}

private CompressionResult MergeCompressionResults(CompressionResult[] results)
{
    var allSegments = results.SelectMany(r => r.compressedCurve.segments).ToArray();
    var mergedCurve = new CompressedCurveData(allSegments);
    
    return new CompressionResult(GetOriginalData(), mergedCurve);
}
```

### 3. キャッシュ戦略

#### 計算結果のキャッシュ
```csharp
public class CompressionCache
{
    private readonly Dictionary<CompressionCacheKey, CompressionResult> _cache = 
        new Dictionary<CompressionCacheKey, CompressionResult>();
    
    public CompressionResult GetOrCompute(TimeValuePair[] data, CompressionParams parameters)
    {
        var key = new CompressionCacheKey(data, parameters);
        
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }
        
        var result = CurveCompressor.Compress(data, parameters);
        _cache[key] = result;
        
        return result;
    }
}

public struct CompressionCacheKey : IEquatable<CompressionCacheKey>
{
    private readonly int _dataHash;
    private readonly int _paramsHash;
    
    public CompressionCacheKey(TimeValuePair[] data, CompressionParams parameters)
    {
        _dataHash = CalculateDataHash(data);
        _paramsHash = parameters.GetHashCode();
    }
    
    public bool Equals(CompressionCacheKey other) => 
        _dataHash == other._dataHash && _paramsHash == other._paramsHash;
}
```

## エラーハンドリングとデバッグ

### 1. 堅牢なエラーハンドリング

#### カスタム例外の定義
```csharp
public class CompressionException : Exception
{
    public CompressionMethod FailedMethod { get; }
    public TimeValuePair[] OriginalData { get; }
    
    public CompressionException(string message, CompressionMethod method, TimeValuePair[] data) 
        : base(message)
    {
        FailedMethod = method;
        OriginalData = data;
    }
}
```

#### グレースフルな劣化処理
```csharp
public CompressionResult SafeCompression(TimeValuePair[] data, CompressionParams parameters)
{
    try
    {
        // 主要なアルゴリズムを試行
        return CurveCompressor.Compress(data, parameters);
    }
    catch (CompressionException ex)
    {
        Debug.LogWarning($"Primary compression failed: {ex.Message}");
        
        // フォールバック: より単純なアルゴリズム
        var fallbackParams = parameters.Clone();
        fallbackParams.compressionMethod = CompressionMethod.RDP_Linear;
        fallbackParams.tolerance *= 2f; // 許容誤差を緩める
        
        try
        {
            return CurveCompressor.Compress(data, fallbackParams);
        }
        catch
        {
            // 最終フォールバック: 最小限の圧縮
            return CreateMinimalCompression(data);
        }
    }
}
```

### 2. デバッグとプロファイリング

#### 詳細ログ機能
```csharp
public static class CompressionLogger
{
    public static bool EnableDetailedLogging = false;
    
    public static CompressionResult LoggedCompression(TimeValuePair[] data, CompressionParams parameters)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        if (EnableDetailedLogging)
        {
            Debug.Log($"Starting compression: {data.Length} points, method: {parameters.compressionMethod}");
        }
        
        var result = CurveCompressor.Compress(data, parameters);
        
        stopwatch.Stop();
        
        if (EnableDetailedLogging)
        {
            Debug.Log($"Compression completed in {stopwatch.ElapsedMilliseconds}ms");
            Debug.Log($"Compression ratio: {result.compressionRatio:F3}");
            Debug.Log($"Max error: {result.maxError:F6}");
        }
        
        return result;
    }
}
```

#### パフォーマンス分析
```csharp
public class CompressionProfiler
{
    public struct ProfileResult
    {
        public TimeSpan CompressionTime;
        public long MemoryUsed;
        public float CompressionRatio;
        public float MaxError;
    }
    
    public static ProfileResult ProfileCompression(TimeValuePair[] data, CompressionParams parameters)
    {
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var result = CurveCompressor.Compress(data, parameters);
        
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        
        return new ProfileResult
        {
            CompressionTime = stopwatch.Elapsed,
            MemoryUsed = finalMemory - initialMemory,
            CompressionRatio = result.compressionRatio,
            MaxError = result.maxError
        };
    }
}
```

## ライブラリの拡張

### 1. 新しいアルゴリズムの追加

#### カスタムアルゴリズムの実装
```csharp
public static class CustomCompressionAlgorithm
{
    public static CompressedCurveData CompressWithWavelets(TimeValuePair[] points, CompressionParams parameters)
    {
        // 入力検証
        ValidationUtils.ValidatePoints(points, nameof(points));
        ValidationUtils.ValidateCompressionParams(parameters);
        
        // ウェーブレット変換による圧縮
        var waveletCoefficients = WaveletTransform(points);
        var compressedCoefficients = CompressCoefficients(waveletCoefficients, parameters.tolerance);
        var reconstructedPoints = InverseWaveletTransform(compressedCoefficients);
        
        // セグメント化
        var segments = ConvertToSegments(reconstructedPoints, CurveType.BSpline);
        return new CompressedCurveData(segments);
    }
}
```

#### アルゴリズムの登録と統合
```csharp
// 新しい圧縮手法の列挙型追加
public enum CompressionMethod
{
    // 既存の手法...
    Wavelet_Direct,     // 新しい手法
    Custom_Algorithm    // カスタムアルゴリズム
}

// HybridCompressorの拡張
public static CompressedCurveData CompressWithCustom(TimeValuePair[] points, CompressionParams parameters)
{
    return parameters.compressionMethod switch
    {
        // 既存の処理...
        CompressionMethod.Wavelet_Direct => CustomCompressionAlgorithm.CompressWithWavelets(points, parameters),
        _ => throw new NotSupportedException($"Unsupported compression method: {parameters.compressionMethod}")
    };
}
```

### 2. カスタムデータ型の対応

#### 3Dベクターデータの圧縮
```csharp
public struct Vector3TimePair : IComparable<Vector3TimePair>
{
    public float time;
    public Vector3 value;
    
    public int CompareTo(Vector3TimePair other) => time.CompareTo(other.time);
}

public static class Vector3Compression
{
    public static CompressedVector3CurveData Compress(Vector3TimePair[] data, CompressionParams parameters)
    {
        // X, Y, Z成分を個別に圧縮
        var xData = data.Select(p => new TimeValuePair(p.time, p.value.x)).ToArray();
        var yData = data.Select(p => new TimeValuePair(p.time, p.value.y)).ToArray();
        var zData = data.Select(p => new TimeValuePair(p.time, p.value.z)).ToArray();
        
        var xCompressed = CurveCompressor.Compress(xData, parameters);
        var yCompressed = CurveCompressor.Compress(yData, parameters);
        var zCompressed = CurveCompressor.Compress(zData, parameters);
        
        return new CompressedVector3CurveData(xCompressed.compressedCurve, yCompressed.compressedCurve, zCompressed.compressedCurve);
    }
}
```

このガイドにより、CurveCompressionライブラリを効果的に活用し、必要に応じて拡張することができます。
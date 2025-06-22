# 使用例集

## 概要

CurveCompressionライブラリの実用的なコード例を用途別に整理して提供します。コピー&ペーストで使用できる完全なコード例を含みます。

## 基本的な使用例

### 1. シンプルな圧縮

#### 最小限のコード
```csharp
using CurveCompression.Core;
using CurveCompression.DataStructures;

public class BasicCompressionExample : MonoBehaviour
{
    void Start()
    {
        // テストデータ生成
        var data = GenerateSineWave(100, 10f);
        
        // 圧縮実行
        var result = CurveCompressor.Compress(data, 0.01f);
        
        // 結果表示
        Debug.Log($"圧縮率: {result.compressionRatio:F3}");
        Debug.Log($"最大誤差: {result.maxError:F6}");
    }
    
    private TimeValuePair[] GenerateSineWave(int pointCount, float duration)
    {
        var points = new TimeValuePair[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float time = (float)i / (pointCount - 1) * duration;
            float value = Mathf.Sin(time * 2f * Mathf.PI);
            points[i] = new TimeValuePair(time, value);
        }
        return points;
    }
}
```

### 2. パラメータを使った詳細設定

#### アニメーションデータの圧縮
```csharp
public class AnimationCompressionExample : MonoBehaviour
{
    [SerializeField] private float compressionTolerance = 0.005f;
    [SerializeField] private CompressionMethod method = CompressionMethod.Bezier_Direct;
    
    public CompressionResult CompressAnimationData(TimeValuePair[] animationData)
    {
        var parameters = new CompressionParams
        {
            tolerance = compressionTolerance,
            compressionMethod = method,
            dataType = CompressionDataType.Animation,
            importanceThreshold = 1.2f,
            importanceWeights = ImportanceWeights.ForAnimation
        };
        
        return CurveCompressor.Compress(animationData, parameters);
    }
}
```

#### センサーデータの圧縮
```csharp
public class SensorDataCompressionExample
{
    public CompressionResult CompressSensorData(TimeValuePair[] sensorReadings)
    {
        var parameters = new CompressionParams
        {
            tolerance = 0.001f,                              // 高精度
            compressionMethod = CompressionMethod.RDP_Linear, // ノイズ除去重視
            dataType = CompressionDataType.SensorData,
            importanceThreshold = 2.0f,                      // 重要点強調
            importanceWeights = ImportanceWeights.ForSensorData
        };
        
        return CurveCompressor.Compress(sensorReadings, parameters);
    }
}
```

## Unity統合例

### 1. AnimationCurve圧縮

#### AnimationCurveの最適化
```csharp
using CurveCompression.Core;

public class AnimationCurveOptimizer : MonoBehaviour
{
    [SerializeField] private AnimationCurve originalCurve;
    [SerializeField] private float compressionTolerance = 0.01f;
    [SerializeField] private bool showComparison = true;
    
    private AnimationCurve optimizedCurve;
    
    [ContextMenu("Optimize Animation Curve")]
    public void OptimizeAnimationCurve()
    {
        // 1. AnimationCurveをTimeValuePairに変換
        var timeValuePairs = UnityCompressionUtils.FromAnimationCurve(originalCurve, 1000);
        
        // 2. 圧縮実行
        var result = CurveCompressor.Compress(timeValuePairs, compressionTolerance);
        
        // 3. 最適化されたAnimationCurveを生成
        optimizedCurve = UnityCompressionUtils.ToAnimationCurve(result.compressedCurve);
        
        // 4. 結果レポート
        Debug.Log($"元のキーフレーム数: {originalCurve.length}");
        Debug.Log($"最適化後のセグメント数: {result.compressedCount}");
        Debug.Log($"圧縮率: {result.compressionRatio:F3}");
        Debug.Log($"最大誤差: {result.maxError:F6}");
        
        if (showComparison)
        {
            ShowCurveComparison();
        }
    }
    
    private void ShowCurveComparison()
    {
        // 比較用のサンプリング
        int sampleCount = 100;
        float duration = originalCurve.keys[originalCurve.length - 1].time;
        
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / (sampleCount - 1) * duration;
            float originalValue = originalCurve.Evaluate(time);
            float optimizedValue = optimizedCurve.Evaluate(time);
            float error = Mathf.Abs(originalValue - optimizedValue);
            
            if (error > compressionTolerance * 2) // 大きな誤差を警告
            {
                Debug.LogWarning($"時刻 {time:F2} で大きな誤差: {error:F6}");
            }
        }
    }
}
```

### 2. リアルタイムデータストリーミング

#### リアルタイム圧縮システム
```csharp
public class RealtimeCompressionSystem : MonoBehaviour
{
    [System.Serializable]
    public class CompressionSettings
    {
        public float tolerance = 0.01f;
        public CompressionMethod method = CompressionMethod.RDP_Linear;
        public int bufferSize = 100;
        public float updateInterval = 0.1f;
    }
    
    [SerializeField] private CompressionSettings settings;
    [SerializeField] private bool enableCompression = true;
    
    private Queue<TimeValuePair> dataBuffer = new Queue<TimeValuePair>();
    private List<TimeValuePair> compressionBuffer = new List<TimeValuePair>();
    private float lastUpdateTime;
    
    public UnityEvent<CompressionResult> OnCompressionCompleted;
    
    void Update()
    {
        // 模擬データの追加
        AddDataPoint(Time.time, GenerateDataValue());
        
        // 定期的な圧縮実行
        if (enableCompression && Time.time - lastUpdateTime >= settings.updateInterval)
        {
            PerformCompression();
            lastUpdateTime = Time.time;
        }
    }
    
    public void AddDataPoint(float time, float value)
    {
        dataBuffer.Enqueue(new TimeValuePair(time, value));
        
        // バッファサイズ制限
        while (dataBuffer.Count > settings.bufferSize)
        {
            dataBuffer.Dequeue();
        }
    }
    
    private void PerformCompression()
    {
        if (dataBuffer.Count < 2) return;
        
        // バッファからデータを取得
        compressionBuffer.Clear();
        compressionBuffer.AddRange(dataBuffer);
        
        // 圧縮実行
        var dataArray = compressionBuffer.ToArray();
        var result = CurveCompressor.Compress(dataArray, settings.tolerance);
        
        // イベント発火
        OnCompressionCompleted?.Invoke(result);
        
        // デバッグ情報
        Debug.Log($"リアルタイム圧縮: {dataArray.Length} → {result.compressedCount} ポイント");
    }
    
    private float GenerateDataValue()
    {
        // 模擬センサーデータ
        return Mathf.Sin(Time.time * 2f) + 0.1f * Random.Range(-1f, 1f);
    }
}
```

### 3. エディター拡張

#### カスタムエディターツール
```csharp
#if UNITY_EDITOR
using UnityEditor;
using CurveCompression.Core;

[CustomEditor(typeof(AnimationCurveOptimizer))]
public class AnimationCurveOptimizerEditor : Editor
{
    private AnimationCurveOptimizer optimizer;
    private CompressionResult lastResult;
    
    void OnEnable()
    {
        optimizer = (AnimationCurveOptimizer)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("圧縮ツール", EditorStyles.boldLabel);
        
        if (GUILayout.Button("AnimationCurveを最適化"))
        {
            optimizer.OptimizeAnimationCurve();
            // 結果を取得（実際の実装では公開プロパティが必要）
            // lastResult = optimizer.LastCompressionResult;
        }
        
        if (lastResult != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("最適化結果", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"圧縮率: {lastResult.compressionRatio:F3}");
            EditorGUILayout.LabelField($"最大誤差: {lastResult.maxError:F6}");
            EditorGUILayout.LabelField($"平均誤差: {lastResult.avgError:F6}");
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("テストデータで検証"))
        {
            RunCompressionTest();
        }
    }
    
    private void RunCompressionTest()
    {
        // 各アルゴリズムでテスト
        var testData = GenerateTestData();
        var methods = System.Enum.GetValues(typeof(CompressionMethod));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("圧縮アルゴリズム比較", EditorStyles.boldLabel);
        
        foreach (CompressionMethod method in methods)
        {
            var parameters = new CompressionParams
            {
                tolerance = 0.01f,
                compressionMethod = method
            };
            
            var result = CurveCompressor.Compress(testData, parameters);
            Debug.Log($"{method}: 圧縮率 {result.compressionRatio:F3}, 最大誤差 {result.maxError:F6}");
        }
    }
    
    private TimeValuePair[] GenerateTestData()
    {
        var data = new TimeValuePair[200];
        for (int i = 0; i < data.Length; i++)
        {
            float time = (float)i / (data.Length - 1) * 10f;
            float value = Mathf.Sin(time * 2f) + 0.3f * Mathf.Sin(time * 10f);
            data[i] = new TimeValuePair(time, value);
        }
        return data;
    }
}
#endif
```

## 高度な使用例

### 1. 自動最適化システム

#### 自動パラメータ調整
```csharp
public class AutoOptimizationSystem
{
    public struct OptimizationResult
    {
        public CompressionParams bestParameters;
        public CompressionResult compressionResult;
        public float qualityScore;
    }
    
    public OptimizationResult FindOptimalParameters(TimeValuePair[] data, float targetCompressionRatio)
    {
        var bestResult = new OptimizationResult { qualityScore = float.MinValue };
        
        // パラメータ範囲の定義
        var toleranceRange = new[] { 0.001f, 0.005f, 0.01f, 0.02f, 0.05f };
        var methods = System.Enum.GetValues(typeof(CompressionMethod)).Cast<CompressionMethod>();
        
        foreach (var tolerance in toleranceRange)
        {
            foreach (var method in methods)
            {
                var parameters = new CompressionParams
                {
                    tolerance = tolerance,
                    compressionMethod = method,
                    dataType = ClassifyDataType(data)
                };
                
                try
                {
                    var result = CurveCompressor.Compress(data, parameters);
                    float score = CalculateQualityScore(result, targetCompressionRatio);
                    
                    if (score > bestResult.qualityScore)
                    {
                        bestResult = new OptimizationResult
                        {
                            bestParameters = parameters,
                            compressionResult = result,
                            qualityScore = score
                        };
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"圧縮失敗 {method} (tolerance: {tolerance}): {ex.Message}");
                }
            }
        }
        
        return bestResult;
    }
    
    private CompressionDataType ClassifyDataType(TimeValuePair[] data)
    {
        // データの特性を分析してタイプを判定
        float variance = CalculateVariance(data);
        float smoothness = CalculateSmoothness(data);
        
        if (smoothness > 0.8f)
            return CompressionDataType.Animation;
        else if (variance > 0.1f)
            return CompressionDataType.SensorData;
        else
            return CompressionDataType.FinancialData;
    }
    
    private float CalculateQualityScore(CompressionResult result, float targetRatio)
    {
        // 圧縮率と精度のバランススコア
        float ratioScore = 1f - Mathf.Abs(result.compressionRatio - targetRatio);
        float accuracyScore = 1f / (1f + result.maxError * 100f);
        
        return ratioScore * 0.6f + accuracyScore * 0.4f;
    }
}
```

### 2. バッチ処理システム

#### 大量ファイルの一括圧縮
```csharp
using System.Threading.Tasks;
using System.Collections.Concurrent;

public class BatchCompressionProcessor
{
    public struct BatchJob
    {
        public string inputPath;
        public string outputPath;
        public CompressionParams parameters;
    }
    
    public struct BatchResult
    {
        public string filePath;
        public bool success;
        public CompressionResult result;
        public string errorMessage;
    }
    
    public async Task<BatchResult[]> ProcessBatchAsync(BatchJob[] jobs, int maxConcurrency = 4)
    {
        var results = new ConcurrentBag<BatchResult>();
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        
        var tasks = jobs.Select(async job =>
        {
            await semaphore.WaitAsync();
            try
            {
                var result = await ProcessSingleFileAsync(job);
                results.Add(result);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        return results.ToArray();
    }
    
    private async Task<BatchResult> ProcessSingleFileAsync(BatchJob job)
    {
        try
        {
            // ファイル読み込み
            var data = await LoadTimeValuePairsAsync(job.inputPath);
            
            // 圧縮実行
            var result = CurveCompressor.Compress(data, job.parameters);
            
            // 結果保存
            await SaveCompressionResultAsync(job.outputPath, result);
            
            return new BatchResult
            {
                filePath = job.inputPath,
                success = true,
                result = result,
                errorMessage = null
            };
        }
        catch (System.Exception ex)
        {
            return new BatchResult
            {
                filePath = job.inputPath,
                success = false,
                result = default,
                errorMessage = ex.Message
            };
        }
    }
    
    private async Task<TimeValuePair[]> LoadTimeValuePairsAsync(string filePath)
    {
        // CSVファイルからの読み込み例
        var lines = await File.ReadAllLinesAsync(filePath);
        var data = new List<TimeValuePair>();
        
        for (int i = 1; i < lines.Length; i++) // ヘッダースキップ
        {
            var parts = lines[i].Split(',');
            if (parts.Length >= 2 && 
                float.TryParse(parts[0], out float time) && 
                float.TryParse(parts[1], out float value))
            {
                data.Add(new TimeValuePair(time, value));
            }
        }
        
        return data.ToArray();
    }
}
```

### 3. 品質保証システム

#### 圧縮品質の自動検証
```csharp
public class CompressionQualityAssurance
{
    public struct QualityReport
    {
        public bool passedAllTests;
        public float overallScore;
        public Dictionary<string, TestResult> testResults;
    }
    
    public struct TestResult
    {
        public bool passed;
        public float score;
        public string message;
    }
    
    public QualityReport RunQualityTests(TimeValuePair[] originalData, CompressionResult result)
    {
        var tests = new Dictionary<string, TestResult>();
        
        // テスト1: 最大誤差チェック
        tests["MaxError"] = TestMaxError(originalData, result);
        
        // テスト2: 特徴保持チェック
        tests["FeaturePreservation"] = TestFeaturePreservation(originalData, result);
        
        // テスト3: 滑らかさチェック
        tests["Smoothness"] = TestSmoothness(result);
        
        // テスト4: 圧縮効率チェック
        tests["CompressionEfficiency"] = TestCompressionEfficiency(result);
        
        // 総合評価
        bool allPassed = tests.Values.All(t => t.passed);
        float overallScore = tests.Values.Average(t => t.score);
        
        return new QualityReport
        {
            passedAllTests = allPassed,
            overallScore = overallScore,
            testResults = tests
        };
    }
    
    private TestResult TestMaxError(TimeValuePair[] original, CompressionResult result)
    {
        float threshold = 0.05f; // 5%の誤差まで許容
        bool passed = result.maxError <= threshold;
        float score = Mathf.Clamp01(1f - result.maxError / threshold);
        
        return new TestResult
        {
            passed = passed,
            score = score,
            message = $"最大誤差: {result.maxError:F6} (閾値: {threshold:F6})"
        };
    }
    
    private TestResult TestFeaturePreservation(TimeValuePair[] original, CompressionResult result)
    {
        // 極値（ピーク）の保持度をチェック
        var originalPeaks = FindPeaks(original);
        var compressedData = result.compressedCurve.ToTimeValuePairs(original.Length);
        var compressedPeaks = FindPeaks(compressedData);
        
        float preservationRatio = (float)compressedPeaks.Count / originalPeaks.Count;
        bool passed = preservationRatio >= 0.8f; // 80%以上のピーク保持
        
        return new TestResult
        {
            passed = passed,
            score = preservationRatio,
            message = $"特徴保持率: {preservationRatio:F3} ({compressedPeaks.Count}/{originalPeaks.Count} ピーク)"
        };
    }
    
    private List<int> FindPeaks(TimeValuePair[] data)
    {
        var peaks = new List<int>();
        for (int i = 1; i < data.Length - 1; i++)
        {
            if (data[i].value > data[i-1].value && data[i].value > data[i+1].value)
            {
                peaks.Add(i);
            }
        }
        return peaks;
    }
}
```

### 4. インタラクティブツール

#### Unity Editor用の圧縮比較ツール
```csharp
#if UNITY_EDITOR
using UnityEditor;

public class CompressionComparisonWindow : EditorWindow
{
    private TimeValuePair[] testData;
    private CompressionResult[] results;
    private Vector2 scrollPosition;
    private int selectedDataType = 0;
    private readonly string[] dataTypeNames = { "Sine Wave", "Noise", "Step Function", "Complex" };
    
    [MenuItem("Tools/Curve Compression/Comparison Tool")]
    public static void ShowWindow()
    {
        GetWindow<CompressionComparisonWindow>("圧縮比較ツール");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("圧縮アルゴリズム比較ツール", EditorStyles.boldLabel);
        
        // テストデータ選択
        EditorGUILayout.Space();
        selectedDataType = EditorGUILayout.Popup("テストデータ", selectedDataType, dataTypeNames);
        
        if (GUILayout.Button("テストデータ生成"))
        {
            GenerateTestData();
        }
        
        if (testData != null)
        {
            EditorGUILayout.LabelField($"データ点数: {testData.Length}");
            
            if (GUILayout.Button("全アルゴリズムでテスト実行"))
            {
                RunAllCompressionTests();
            }
            
            // 結果表示
            if (results != null)
            {
                DisplayResults();
            }
        }
    }
    
    private void GenerateTestData()
    {
        const int pointCount = 1000;
        testData = new TimeValuePair[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float time = (float)i / (pointCount - 1) * 10f;
            float value = selectedDataType switch
            {
                0 => Mathf.Sin(time * 2f), // Sine Wave
                1 => Random.Range(-1f, 1f), // Noise
                2 => Mathf.Floor(time) % 2, // Step Function
                3 => Mathf.Sin(time * 2f) + 0.3f * Mathf.Sin(time * 10f) + 0.1f * Random.Range(-1f, 1f), // Complex
                _ => 0f
            };
            testData[i] = new TimeValuePair(time, value);
        }
    }
    
    private void RunAllCompressionTests()
    {
        var methods = System.Enum.GetValues(typeof(CompressionMethod)).Cast<CompressionMethod>().ToArray();
        results = new CompressionResult[methods.Length];
        
        for (int i = 0; i < methods.Length; i++)
        {
            var parameters = new CompressionParams
            {
                tolerance = 0.01f,
                compressionMethod = methods[i]
            };
            
            try
            {
                results[i] = CurveCompressor.Compress(testData, parameters);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"圧縮失敗 {methods[i]}: {ex.Message}");
            }
        }
    }
    
    private void DisplayResults()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("圧縮結果", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        var methods = System.Enum.GetValues(typeof(CompressionMethod)).Cast<CompressionMethod>().ToArray();
        
        for (int i = 0; i < results.Length && i < methods.Length; i++)
        {
            if (results[i] != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(methods[i].ToString(), EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"圧縮率: {results[i].compressionRatio:F3}");
                EditorGUILayout.LabelField($"最大誤差: {results[i].maxError:F6}");
                EditorGUILayout.LabelField($"平均誤差: {results[i].avgError:F6}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
}
#endif
```

これらの使用例により、CurveCompressionライブラリの多様な活用方法を学ぶことができます。コードはそのまま使用するか、プロジェクトの要件に応じてカスタマイズしてください。
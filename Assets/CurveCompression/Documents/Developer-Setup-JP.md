# 開発者セットアップガイド

## 概要

このガイドでは、CurveCompressionライブラリの拡張、カスタマイズ、維持のための開発環境のセットアップと手順について説明します。

## 開発環境要件

### 必須要件
- **Unity 2022.3 LTS以上**: プロジェクトの基本要件
- **.NET Standard 2.1**: C#言語機能の互換性
- **Visual Studio 2022またはJetBrains Rider**: 推奨IDE
- **Git**: バージョン管理

### 推奨ツール
- **Unity Test Framework**: 単体テストとインテグレーションテスト
- **Unity Profiler**: パフォーマンス分析
- **Unity Memory Profiler**: メモリ使用量分析
- **DocFX**: ドキュメント生成（オプション）

## プロジェクト構造理解

### アセンブリ定義
```json
{
    "name": "CurveCompression",
    "rootNamespace": "CurveCompression",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 名前空間マッピング
```
CurveCompression.Core           → Scripts/Core/
CurveCompression.DataStructures → Scripts/DataStructures/
CurveCompression.Algorithms     → Scripts/Algorithms/
CurveCompression.Visualization  → Scripts/Visualization/
```

## 開発ワークフロー

### 1. 新機能開発

#### 新しいアルゴリズム追加の手順
1. **アルゴリズムクラス作成**
   ```csharp
   namespace CurveCompression.Algorithms
   {
       public static class MyNewAlgorithm
       {
           public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
           {
               // 実装
           }
           
           public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
           {
               var params = new CompressionParams { tolerance = tolerance };
               return Compress(points, params);
           }
       }
   }
   ```

2. **列挙型更新**
   ```csharp
   // DataStructures/CompressionParams.cs
   public enum CompressionMethod
   {
       // 既存メソッド...
       MyNew_Algorithm
   }
   ```

3. **ルーティング更新**
   ```csharp
   // Core/HybridCompressor.cs
   CompressionMethod.MyNew_Algorithm => MyNewAlgorithm.Compress(points, parameters)
   ```

4. **選択システム統合**
   ```csharp
   // Core/AlgorithmSelector.cs
   scores[CompressionMethod.MyNew_Algorithm] = CalculateMyNewAlgorithmScore(analysis);
   ```

### 2. テスト戦略

#### 単体テスト作成
```csharp
[TestFixture]
public class MyNewAlgorithmTests
{
    [Test]
    public void Compress_ValidInput_ReturnsCompressedData()
    {
        // Arrange
        var testData = GenerateTestData();
        var tolerance = 0.01f;
        
        // Act
        var result = MyNewAlgorithm.Compress(testData, tolerance);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.Greater(result.segments.Length, 0);
        Assert.LessOrEqual(result.segments.Length, testData.Length);
    }
    
    [Test]
    public void Compress_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            MyNewAlgorithm.Compress(null, 0.01f));
    }
    
    private TimeValuePair[] GenerateTestData()
    {
        var data = new TimeValuePair[100];
        for (int i = 0; i < data.Length; i++)
        {
            float time = (float)i / 99f * 10f;
            float value = Mathf.Sin(time * 2f * Mathf.PI);
            data[i] = new TimeValuePair(time, value);
        }
        return data;
    }
}
```

#### パフォーマンステスト
```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    [Performance]
    public void Compress_LargeDataset_PerformsWithinTimeLimit()
    {
        // Arrange
        var largeDataset = GenerateLargeDataset(10000);
        var tolerance = 0.01f;
        
        // Act
        using (Measure.Scope())
        {
            var result = CurveCompressor.Compress(largeDataset, tolerance);
        }
        
        // Assert - Unity Performance Testing framework will handle timing assertions
    }
    
    private TimeValuePair[] GenerateLargeDataset(int size)
    {
        var data = new TimeValuePair[size];
        var random = new System.Random(42); // 確定的シード
        
        for (int i = 0; i < size; i++)
        {
            float time = (float)i / (size - 1) * 100f;
            float value = Mathf.Sin(time * 0.1f) + 0.1f * (float)random.NextDouble();
            data[i] = new TimeValuePair(time, value);
        }
        return data;
    }
}
```

### 3. デバッグとプロファイリング

#### デバッグユーティリティ
```csharp
public static class CompressionDebugUtils
{
    public static void LogAlgorithmPerformance(TimeValuePair[] data, CompressionParams parameters)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        
        var result = CurveCompressor.Compress(data, parameters);
        
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        
        Debug.Log($"Algorithm: {parameters.compressionMethod}");
        Debug.Log($"Time: {stopwatch.ElapsedMilliseconds}ms");
        Debug.Log($"Memory: {finalMemory - initialMemory} bytes");
        Debug.Log($"Compression Ratio: {result.compressionRatio:F3}");
        Debug.Log($"Max Error: {result.maxError:F6}");
    }
    
    public static void VisualizeCompressionQuality(TimeValuePair[] original, CompressionResult result)
    {
        // Unity Editorでの可視化ロジック
        if (Application.isEditor)
        {
            var compressedData = result.compressedCurve.ToTimeValuePairs(original.Length);
            
            for (int i = 0; i < original.Length; i++)
            {
                float error = Mathf.Abs(original[i].value - compressedData[i].value);
                if (error > result.avgError * 2)
                {
                    Debug.LogWarning($"High error at index {i}: {error:F6}");
                }
            }
        }
    }
}
```

## カスタマイゼーションガイド

### 1. カスタムデータタイプ

#### 新しいデータ分類の追加
```csharp
// DataStructures/CompressionParams.cs
public enum CompressionDataType
{
    Animation,
    SensorData,
    FinancialData,
    AudioData,      // 新しいタイプ
    GameplayData    // 新しいタイプ
}

// Core/AlgorithmSelector.cs
private static CompressionDataType ClassifyDataType(DataAnalysis analysis)
{
    // オーディオデータ検出
    if (analysis.temporalDensity > 1000f && analysis.noiseLevel < 0.05f)
        return CompressionDataType.AudioData;
    
    // ゲームプレイデータ検出
    if (analysis.complexity > 0.8f && analysis.variability > 1.0f)
        return CompressionDataType.GameplayData;
    
    // 既存の分類ロジック...
}
```

#### カスタム重要度重み
```csharp
// DataStructures/ImportanceWeights.cs
public static ImportanceWeights ForAudioData => new ImportanceWeights
{
    curvatureWeight = 1.5f,
    velocityWeight = 3.0f,      // オーディオの周波数変化を重視
    accelerationWeight = 2.0f,
    temporalWeight = 1.0f
};

public static ImportanceWeights ForGameplayData => new ImportanceWeights
{
    curvatureWeight = 2.5f,     // ゲームプレイの急激な変化を重視
    velocityWeight = 1.0f,
    accelerationWeight = 3.0f,  // 加速度変化を最重視
    temporalWeight = 0.5f
};
```

### 2. カスタムメトリクス

#### 新しい品質指標の追加
```csharp
// DataStructures/CompressionResult.cs
public class CompressionResult
{
    // 既存メトリクス...
    public float spectralFidelity;    // 周波数ドメイン忠実度
    public float peakPreservation;    // ピーク保持率
    public float smoothnessIndex;     // 滑らかさ指標
    
    private void CalculateExtendedMetrics(TimeValuePair[] original, CompressedCurveData compressed)
    {
        spectralFidelity = CalculateSpectralFidelity(original, compressed);
        peakPreservation = CalculatePeakPreservation(original, compressed);
        smoothnessIndex = CalculateSmoothness(compressed);
    }
    
    private float CalculateSpectralFidelity(TimeValuePair[] original, CompressedCurveData compressed)
    {
        // FFTベースの周波数分析
        var originalFFT = PerformFFT(original);
        var compressedSamples = compressed.ToTimeValuePairs(original.Length);
        var compressedFFT = PerformFFT(compressedSamples);
        
        // 周波数ドメインでの類似度計算
        float totalPower = 0f;
        float errorPower = 0f;
        
        for (int i = 0; i < originalFFT.Length; i++)
        {
            float originalMagnitude = originalFFT[i].magnitude;
            float compressedMagnitude = compressedFFT[i].magnitude;
            
            totalPower += originalMagnitude * originalMagnitude;
            errorPower += (originalMagnitude - compressedMagnitude) * (originalMagnitude - compressedMagnitude);
        }
        
        return 1.0f - (errorPower / (totalPower + 0.0001f));
    }
}
```

### 3. カスタム曲線タイプ

#### 新しい曲線評価の実装
```csharp
// DataStructures/CurveSegment.cs
public enum CurveType
{
    Linear,
    BSpline,
    Bezier,
    CatmullRom,    // 新しいタイプ
    NURBS          // 新しいタイプ
}

public float Evaluate(float time)
{
    float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
    
    return curveType switch
    {
        CurveType.Linear => EvaluateLinear(t),
        CurveType.Bezier => EvaluateBezier(t),
        CurveType.BSpline => EvaluateBSpline(t),
        CurveType.CatmullRom => EvaluateCatmullRom(t),
        CurveType.NURBS => EvaluateNURBS(t),
        _ => Mathf.Lerp(startValue, endValue, t)
    };
}

private float EvaluateCatmullRom(float t)
{
    // Catmull-Rom スプライン実装
    if (bsplineControlPoints == null || bsplineControlPoints.Length < 4)
        return Mathf.Lerp(startValue, endValue, t);
    
    // 4つの制御点を使用したCatmull-Rom補間
    Vector2 p0 = bsplineControlPoints[0];
    Vector2 p1 = bsplineControlPoints[1];
    Vector2 p2 = bsplineControlPoints[2];
    Vector2 p3 = bsplineControlPoints[3];
    
    float t2 = t * t;
    float t3 = t2 * t;
    
    return 0.5f * (
        (2 * p1.y) +
        (-p0.y + p2.y) * t +
        (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * t2 +
        (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * t3
    );
}
```

## エディター拡張

### 1. カスタムPropertyDrawer

#### CompressionParams用のカスタムInspector
```csharp
#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(CompressionParams))]
public class CompressionParamsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        // 許容誤差フィールド
        var toleranceRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var toleranceProp = property.FindPropertyRelative("_tolerance");
        EditorGUI.PropertyField(toleranceRect, toleranceProp, new GUIContent("Tolerance"));
        
        // アルゴリズム選択
        var methodRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
        var methodProp = property.FindPropertyRelative("compressionMethod");
        EditorGUI.PropertyField(methodRect, methodProp, new GUIContent("Method"));
        
        // 自動設定ボタン
        var buttonRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + 2) * 2, position.width, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(buttonRect, "Auto Configure"))
        {
            AutoConfigureParams(property);
        }
        
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + 2) * 3;
    }
    
    private void AutoConfigureParams(SerializedProperty property)
    {
        // 自動設定ロジック
        var toleranceProp = property.FindPropertyRelative("_tolerance");
        var methodProp = property.FindPropertyRelative("compressionMethod");
        
        toleranceProp.floatValue = 0.01f;
        methodProp.enumValueIndex = (int)CompressionMethod.Bezier_Direct;
        
        property.serializedObject.ApplyModifiedProperties();
    }
}
#endif
```

### 2. エディターウィンドウツール

#### 圧縮分析ツール
```csharp
#if UNITY_EDITOR
public class CompressionAnalysisWindow : EditorWindow
{
    private TimeValuePair[] testData;
    private CompressionParams parameters = new CompressionParams();
    private CompressionResult lastResult;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Curve Compression/Analysis Window")]
    public static void ShowWindow()
    {
        GetWindow<CompressionAnalysisWindow>("Compression Analysis");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("圧縮分析ツール", EditorStyles.boldLabel);
        
        // テストデータ生成
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Test Data"))
        {
            GenerateTestData();
        }
        
        if (testData != null)
        {
            EditorGUILayout.LabelField($"Data Points: {testData.Length}");
            
            // パラメータ設定
            EditorGUI.BeginChangeCheck();
            parameters.tolerance = EditorGUILayout.FloatField("Tolerance", parameters.tolerance);
            parameters.compressionMethod = (CompressionMethod)EditorGUILayout.EnumPopup("Method", parameters.compressionMethod);
            
            if (EditorGUI.EndChangeCheck() || GUILayout.Button("Analyze"))
            {
                PerformAnalysis();
            }
            
            // 結果表示
            if (lastResult != null)
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
            float value = Mathf.Sin(time * 2f) + 0.1f * Mathf.Sin(time * 20f);
            testData[i] = new TimeValuePair(time, value);
        }
    }
    
    private void PerformAnalysis()
    {
        if (testData == null) return;
        
        lastResult = CurveCompressor.Compress(testData, parameters);
        
        // データ分析も実行
        var analysis = CurveCompressor.AnalyzeData(testData);
        var recommendation = CurveCompressor.GetAlgorithmRecommendation(testData, parameters);
        
        Debug.Log($"Data Analysis - Smoothness: {analysis.smoothness:F3}, Complexity: {analysis.complexity:F3}");
        Debug.Log($"Recommendation: {recommendation.primaryMethod} (Confidence: {recommendation.confidence:F3})");
    }
    
    private void DisplayResults()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Analysis Results", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField($"Compression Ratio: {lastResult.compressionRatio:F3}");
        EditorGUILayout.LabelField($"Max Error: {lastResult.maxError:F6}");
        EditorGUILayout.LabelField($"Avg Error: {lastResult.avgError:F6}");
        EditorGUILayout.LabelField($"Original Count: {lastResult.originalCount}");
        EditorGUILayout.LabelField($"Compressed Count: {lastResult.compressedCount}");
        
        if (lastResult.compressionTime != TimeSpan.Zero)
        {
            EditorGUILayout.LabelField($"Compression Time: {lastResult.compressionTime.TotalMilliseconds:F2}ms");
        }
        
        EditorGUILayout.EndScrollView();
    }
}
#endif
```

## ベストプラクティス

### 1. コーディング標準

#### 命名規則
- **クラス**: PascalCase (例: `AlgorithmSelector`)
- **メソッド**: PascalCase (例: `CalculateError`)
- **プロパティ**: PascalCase (例: `CompressionRatio`)
- **フィールド**: camelCase with underscore prefix for private (例: `_tolerance`)
- **定数**: UPPER_SNAKE_CASE (例: `MAX_ITERATIONS`)

#### ドキュメント
```csharp
/// <summary>
/// データを指定された許容誤差で圧縮します。
/// </summary>
/// <param name="points">圧縮するデータポイント</param>
/// <param name="tolerance">許容誤差（正の値）</param>
/// <returns>圧縮結果とメトリクス</returns>
/// <exception cref="ArgumentNullException">pointsがnullの場合</exception>
/// <exception cref="ArgumentOutOfRangeException">toleranceが負または0の場合</exception>
public static CompressionResult Compress(TimeValuePair[] points, float tolerance)
{
    // 実装
}
```

### 2. エラーハンドリング

#### 例外処理パターン
```csharp
public static CompressionResult SafeCompress(TimeValuePair[] points, CompressionParams parameters)
{
    try
    {
        // 入力検証
        ValidationUtils.ValidatePoints(points, nameof(points));
        ValidationUtils.ValidateCompressionParams(parameters);
        
        // メイン処理
        return CurveCompressor.Compress(points, parameters);
    }
    catch (ArgumentException ex)
    {
        Debug.LogError($"Invalid input for compression: {ex.Message}");
        return CreateFallbackResult(points);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Unexpected error during compression: {ex.Message}");
        return CreateFallbackResult(points);
    }
}

private static CompressionResult CreateFallbackResult(TimeValuePair[] points)
{
    // 最小限の線形近似でフォールバック
    var fallbackData = new[] { points[0], points[points.Length - 1] };
    return new CompressionResult(points, fallbackData);
}
```

### 3. パフォーマンス考慮

#### メモリ効率のパターン
```csharp
// 良い例: 一時的な割り当てを避ける
public static void ProcessDataInPlace(ref TimeValuePair[] data)
{
    // インプレース操作
    for (int i = 0; i < data.Length; i++)
    {
        data[i] = new TimeValuePair(data[i].time, ProcessValue(data[i].value));
    }
}

// 避けるべき例: 不必要な配列作成
public static TimeValuePair[] ProcessDataCopy(TimeValuePair[] data)
{
    var result = new TimeValuePair[data.Length]; // 不必要なコピー
    for (int i = 0; i < data.Length; i++)
    {
        result[i] = new TimeValuePair(data[i].time, ProcessValue(data[i].value));
    }
    return result;
}
```

このガイドに従うことで、CurveCompressionライブラリを効率的に拡張し、高品質な追加機能を開発できます。
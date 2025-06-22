using UnityEngine;
using System.Collections.Generic;
using CurveCompression.DataStructures;
using CurveCompression.Algorithms;
using CurveCompression.Core;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace CurveCompression.Test
{
    // =============================================================================
    // CurveCompressor.cs - メイン処理クラス
    // =============================================================================
    
	/// <summary>
    /// カーブ圧縮のテストクラス（レガシー）
    /// 注: 新しい実装では CurveCompressionDemo を使用してください
    /// </summary>
    [System.Obsolete("Use CurveCompressionDemo instead for new implementations")]
    public class CurveCompressor : MonoBehaviour
    {
        [Header("圧縮パラメータ")]
        [SerializeField] private CompressionParams compressionParams = new CompressionParams();
        
        [Header("テスト用データ")]
        [SerializeField] private int testDataPoints = 1000;
        [SerializeField] private float testDuration = 10.0f;
        [SerializeField] private bool generateTestData = true;
        
        [Header("可視化設定")]
        [SerializeField] private bool enableVisualization = true;
        [SerializeField] private bool useAdvancedCompression = true;
        [SerializeField] private bool compareAllMethods = false; // 全手法比較テスト
        [SerializeField] private Color originalDataColor = Color.blue;
        [SerializeField] private Color compressedDataColor = Color.red;
        [SerializeField] private Color errorVisualizationColor = Color.yellow;
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private float curveHeight = 2.0f;
        [SerializeField] private float timeScale = 1.0f;
        [SerializeField] private float compressedDataYOffset = 0f; // 圧縮データのYオフセット
        
        [Header("AnimationClip保存設定")]
        [SerializeField] private bool saveAsAnimationClip = true;
        [SerializeField] private string animationClipSavePath = "Assets/CurveCompression/GeneratedClips/";
        [SerializeField] private string animationClipNamePrefix = "CurveCompression_";
        [SerializeField] private string targetPropertyPath = "transform.position.y"; // アニメーション対象プロパティ
        
        [Header("Gizmo設定")]
        [SerializeField] private bool showControlPoints = true;
        [SerializeField] private Color controlPointColor = Color.green;
        [SerializeField] private float controlPointSize = 0.1f;
        [SerializeField] private bool showControlLines = true;
        [SerializeField] private Color controlLineColor = Color.gray;
        
        [Header("実行時更新設定")]
        [SerializeField] private bool autoUpdateOnParameterChange = true;
        [SerializeField] private float updateCheckInterval = 0.5f; // 秒
        
        [Header("コントロールポイント推定")]
        [SerializeField] private bool useFixedControlPoints = false;
        [SerializeField] private int fixedControlPointCount = 10;
        [SerializeField] private bool showEstimationResults = true;
        [SerializeField] private string selectedEstimationMethod = "Elbow";
        
        // 推定結果の表示用
        [System.Serializable]
        public class EstimationDisplay
        {
            public string methodName;
            public int optimalPoints;
            public float score;
            public string metrics;
        }
        [SerializeField] private List<EstimationDisplay> estimationResults = new List<EstimationDisplay>();
        
        private LineRenderer originalLineRenderer;
        private LineRenderer compressedLineRenderer;
        private LineRenderer errorLineRenderer;
        private TimeValuePair[] currentTestData;
        private CompressionResult currentResult;
        private CompressionParams lastParams;
        private float lastUpdateTime;
        private Dictionary<string, ControlPointEstimator.EstimationResult> lastEstimationResults;
        
        /// <summary>
        /// データを圧縮
        /// </summary>
        public CompressionResult CompressData(TimeValuePair[] originalData)
        {
            if (originalData == null || originalData.Length == 0)
            {
                Debug.LogWarning("圧縮対象のデータが空です");
                return null;
            }
            
            TimeValuePair[] compressedData;
            
            // 従来の圧縮手法（RDPベース）
            var weights = HybridCompressor.GetOptimalWeights(compressionParams.dataType, compressionParams.importanceWeights);
            compressedData = RDPAlgorithm.Simplify(originalData, compressionParams.tolerance, 
                compressionParams.importanceThreshold, weights);
            
            return new CompressionResult(originalData, compressedData);
        }
        
        /// <summary>
        /// データを圧縮（新しいデータ構造を使用）
        /// </summary>
        public CompressionResult CompressDataAdvanced(TimeValuePair[] originalData)
        {
            if (originalData == null || originalData.Length == 0)
            {
                Debug.LogWarning("圧縮対象のデータが空です");
                return null;
            }
            
            // 新しい統一的な圧縮手法を使用
            CompressedCurveData compressedCurve = HybridCompressor.CompressAdvanced(originalData, compressionParams);
            
            return new CompressionResult(originalData, compressedCurve);
        }
        
        /// <summary>
        /// テスト用データを生成
        /// </summary>
        public TimeValuePair[] GenerateTestData()
        {
            var data = new TimeValuePair[testDataPoints];
            
            for (int i = 0; i < testDataPoints; i++)
            {
                float time = (float)i / (testDataPoints - 1) * testDuration;
                
                // 複雑な波形を生成（複数の周波数成分）
                float value = Mathf.Sin(time * 2.0f) * 0.5f +
                             Mathf.Sin(time * 5.0f) * 0.3f +
                             Mathf.Sin(time * 10.0f) * 0.2f +
                             Mathf.PerlinNoise(time * 0.5f, 0) * 0.4f;
                
                data[i] = new TimeValuePair(time, value);
            }
            
            return data;
        }
        
        void Start()
        {
            if (enableVisualization)
            {
                InitializeVisualization();
            }
            
            if (generateTestData)
            {
                currentTestData = GenerateTestData();
                TestCompression();
                lastParams = compressionParams.Clone();
                lastUpdateTime = Time.time;
            }
        }
        
        void Update()
        {
            if (autoUpdateOnParameterChange && currentTestData != null && 
                Time.time - lastUpdateTime > updateCheckInterval)
            {
                if (!compressionParams.Equals(lastParams))
                {
                    Debug.Log("パラメータ変更を検出、圧縮を再実行中...");
                    TestCompression();
                    lastParams = compressionParams.Clone();
                }
                lastUpdateTime = Time.time;
            }
        }
        
        private void InitializeVisualization()
        {
            // 元データ用LineRenderer
            GameObject originalObject = new GameObject("OriginalData");
            originalObject.transform.SetParent(transform);
            originalLineRenderer = originalObject.AddComponent<LineRenderer>();
            SetupLineRenderer(originalLineRenderer, originalDataColor);
            
            // 圧縮データ用LineRenderer
            GameObject compressedObject = new GameObject("CompressedData");
            compressedObject.transform.SetParent(transform);
            compressedLineRenderer = compressedObject.AddComponent<LineRenderer>();
            SetupLineRenderer(compressedLineRenderer, compressedDataColor);
            
            // 誤差可視化用LineRenderer
            GameObject errorObject = new GameObject("ErrorVisualization");
            errorObject.transform.SetParent(transform);
            errorLineRenderer = errorObject.AddComponent<LineRenderer>();
            SetupLineRenderer(errorLineRenderer, errorVisualizationColor);
        }
        
        private void SetupLineRenderer(LineRenderer lineRenderer, Color color)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 1;
        }
        
        private void TestCompression()
        {
            if (currentTestData == null)
            {
                currentTestData = GenerateTestData();
            }
            
            // コントロールポイント推定を実行
            if (showEstimationResults)
            {
                RunControlPointEstimation();
            }
            
            if (compareAllMethods)
            {
                CompareAllCompressionMethods(currentTestData);
                return;
            }
            
            CompressionResult result;
            
            if (useFixedControlPoints)
            {
                // 固定コントロールポイントでの圧縮
                result = CompressWithFixedControlPoints(currentTestData, fixedControlPointCount);
                Debug.Log($"固定コントロールポイント圧縮（{fixedControlPointCount}ポイント）を使用");
            }
            else if (useAdvancedCompression)
            {
                result = CompressDataAdvanced(currentTestData);
                Debug.Log($"高度な圧縮（{compressionParams.compressionMethod}）を使用");
            }
            else
            {
                result = CompressData(currentTestData);
                Debug.Log("従来の圧縮（線形補間ベース）を使用");
            }
            
            currentResult = result;
            
            if (result != null)
            {
                Debug.Log($"圧縮結果:");
                Debug.Log($"元データ: {result.originalCount} ポイント");
                Debug.Log($"圧縮後: {result.compressedCount} ポイント");
                Debug.Log($"圧縮率: {result.compressionRatio:F3}");
                Debug.Log($"最大誤差: {result.maxError:F6}");
                Debug.Log($"平均誤差: {result.avgError:F6}");
                
                if (enableVisualization && originalLineRenderer != null && compressedLineRenderer != null)
                {
                    VisualizeData(currentTestData, result.compressedData);
                    VisualizeError(currentTestData, result.compressedData);
                }
                
                if (saveAsAnimationClip)
                {
                    SaveAsAnimationClips(currentTestData, result);
                }
            }
        }
        
        private void CompareAllCompressionMethods(TimeValuePair[] testData)
        {
            Debug.Log("=== 全圧縮手法比較テスト ===");
            Debug.Log($"元データ: {testData.Length} ポイント");
            
            var originalMethod = compressionParams.compressionMethod;
            var methods = System.Enum.GetValues(typeof(CompressionMethod));
            
            foreach (CompressionMethod method in methods)
            {
                compressionParams.compressionMethod = method;
                var result = CompressDataAdvanced(testData);
                
                if (result != null)
                {
                    Debug.Log($"--- {method} ---");
                    Debug.Log($"圧縮後: {result.compressedCount} セグメント/ポイント");
                    Debug.Log($"圧縮率: {result.compressionRatio:F3}");
                    Debug.Log($"最大誤差: {result.maxError:F6}");
                    Debug.Log($"平均誤差: {result.avgError:F6}");
                }
            }
            
            // 元の設定を復元
            compressionParams.compressionMethod = originalMethod;
        }
        
        private void VisualizeData(TimeValuePair[] originalData, TimeValuePair[] compressedData)
        {
            // 元データの可視化
            DrawCurve(originalLineRenderer, originalData, 0f);
            
            // 圧縮データの可視化（設定可能なオフセット）
            DrawCurve(compressedLineRenderer, compressedData, compressedDataYOffset);
        }
        
        private void DrawCurve(LineRenderer lineRenderer, TimeValuePair[] data, float yOffset)
        {
            if (data == null || data.Length == 0) return;
            
            lineRenderer.positionCount = data.Length;
            Vector3[] positions = new Vector3[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                positions[i] = new Vector3(
                    data[i].time * timeScale,
                    data[i].value * curveHeight + yOffset,
                    0
                );
            }
            
            lineRenderer.SetPositions(positions);
        }
        
        private void VisualizeError(TimeValuePair[] originalData, TimeValuePair[] compressedData)
        {
            if (originalData == null || compressedData == null || originalData.Length == 0) return;
            
            errorLineRenderer.positionCount = originalData.Length;
            Vector3[] errorPositions = new Vector3[originalData.Length];
            
            for (int i = 0; i < originalData.Length; i++)
            {
                float interpolatedValue = InterpolateCompressedValue(compressedData, originalData[i].time);
                float error = Mathf.Abs(originalData[i].value - interpolatedValue);
                
                errorPositions[i] = new Vector3(
                    originalData[i].time * timeScale,
                    error * curveHeight + 0.2f, // 誤差は上部に表示
                    0
                );
            }
            
            errorLineRenderer.SetPositions(errorPositions);
        }
        
        private float InterpolateCompressedValue(TimeValuePair[] compressedData, float time)
        {
            if (compressedData.Length == 0) return 0f;
            if (compressedData.Length == 1) return compressedData[0].value;
            
            for (int i = 0; i < compressedData.Length - 1; i++)
            {
                if (time >= compressedData[i].time && time <= compressedData[i + 1].time)
                {
                    float t = (time - compressedData[i].time) / (compressedData[i + 1].time - compressedData[i].time);
                    return Mathf.Lerp(compressedData[i].value, compressedData[i + 1].value, t);
                }
            }
            
            return time < compressedData[0].time ? compressedData[0].value : compressedData[^1].value;
        }
        
        /// <summary>
        /// AnimationCurveから変換
        /// </summary>
        public static TimeValuePair[] FromAnimationCurve(AnimationCurve curve, int sampleCount)
        {
            var data = new TimeValuePair[sampleCount];
            float duration = curve.length > 0 ? curve.keys[^1].time : 1.0f;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / (sampleCount - 1) * duration;
                float value = curve.Evaluate(time);
                data[i] = new TimeValuePair(time, value);
            }
            
            return data;
        }
        
        /// <summary>
        /// AnimationCurveに変換
        /// </summary>
        public static AnimationCurve ToAnimationCurve(TimeValuePair[] data)
        {
            var curve = new AnimationCurve();
            
            foreach (var point in data)
            {
                curve.AddKey(point.time, point.value);
            }
            
            return curve;
        }
        
        void OnDrawGizmos()
        {
            if (!showControlPoints || currentResult?.compressedCurve?.segments == null)
                return;
                
            DrawControlPointGizmos();
        }
        
        private void DrawControlPointGizmos()
        {
            Gizmos.color = controlPointColor;
            
            foreach (var segment in currentResult.compressedCurve.segments)
            {
                switch (segment.curveType)
                {
                    case CurveType.BSpline:
                        DrawBSplineControlPoints(segment);
                        break;
                        
                    case CurveType.Bezier:
                        DrawBezierControlPoints(segment);
                        break;
                        
                    case CurveType.Linear:
                        DrawLinearControlPoints(segment);
                        break;
                }
            }
        }
        
        private void DrawBSplineControlPoints(CurveSegment segment)
        {
            if (segment.bsplineControlPoints == null) return;
            
            // コントロールポイントを描画
            for (int i = 0; i < segment.bsplineControlPoints.Length; i++)
            {
                var point = segment.bsplineControlPoints[i];
                Vector3 worldPos = new Vector3(
                    point.x * timeScale,
                    point.y * curveHeight,
                    0
                );
                
                Gizmos.DrawSphere(worldPos, controlPointSize);
                
                // コントロールラインを描画
                if (showControlLines && i < segment.bsplineControlPoints.Length - 1)
                {
                    var nextPoint = segment.bsplineControlPoints[i + 1];
                    Vector3 nextWorldPos = new Vector3(
                        nextPoint.x * timeScale,
                        nextPoint.y * curveHeight,
                        0
                    );
                    
                    Gizmos.color = controlLineColor;
                    Gizmos.DrawLine(worldPos, nextWorldPos);
                    Gizmos.color = controlPointColor;
                }
            }
        }
        
        private void DrawBezierControlPoints(CurveSegment segment)
        {
            // 始点と終点
            Vector3 startPos = new Vector3(
                segment.startTime * timeScale,
                segment.startValue * curveHeight,
                0
            );
            Vector3 endPos = new Vector3(
                segment.endTime * timeScale,
                segment.endValue * curveHeight,
                0
            );
            
            Gizmos.DrawSphere(startPos, controlPointSize);
            Gizmos.DrawSphere(endPos, controlPointSize);
            
            if (showControlLines)
            {
                // タンジェントハンドルを描画
                float dt = segment.endTime - segment.startTime;
                float handleLength = dt * 0.3f; // ハンドルの長さを調整
                
                Vector3 inHandle = startPos + new Vector3(
                    handleLength * timeScale,
                    segment.inTangent * handleLength * curveHeight,
                    0
                );
                Vector3 outHandle = endPos - new Vector3(
                    handleLength * timeScale,
                    segment.outTangent * handleLength * curveHeight,
                    0
                );
                
                Gizmos.color = controlLineColor;
                Gizmos.DrawLine(startPos, inHandle);
                Gizmos.DrawLine(endPos, outHandle);
                
                // ハンドルポイント
                Gizmos.color = controlPointColor;
                Gizmos.DrawWireSphere(inHandle, controlPointSize * 0.7f);
                Gizmos.DrawWireSphere(outHandle, controlPointSize * 0.7f);
            }
        }
        
        private void DrawLinearControlPoints(CurveSegment segment)
        {
            // 始点と終点のみ
            Vector3 startPos = new Vector3(
                segment.startTime * timeScale,
                segment.startValue * curveHeight,
                0
            );
            Vector3 endPos = new Vector3(
                segment.endTime * timeScale,
                segment.endValue * curveHeight,
                0
            );
            
            Gizmos.DrawWireSphere(startPos, controlPointSize * 0.5f);
            Gizmos.DrawWireSphere(endPos, controlPointSize * 0.5f);
        }
        
        /// <summary>
        /// コントロールポイント推定を実行
        /// </summary>
        private void RunControlPointEstimation()
        {
            if (currentTestData == null || currentTestData.Length < 3)
                return;
                
            // 全アルゴリズムで推定を実行
            lastEstimationResults = ControlPointEstimator.EstimateAll(
                currentTestData, 
                compressionParams.tolerance,
                2,
                Mathf.Min(currentTestData.Length / 2, 100)
            );
            
            // 表示用リストを更新
            estimationResults.Clear();
            foreach (var kvp in lastEstimationResults)
            {
                var display = new EstimationDisplay
                {
                    methodName = kvp.Key,
                    optimalPoints = kvp.Value.optimalPoints,
                    score = kvp.Value.score,
                    metrics = FormatMetrics(kvp.Value.metrics)
                };
                estimationResults.Add(display);
            }
            
            // 選択されたメソッドの結果を固定コントロールポイント数に反映
            if (lastEstimationResults.ContainsKey(selectedEstimationMethod))
            {
                fixedControlPointCount = lastEstimationResults[selectedEstimationMethod].optimalPoints;
            }
            
            Debug.Log("コントロールポイント推定完了:");
            foreach (var result in estimationResults)
            {
                Debug.Log($"- {result.methodName}: {result.optimalPoints}ポイント (スコア: {result.score:F3})");
            }
        }
        
        /// <summary>
        /// メトリクスを文字列形式にフォーマット
        /// </summary>
        private string FormatMetrics(Dictionary<string, float> metrics)
        {
            var formatted = new List<string>();
            foreach (var kvp in metrics)
            {
                formatted.Add($"{kvp.Key}: {kvp.Value:F3}");
            }
            return string.Join(", ", formatted);
        }
        
        /// <summary>
        /// 固定コントロールポイント数で圧縮
        /// </summary>
        private CompressionResult CompressWithFixedControlPoints(TimeValuePair[] originalData, int numControlPoints)
        {
            if (originalData == null || originalData.Length == 0)
            {
                Debug.LogWarning("圧縮対象のデータが空です");
                return null;
            }
            
            TimeValuePair[] compressedData;
            var segments = new List<CurveSegment>();
            float[] tangents = null;
            
            // CompressionMethodに応じて使用するアルゴリズムを選択
            switch (compressionParams.compressionMethod)
            {
                case CompressionMethod.RDP_Linear:
                    // RDPで重要点を抽出し、線形補間
                    compressedData = RDPAlgorithm.Simplify(originalData, compressionParams.tolerance);
                    // numControlPointsに調整
                    compressedData = ResampleToFixedPoints(compressedData, numControlPoints);
                    // 線形セグメントを作成
                    for (int i = 0; i < compressedData.Length - 1; i++)
                    {
                        segments.Add(CurveSegment.CreateLinear(
                            compressedData[i].time, compressedData[i].value,
                            compressedData[i + 1].time, compressedData[i + 1].value
                        ));
                    }
                    break;
                    
                case CompressionMethod.RDP_BSpline:
                    // RDPで重要点を抽出し、BSpline評価
                    compressedData = RDPAlgorithm.Simplify(originalData, compressionParams.tolerance);
                    compressedData = ResampleToFixedPoints(compressedData, numControlPoints);
                    // BSplineセグメントを作成
                    for (int i = 0; i < compressedData.Length - 1; i++)
                    {
                        var controlPoints = new Vector2[] {
                            new Vector2(compressedData[i].time, compressedData[i].value),
                            new Vector2(compressedData[i + 1].time, compressedData[i + 1].value)
                        };
                        segments.Add(CurveSegment.CreateBSpline(controlPoints));
                    }
                    break;
                    
                case CompressionMethod.RDP_Bezier:
                    // RDPで重要点を抽出し、Bezier評価
                    compressedData = RDPAlgorithm.Simplify(originalData, compressionParams.tolerance);
                    compressedData = ResampleToFixedPoints(compressedData, numControlPoints);
                    // Bezierセグメントを作成
                    // タンジェントを事前計算
                    tangents = Core.TangentCalculator.CalculateSmoothTangents(compressedData);
                    for (int i = 0; i < compressedData.Length - 1; i++)
                    {
                        segments.Add(CurveSegment.CreateBezier(
                            compressedData[i].time, compressedData[i].value,
                            compressedData[i + 1].time, compressedData[i + 1].value,
                            tangents[i],      // 始点のタンジェント
                            tangents[i + 1]   // 終点のタンジェント
                        ));
                    }
                    break;
                    
                case CompressionMethod.BSpline_Direct:
                    // BSplineで固定数のコントロールポイントに圧縮
                    compressedData = BSplineAlgorithm.ApproximateWithFixedPoints(originalData, numControlPoints);
                    // BSplineセグメントを作成
                    for (int i = 0; i < compressedData.Length - 1; i++)
                    {
                        var controlPoints = new Vector2[] {
                            new Vector2(compressedData[i].time, compressedData[i].value),
                            new Vector2(compressedData[i + 1].time, compressedData[i + 1].value)
                        };
                        segments.Add(CurveSegment.CreateBSpline(controlPoints));
                    }
                    break;
                    
                case CompressionMethod.Bezier_Direct:
                default:
                    // Bezierで固定数のコントロールポイントに圧縮
                    compressedData = BezierAlgorithm.ApproximateWithFixedPoints(originalData, numControlPoints);
                    // Bezierセグメントを作成
                    // タンジェントを事前計算
                    tangents = Core.TangentCalculator.CalculateSmoothTangents(compressedData);
                    for (int i = 0; i < compressedData.Length - 1; i++)
                    {
                        segments.Add(CurveSegment.CreateBezier(
                            compressedData[i].time, compressedData[i].value,
                            compressedData[i + 1].time, compressedData[i + 1].value,
                            tangents[i],      // 始点のタンジェント
                            tangents[i + 1]   // 終点のタンジェント
                        ));
                    }
                    break;
            }
            
            var compressedCurve = new CompressedCurveData(segments.ToArray());
            return new CompressionResult(originalData, compressedCurve);
        }
        
        /// <summary>
        /// ポイント数を固定数にリサンプリング
        /// </summary>
        private TimeValuePair[] ResampleToFixedPoints(TimeValuePair[] points, int targetCount)
        {
            if (points.Length <= targetCount)
                return points;
                
            var result = new TimeValuePair[targetCount];
            
            // 均等にサンプリング
            for (int i = 0; i < targetCount; i++)
            {
                float t = (float)i / (targetCount - 1);
                int sourceIndex = Mathf.FloorToInt(t * (points.Length - 1));
                
                if (sourceIndex >= points.Length - 1)
                {
                    result[i] = points[points.Length - 1];
                }
                else
                {
                    // 線形補間
                    float localT = (t * (points.Length - 1)) - sourceIndex;
                    float time = Mathf.Lerp(points[sourceIndex].time, points[sourceIndex + 1].time, localT);
                    float value = Mathf.Lerp(points[sourceIndex].value, points[sourceIndex + 1].value, localT);
                    result[i] = new TimeValuePair(time, value);
                }
            }
            
            return result;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 圧縮前後のカーブをAnimationClipとして保存
        /// </summary>
        private void SaveAsAnimationClips(TimeValuePair[] originalData, CompressionResult result)
        {
            try
            {
                // 保存フォルダの作成
                if (!Directory.Exists(animationClipSavePath))
                {
                    Directory.CreateDirectory(animationClipSavePath);
                }
                
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                // 元データのAnimationClip作成
                var originalCurve = BezierAlgorithm.ToAnimationCurve(originalData);
                var originalClip = CreateAnimationClip(originalCurve, $"{animationClipNamePrefix}Original_{timestamp}");
                SaveAnimationClip(originalClip, $"{animationClipNamePrefix}Original_{timestamp}.anim");
                
                // 圧縮データのAnimationClip作成
                AnimationCurve compressedCurve;
                if (result.compressedCurve != null)
                {
                    // 新しいデータ構造を使用している場合
                    compressedCurve = BezierAlgorithm.ToAnimationCurve(result.compressedCurve);
                }
                else
                {
                    // 従来のデータ構造を使用している場合
                    compressedCurve = BezierAlgorithm.ToAnimationCurve(result.compressedData);
                }
                
                var compressedClip = CreateAnimationClip(compressedCurve, $"{animationClipNamePrefix}Compressed_{timestamp}");
                SaveAnimationClip(compressedClip, $"{animationClipNamePrefix}Compressed_{timestamp}.anim");
                
                Debug.Log($"AnimationClipを保存しました: {animationClipSavePath}");
                Debug.Log($"元データ: {animationClipNamePrefix}Original_{timestamp}.anim");
                Debug.Log($"圧縮データ: {animationClipNamePrefix}Compressed_{timestamp}.anim");
                
                // Projectビューを更新
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AnimationClip保存中にエラーが発生しました: {e.Message}");
            }
        }
        
        /// <summary>
        /// AnimationClipを作成
        /// </summary>
        private AnimationClip CreateAnimationClip(AnimationCurve curve, string clipName)
        {
            var clip = new AnimationClip();
            clip.name = clipName;
            
            // カーブをAnimationClipに設定
            clip.SetCurve("", typeof(Transform), targetPropertyPath, curve);
            
            // Legacy設定（必要に応じて）
            clip.legacy = false;
            
            return clip;
        }
        
        /// <summary>
        /// AnimationClipをアセットとして保存
        /// </summary>
        private void SaveAnimationClip(AnimationClip clip, string fileName)
        {
            string fullPath = Path.Combine(animationClipSavePath, fileName);
            AssetDatabase.CreateAsset(clip, fullPath);
        }
        
        /// <summary>
        /// エディタでのみ使用可能な手動保存メソッド
        /// </summary>
        [ContextMenu("Save Current Data as AnimationClips")]
        public void SaveCurrentDataAsAnimationClips()
        {
            if (Application.isPlaying)
            {
                if (currentTestData != null && currentResult != null)
                {
                    SaveAsAnimationClips(currentTestData, currentResult);
                }
                else
                {
                    Debug.LogWarning("圧縮データがありません。最初にテストを実行してください。");
                }
            }
            else
            {
                Debug.LogWarning("この機能は再生モードでのみ使用できます。");
            }
        }
        
        /// <summary>
        /// 手動でテストデータを再生成
        /// </summary>
        [ContextMenu("Regenerate Test Data")]
        public void RegenerateTestData()
        {
            if (Application.isPlaying)
            {
                currentTestData = GenerateTestData();
                TestCompression();
                Debug.Log("テストデータを再生成し、圧縮を実行しました。");
            }
            else
            {
                Debug.LogWarning("この機能は再生モードでのみ使用できます。");
            }
        }
        
        /// <summary>
        /// 手動で圧縮を再実行
        /// </summary>
        [ContextMenu("Re-run Compression")]
        public void RerunCompression()
        {
            if (Application.isPlaying)
            {
                if (currentTestData != null)
                {
                    TestCompression();
                    Debug.Log("圧縮を再実行しました。");
                }
                else
                {
                    Debug.LogWarning("テストデータがありません。最初にテストデータを生成してください。");
                }
            }
            else
            {
                Debug.LogWarning("この機能は再生モードでのみ使用できます。");
            }
        }
        
        /// <summary>
        /// 手動でコントロールポイント推定を実行
        /// </summary>
        [ContextMenu("Run Control Point Estimation")]
        public void RunControlPointEstimationManual()
        {
            if (Application.isPlaying)
            {
                if (currentTestData != null)
                {
                    RunControlPointEstimation();
                    Debug.Log("コントロールポイント推定を実行しました。");
                }
                else
                {
                    Debug.LogWarning("テストデータがありません。最初にテストデータを生成してください。");
                }
            }
            else
            {
                Debug.LogWarning("この機能は再生モードでのみ使用できます。");
            }
        }
#endif
    }
}
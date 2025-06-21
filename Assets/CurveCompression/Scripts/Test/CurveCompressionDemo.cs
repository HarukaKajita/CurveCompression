using UnityEngine;
using System.Collections.Generic;
using CurveCompression.DataStructures;
using CurveCompression.Core;
using CurveCompression.Algorithms;
using CurveCompression.Visualization;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace CurveCompression.Test
{
    /// <summary>
    /// カーブ圧縮のデモ・テストコンポーネント
    /// </summary>
    public class CurveCompressionDemo : MonoBehaviour
    {
        [Header("圧縮パラメータ")]
        [SerializeField] private CompressionParams compressionParams = new CompressionParams();
        
        [Header("テスト用データ")]
        [SerializeField] private int testDataPoints = 1000;
        [SerializeField] private float testDuration = 10.0f;
        [SerializeField] private bool generateTestData = true;
        
        [Header("圧縮オプション")]
        [SerializeField] private bool useAdvancedCompression = true;
        [SerializeField] private bool compareAllMethods = false;
        [SerializeField] private bool useFixedControlPoints = false;
        [SerializeField] private int fixedControlPointCount = 10;
        
        [Header("コントロールポイント推定")]
        [SerializeField] private bool showEstimationResults = true;
        [SerializeField] private string selectedEstimationMethod = "Elbow";
        [SerializeField] private List<EstimationDisplay> estimationResults = new List<EstimationDisplay>();
        
        [Header("実行時更新設定")]
        [SerializeField] private bool autoUpdateOnParameterChange = true;
        [SerializeField] private float updateCheckInterval = 0.5f;
        
        [Header("AnimationClip保存設定")]
        [SerializeField] private bool saveAsAnimationClip = true;
        [SerializeField] private string animationClipSavePath = "Assets/CurveCompression/GeneratedClips/";
        [SerializeField] private string animationClipNamePrefix = "CurveCompression_";
        [SerializeField] private string targetPropertyPath = "transform.position.y";
        
        // 内部状態
        private CurveVisualizer visualizer;
        private TimeValuePair[] currentTestData;
        private CompressionResult currentResult;
        private CompressionParams lastParams;
        private float lastUpdateTime;
        private Dictionary<string, ControlPointEstimator.EstimationResult> lastEstimationResults;
        
        [System.Serializable]
        public class EstimationDisplay
        {
            public string methodName;
            public int optimalPoints;
            public float score;
            public string metrics;
        }
        
        void Start()
        {
            // ビジュアライザーの初期化
            visualizer = GetComponent<CurveVisualizer>();
            if (visualizer == null)
            {
                visualizer = gameObject.AddComponent<CurveVisualizer>();
            }
            visualizer.Initialize();
            
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
                result = Core.CurveCompressor.CompressDataAdvanced(currentTestData, compressionParams);
                Debug.Log($"高度な圧縮（{compressionParams.compressionMethod}）を使用");
            }
            else
            {
                result = Core.CurveCompressor.CompressData(currentTestData, compressionParams);
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
                
                // 可視化
                visualizer.VisualizeData(currentTestData, result);
                
                // AnimationClip保存
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
                var result = Core.CurveCompressor.CompressDataAdvanced(testData, compressionParams);
                
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
            
            // CompressionMethodに応じて使用するアルゴリズムを選択
            switch (compressionParams.compressionMethod)
            {
                case CompressionMethod.RDP_Linear:
                case CompressionMethod.RDP_BSpline:
                case CompressionMethod.RDP_Bezier:
                    // RDPベースの手法
                    var weights = Core.HybridCompressor.GetOptimalWeights(compressionParams.dataType, compressionParams.importanceWeights);
                    compressedData = RDPAlgorithm.Simplify(originalData, compressionParams.tolerance, 
                        compressionParams.importanceThreshold, weights);
                    compressedData = ResampleToFixedPoints(compressedData, numControlPoints);
                    break;
                    
                case CompressionMethod.BSpline_Direct:
                    compressedData = BSplineAlgorithm.ApproximateWithFixedPoints(originalData, numControlPoints);
                    break;
                    
                case CompressionMethod.Bezier_Direct:
                default:
                    compressedData = BezierAlgorithm.ApproximateWithFixedPoints(originalData, numControlPoints);
                    break;
            }
            
            // セグメントを作成
            segments = CreateSegmentsFromPoints(compressedData, compressionParams.compressionMethod);
            
            var compressedCurve = new CompressedCurveData(segments.ToArray());
            return new CompressionResult(originalData, compressedCurve);
        }
        
        /// <summary>
        /// ポイント配列からセグメントを作成
        /// </summary>
        private List<CurveSegment> CreateSegmentsFromPoints(TimeValuePair[] points, CompressionMethod method)
        {
            var segments = new List<CurveSegment>();
            
            for (int i = 0; i < points.Length - 1; i++)
            {
                switch (method)
                {
                    case CompressionMethod.RDP_Linear:
                        segments.Add(CurveSegment.CreateLinear(
                            points[i].time, points[i].value,
                            points[i + 1].time, points[i + 1].value
                        ));
                        break;
                        
                    case CompressionMethod.RDP_BSpline:
                    case CompressionMethod.BSpline_Direct:
                        var controlPoints = new Vector2[] {
                            new Vector2(points[i].time, points[i].value),
                            new Vector2(points[i + 1].time, points[i + 1].value)
                        };
                        segments.Add(CurveSegment.CreateBSpline(controlPoints));
                        break;
                        
                    case CompressionMethod.RDP_Bezier:
                    case CompressionMethod.Bezier_Direct:
                        float inTangent = 0f;
                        float outTangent = 0f;
                        
                        if (i > 0)
                        {
                            inTangent = (points[i].value - points[i - 1].value) / 
                                       (points[i].time - points[i - 1].time);
                        }
                        if (i < points.Length - 2)
                        {
                            outTangent = (points[i + 2].value - points[i + 1].value) / 
                                        (points[i + 2].time - points[i + 1].time);
                        }
                        
                        segments.Add(CurveSegment.CreateBezier(
                            points[i].time, points[i].value,
                            points[i + 1].time, points[i + 1].value,
                            inTangent, outTangent
                        ));
                        break;
                }
            }
            
            return segments;
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
                    compressedCurve = BezierAlgorithm.ToAnimationCurve(result.compressedCurve);
                }
                else
                {
                    compressedCurve = BezierAlgorithm.ToAnimationCurve(result.compressedData);
                }
                
                var compressedClip = CreateAnimationClip(compressedCurve, $"{animationClipNamePrefix}Compressed_{timestamp}");
                SaveAnimationClip(compressedClip, $"{animationClipNamePrefix}Compressed_{timestamp}.anim");
                
                Debug.Log($"AnimationClipを保存しました: {animationClipSavePath}");
                
                // Projectビューを更新
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AnimationClip保存中にエラーが発生しました: {e.Message}");
            }
        }
        
        private AnimationClip CreateAnimationClip(AnimationCurve curve, string clipName)
        {
            var clip = new AnimationClip();
            clip.name = clipName;
            clip.SetCurve("", typeof(Transform), targetPropertyPath, curve);
            clip.legacy = false;
            return clip;
        }
        
        private void SaveAnimationClip(AnimationClip clip, string fileName)
        {
            string fullPath = Path.Combine(animationClipSavePath, fileName);
            AssetDatabase.CreateAsset(clip, fullPath);
        }
        
        // Context menu methods
        [ContextMenu("Save Current Data as AnimationClips")]
        public void SaveCurrentDataAsAnimationClips()
        {
            if (Application.isPlaying && currentTestData != null && currentResult != null)
            {
                SaveAsAnimationClips(currentTestData, currentResult);
            }
        }
        
        [ContextMenu("Regenerate Test Data")]
        public void RegenerateTestData()
        {
            if (Application.isPlaying)
            {
                currentTestData = GenerateTestData();
                TestCompression();
            }
        }
        
        [ContextMenu("Re-run Compression")]
        public void RerunCompression()
        {
            if (Application.isPlaying && currentTestData != null)
            {
                TestCompression();
            }
        }
        
        [ContextMenu("Run Control Point Estimation")]
        public void RunControlPointEstimationManual()
        {
            if (Application.isPlaying && currentTestData != null)
            {
                RunControlPointEstimation();
            }
        }
#endif
    }
}
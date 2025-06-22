using UnityEngine;
using System.Collections.Generic;
using CurveCompression.DataStructures;
using CurveCompression.Core;
using CurveCompression.Algorithms;
using CurveCompression.Visualization;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
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
        [SerializeField] private bool compareAllMethods = false;
        
        [Header("コントロールポイント推定")]
        [SerializeField] private bool showEstimationResults = true;
        [SerializeField] private List<EstimationDisplay> estimationResults = new List<EstimationDisplay>();
        
        [Header("実行時更新設定")]
        [SerializeField] private bool autoUpdateOnParameterChange = true;
        [SerializeField] private float updateCheckInterval = 0.5f;
        
        [Header("AnimationClip保存設定")]
        [SerializeField] private bool saveAsAnimationClip = false;
        [SerializeField] private string animationClipSavePath = "Assets/CurveCompression/GeneratedClips/";
        [SerializeField] private string animationClipNamePrefix = "CurveCompression_";
        [SerializeField] private string targetPropertyPath = "transform.position.y";
        
        [Header("Gizmo表示設定")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color originalDataGizmoColor = Color.blue;
        [SerializeField] private Color compressedDataGizmoColor = Color.red;
        [SerializeField] private Color controlPointGizmoColor = Color.green;
        [SerializeField] private float controlPointGizmoSize = 0.1f;
        [SerializeField] private float gizmoTimeScale = 1.0f;
        [SerializeField] private float gizmoCurveHeight = 2.0f;
        [SerializeField] private float gizmoYOffset = 0f;
        
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
            // ビジュアライザーの初期化（Playモードのみ）
            if (Application.isPlaying)
            {
                visualizer = GetComponent<CurveVisualizer>();
                if (visualizer == null)
                {
                    visualizer = gameObject.AddComponent<CurveVisualizer>();
                }
                visualizer.Initialize();
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
            
            switch (compressionParams.compressionMode)
            {
                case CompressionMode.ToleranceBased:
                    result = Core.CurveCompressor.Compress(currentTestData, compressionParams);
                    Debug.Log($"許容誤差ベース圧縮（{compressionParams.compressionMethod}、誤差: {compressionParams.tolerance}）を使用");
                    break;
                    
                case CompressionMode.FixedControlPoints:
                    result = CompressWithFixedControlPoints(currentTestData, compressionParams.fixedControlPointCount, compressionParams.compressionMethod);
                    Debug.Log($"固定コントロールポイント圧縮（{compressionParams.fixedControlPointCount}ポイント、{compressionParams.compressionMethod}）を使用");
                    break;
                    
                case CompressionMode.EstimatedControlPoints:
                    int estimatedCount = GetEstimatedControlPointCount();
                    result = CompressWithFixedControlPoints(currentTestData, estimatedCount, compressionParams.compressionMethod);
                    Debug.Log($"推定コントロールポイント圧縮（{estimatedCount}ポイント、{compressionParams.estimationMethod}推定、{compressionParams.compressionMethod}）を使用");
                    break;
                    
                default:
                    result = Core.CurveCompressor.Compress(currentTestData, compressionParams);
                    Debug.Log($"デフォルト圧縮（{compressionParams.compressionMethod}）を使用");
                    break;
            }
            
            currentResult = result;
            
            if (result != null)
            {
                DisplayResults(result);
                
                // 可視化（Playモードのみ）
                if (Application.isPlaying && visualizer != null)
                {
                    visualizer.VisualizeData(currentTestData, result);
                }
                
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
                var result = Core.CurveCompressor.Compress(testData, compressionParams);
                
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
            
            // 現在の推定メソッドの結果をパラメータに反映
            string currentMethod = compressionParams.estimationMethod.ToString();
            if (lastEstimationResults.ContainsKey(currentMethod))
            {
                compressionParams.fixedControlPointCount = lastEstimationResults[currentMethod].optimalPoints;
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
        private CompressionResult CompressWithFixedControlPoints(TimeValuePair[] originalData, int numControlPoints, CompressionMethod method)
        {
            if (originalData == null || originalData.Length == 0)
            {
                Debug.LogWarning("圧縮対象のデータが空です");
                return null;
            }
            
            // 入力検証
            if (numControlPoints < 2)
            {
                Debug.LogWarning("コントロールポイント数は2以上である必要があります");
                numControlPoints = 2;
            }
            
            if (numControlPoints > originalData.Length)
            {
                Debug.LogWarning($"コントロールポイント数は元データ数({originalData.Length})以下である必要があります");
                numControlPoints = originalData.Length;
            }
            
            // 時間計測の開始
            Stopwatch stopwatch = null;
            if (compressionParams.enableTimeMeasurement)
            {
                stopwatch = Stopwatch.StartNew();
            }
            
            TimeValuePair[] controlPoints;
            CurveType curveType;
            
            // アルゴリズムに応じて固定コントロールポイント圧縮を実行
            switch (method)
            {
                case CompressionMethod.BSpline_Direct:
                    controlPoints = BSplineAlgorithm.ApproximateWithFixedPoints(originalData, numControlPoints);
                    curveType = CurveType.BSpline;
                    break;
                        
                case CompressionMethod.Bezier_Direct:
                    controlPoints = BezierAlgorithm.ApproximateWithFixedPoints(originalData, numControlPoints);
                    curveType = CurveType.Bezier;
                    break;
                        
                default: // 線形補間（RDPベースは固定数に適さないため線形にフォールバック）
                    controlPoints = SelectOptimalLinearPoints(originalData, numControlPoints);
                    curveType = CurveType.Linear;
                    break;
            }
            
            // 時間計測の終了
            float compressionTime = 0f;
            if (stopwatch != null)
            {
                stopwatch.Stop();
                compressionTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            }
            
            var result = CreateResultFromFixedPoints(controlPoints, originalData, curveType);
            if (result != null)
            {
                result.compressionTime = compressionTime;
            }
            
            return result;
        }
        
        /// <summary>
        /// 固定コントロールポイントから結果を作成
        /// </summary>
        private CompressionResult CreateResultFromFixedPoints(TimeValuePair[] controlPoints, TimeValuePair[] originalData, CurveType curveType)
        {
            var segments = new List<CurveSegment>();
            
            // Bezier用のタンジェントを事前計算
            float[] tangents = null;
            if (curveType == CurveType.Bezier)
            {
                tangents = TangentCalculator.CalculateSmoothTangents(controlPoints);
            }
            
            // セグメントを作成
            for (int i = 0; i < controlPoints.Length - 1; i++)
            {
                switch (curveType)
                {
                    case CurveType.Linear:
                        segments.Add(CurveSegment.CreateLinear(
                            controlPoints[i].time, controlPoints[i].value,
                            controlPoints[i + 1].time, controlPoints[i + 1].value
                        ));
                        break;
                        
                    case CurveType.BSpline:
                        var bsplineControlPoints = new Vector2[] {
                            new Vector2(controlPoints[i].time, controlPoints[i].value),
                            new Vector2(controlPoints[i + 1].time, controlPoints[i + 1].value)
                        };
                        segments.Add(CurveSegment.CreateBSpline(bsplineControlPoints));
                        break;
                        
                    case CurveType.Bezier:
                        segments.Add(CurveSegment.CreateBezier(
                            controlPoints[i].time, controlPoints[i].value,
                            controlPoints[i + 1].time, controlPoints[i + 1].value,
                            tangents[i], tangents[i + 1]
                        ));
                        break;
                }
            }
            
            var compressedCurve = new CompressedCurveData(segments.ToArray());
            return new CompressionResult(originalData, compressedCurve);
        }
        
        /// <summary>
        /// 線形補間用の最適なポイント選択
        /// </summary>
        private TimeValuePair[] SelectOptimalLinearPoints(TimeValuePair[] data, int numPoints)
        {
            if (numPoints >= data.Length)
                return data;
                
            var result = new TimeValuePair[numPoints];
            
            // 均等分布で選択
            for (int i = 0; i < numPoints; i++)
            {
                float t = (float)i / (numPoints - 1);
                int index = Mathf.RoundToInt(t * (data.Length - 1));
                result[i] = data[index];
            }
            
            return result;
        }
        
        /// <summary>
        /// 圧縮結果の詳細表示
        /// </summary>
        private void DisplayResults(CompressionResult result)
        {
            Debug.Log($"=== 圧縮結果 ===");
            Debug.Log($"モード: {compressionParams.compressionMode}");
            Debug.Log($"アルゴリズム: {compressionParams.compressionMethod}");
            Debug.Log($"元データ: {result.originalCount} ポイント");
            
            switch (compressionParams.compressionMode)
            {
                case CompressionMode.ToleranceBased:
                    Debug.Log($"許容誤差: {compressionParams.tolerance}");
                    Debug.Log($"圧縮後: {result.compressedCount} ポイント/セグメント");
                    Debug.Log($"データタイプ重み: {compressionParams.dataType}");
                    break;
                    
                case CompressionMode.FixedControlPoints:
                    Debug.Log($"指定コントロールポイント数: {compressionParams.fixedControlPointCount}");
                    Debug.Log($"実際のコントロールポイント数: {result.compressedCount}");
                    Debug.Log($"セグメント数: {result.compressedCurve?.segments?.Length ?? 0}");
                    break;
                    
                case CompressionMode.EstimatedControlPoints:
                    Debug.Log($"推定方法: {compressionParams.estimationMethod}");
                    Debug.Log($"推定コントロールポイント数: {result.compressedCount}");
                    Debug.Log($"セグメント数: {result.compressedCurve?.segments?.Length ?? 0}");
                    if (lastEstimationResults?.ContainsKey(compressionParams.estimationMethod.ToString()) == true)
                    {
                        var estimation = lastEstimationResults[compressionParams.estimationMethod.ToString()];
                        Debug.Log($"推定スコア: {estimation.score:F3}");
                    }
                    break;
            }
            
            Debug.Log($"圧縮率: {result.compressionRatio:F3}");
            Debug.Log($"最大誤差: {result.maxError:F6}");
            Debug.Log($"平均誤差: {result.avgError:F6}");
            
            // 圧縮時間を表示（計測有効時のみ）
            if (compressionParams.enableTimeMeasurement && result.compressionTime > 0)
            {
                Debug.Log($"圧縮時間: {result.compressionTime:F2} ms");
            }
        }
        
        /// <summary>
        /// 推定アルゴリズムからコントロールポイント数を取得
        /// </summary>
        private int GetEstimatedControlPointCount()
        {
            string methodName = compressionParams.estimationMethod.ToString();
            
            if (lastEstimationResults?.ContainsKey(methodName) == true)
            {
                return lastEstimationResults[methodName].optimalPoints;
            }
            
            // フォールバック: リアルタイム推定
            try
            {
                var estimation = ControlPointEstimator.EstimateByMethod(
                    currentTestData,
                    compressionParams.tolerance,
                    methodName);
                
                return estimation.optimalPoints;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"推定アルゴリズム {methodName} でエラーが発生しました: {ex.Message}。デフォルト値を使用します。");
                return compressionParams.fixedControlPointCount;
            }
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
                var originalCurve = UnityCompressionUtils.ToAnimationCurve(originalData);
                var originalClip = CreateAnimationClip(originalCurve, $"{animationClipNamePrefix}Original_{timestamp}");
                SaveAnimationClip(originalClip, $"{animationClipNamePrefix}Original_{timestamp}.anim");
                
                // 圧縮データのAnimationClip作成
                AnimationCurve compressedCurve;
                if (result.compressedCurve != null)
                {
                    compressedCurve = UnityCompressionUtils.ToAnimationCurve(result.compressedCurve);
                }
                else
                {
                    compressedCurve = UnityCompressionUtils.ToAnimationCurve(result.compressedData);
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
            if (currentTestData != null && currentResult != null)
            {
                SaveAsAnimationClips(currentTestData, currentResult);
            }
            else
            {
                Debug.LogWarning("データまたは圧縮結果がありません。先にテストデータの生成と圧縮を実行してください。");
            }
        }
        
        [ContextMenu("Regenerate Test Data")]
        public void RegenerateTestData()
        {
            currentTestData = GenerateTestData();
            TestCompression();
            Debug.Log($"テストデータを生成しました: {currentTestData.Length}ポイント");
        }
        
        [ContextMenu("Re-run Compression")]
        public void RerunCompression()
        {
            if (currentTestData != null)
            {
                TestCompression();
            }
            else
            {
                Debug.LogWarning("テストデータがありません。先にテストデータを生成してください。");
            }
        }
        
        [ContextMenu("Run Control Point Estimation")]
        public void RunControlPointEstimationManual()
        {
            if (currentTestData != null)
            {
                RunControlPointEstimation();
            }
            else
            {
                Debug.LogWarning("テストデータがありません。先にテストデータを生成してください。");
            }
        }
#endif
        
        /// <summary>
        /// Gizmoによる可視化
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            if (currentTestData == null || currentTestData.Length == 0) return;
            
            Vector3 basePosition = transform.position;
            
            // 元データの描画
            if (currentTestData != null && currentTestData.Length > 1)
            {
                Gizmos.color = originalDataGizmoColor;
                for (int i = 0; i < currentTestData.Length - 1; i++)
                {
                    Vector3 startPos = basePosition + new Vector3(
                        currentTestData[i].time * gizmoTimeScale,
                        currentTestData[i].value * gizmoCurveHeight + gizmoYOffset,
                        0
                    );
                    Vector3 endPos = basePosition + new Vector3(
                        currentTestData[i + 1].time * gizmoTimeScale,
                        currentTestData[i + 1].value * gizmoCurveHeight + gizmoYOffset,
                        0
                    );
                    Gizmos.DrawLine(startPos, endPos);
                }
            }
            
            // 圧縮データの描画
            if (currentResult != null && currentResult.compressedCurve != null)
            {
                Gizmos.color = compressedDataGizmoColor;
                DrawCompressedCurveGizmos(currentResult.compressedCurve, basePosition);
                
                // コントロールポイントの描画
                if (currentResult.compressedData != null)
                {
                    Gizmos.color = controlPointGizmoColor;
                    foreach (var point in currentResult.compressedData)
                    {
                        Vector3 pos = basePosition + new Vector3(
                            point.time * gizmoTimeScale,
                            point.value * gizmoCurveHeight + gizmoYOffset,
                            0
                        );
                        Gizmos.DrawSphere(pos, controlPointGizmoSize);
                    }
                }
            }
        }
        
        /// <summary>
        /// 圧縮カーブのGizmo描画
        /// </summary>
        private void DrawCompressedCurveGizmos(CompressedCurveData curveData, Vector3 basePosition)
        {
            const int samplesPerSegment = 20;
            
            foreach (var segment in curveData.segments)
            {
                float startTime = segment.startTime;
                float endTime = segment.endTime;
                
                for (int i = 0; i < samplesPerSegment - 1; i++)
                {
                    float t1 = (float)i / (samplesPerSegment - 1);
                    float t2 = (float)(i + 1) / (samplesPerSegment - 1);
                    
                    float time1 = Mathf.Lerp(startTime, endTime, t1);
                    float time2 = Mathf.Lerp(startTime, endTime, t2);
                    
                    float value1 = segment.Evaluate(time1);
                    float value2 = segment.Evaluate(time2);
                    
                    Vector3 pos1 = basePosition + new Vector3(
                        time1 * gizmoTimeScale,
                        value1 * gizmoCurveHeight + gizmoYOffset,
                        0
                    );
                    Vector3 pos2 = basePosition + new Vector3(
                        time2 * gizmoTimeScale,
                        value2 * gizmoCurveHeight + gizmoYOffset,
                        0
                    );
                    
                    Gizmos.DrawLine(pos1, pos2);
                }
            }
        }
    }
}
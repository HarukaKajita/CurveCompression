using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace CurveCompression
{
    // =============================================================================
    // CurveCompressor.cs - メイン処理クラス
    // =============================================================================
    
	/// <summary>
    /// カーブ圧縮のメインクラス
    /// </summary>
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
        [SerializeField] private Color originalDataColor = Color.blue;
        [SerializeField] private Color compressedDataColor = Color.red;
        [SerializeField] private Color errorVisualizationColor = Color.yellow;
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private float curveHeight = 2.0f;
        [SerializeField] private float timeScale = 1.0f;
        
        [Header("AnimationClip保存設定")]
        [SerializeField] private bool saveAsAnimationClip = true;
        [SerializeField] private string animationClipSavePath = "Assets/CurveCompression/GeneratedClips/";
        [SerializeField] private string animationClipNamePrefix = "CurveCompression_";
        [SerializeField] private string targetPropertyPath = "transform.position.y"; // アニメーション対象プロパティ
        
        private LineRenderer originalLineRenderer;
        private LineRenderer compressedLineRenderer;
        private LineRenderer errorLineRenderer;
        
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
            
            if (compressionParams.enableHybrid)
            {
                compressedData = HybridCompressor.Compress(originalData, compressionParams);
            }
            else if (compressionParams.adaptiveWeight < 0.5f)
            {
                var weights = HybridCompressor.GetOptimalWeights(compressionParams.dataType, compressionParams.importanceWeights);
                compressedData = RDPAlgorithm.Simplify(originalData, compressionParams.tolerance, 
                    compressionParams.importanceThreshold, weights);
            }
            else
            {
                compressedData = BSplineAlgorithm.ApproximateWithBSpline(originalData, compressionParams.tolerance, compressionParams.maxSplineSegments);
            }
            
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
            
            CompressedCurveData compressedCurve;
            
            if (compressionParams.enableHybrid)
            {
                compressedCurve = HybridCompressor.CompressAdvanced(originalData, compressionParams);
            }
            else if (compressionParams.adaptiveWeight < 0.5f)
            {
                compressedCurve = BSplineAlgorithm.Compress(originalData, compressionParams.tolerance, compressionParams.maxSplineSegments);
            }
            else
            {
                compressedCurve = BezierAlgorithm.Compress(originalData, compressionParams.tolerance, compressionParams.maxSplineSegments);
            }
            
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
                TestCompression();
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
            lineRenderer.color = color;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 1;
        }
        
        private void TestCompression()
        {
            var testData = GenerateTestData();
            CompressionResult result;
            
            if (useAdvancedCompression)
            {
                result = CompressDataAdvanced(testData);
                Debug.Log("高度な圧縮（曲線ベース）を使用");
            }
            else
            {
                result = CompressData(testData);
                Debug.Log("従来の圧縮（線形補間ベース）を使用");
            }
            
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
                    VisualizeData(testData, result.compressedData);
                    VisualizeError(testData, result.compressedData);
                }
                
                if (saveAsAnimationClip)
                {
                    SaveAsAnimationClips(testData, result);
                }
            }
        }
        
        private void VisualizeData(TimeValuePair[] originalData, TimeValuePair[] compressedData)
        {
            // 元データの可視化
            DrawCurve(originalLineRenderer, originalData, 0f);
            
            // 圧縮データの可視化（少し上にオフセット）
            DrawCurve(compressedLineRenderer, compressedData, 0.1f);
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
                var testData = GenerateTestData();
                var result = useAdvancedCompression ? CompressDataAdvanced(testData) : CompressData(testData);
                
                if (result != null)
                {
                    SaveAsAnimationClips(testData, result);
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
using UnityEngine;

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
            if (generateTestData)
            {
                TestCompression();
            }
        }
        
        private void TestCompression()
        {
            var testData = GenerateTestData();
            var result = CompressData(testData);
            
            if (result != null)
            {
                Debug.Log($"圧縮結果:");
                Debug.Log($"元データ: {result.originalCount} ポイント");
                Debug.Log($"圧縮後: {result.compressedCount} ポイント");
                Debug.Log($"圧縮率: {result.compressionRatio:F3}");
                Debug.Log($"最大誤差: {result.maxError:F6}");
                Debug.Log($"平均誤差: {result.avgError:F6}");
            }
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
    }
}
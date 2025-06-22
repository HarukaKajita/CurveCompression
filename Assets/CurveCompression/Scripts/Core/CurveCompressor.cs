using UnityEngine;
using CurveCompression.DataStructures;
using CurveCompression.Algorithms;

namespace CurveCompression.Core
{
    /// <summary>
    /// カーブ圧縮のメインクラス
    /// </summary>
    public static class CurveCompressor
    {
        /// <summary>
        /// データを圧縮（標準インターフェース）
        /// </summary>
        public static CompressionResult Compress(TimeValuePair[] originalData, CompressionParams parameters)
        {
            ValidationUtils.ValidatePoints(originalData, nameof(originalData));
            ValidationUtils.ValidateCompressionParams(parameters);
            
            // 統一的な圧縮手法を使用
            CompressedCurveData compressedCurve = HybridCompressor.Compress(originalData, parameters);
            
            return new CompressionResult(originalData, compressedCurve);
        }
        
        /// <summary>
        /// データを圧縮（シンプルインターフェース）
        /// </summary>
        public static CompressionResult Compress(TimeValuePair[] originalData, float tolerance)
        {
            var parameters = new CompressionParams { tolerance = tolerance };
            return Compress(originalData, parameters);
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
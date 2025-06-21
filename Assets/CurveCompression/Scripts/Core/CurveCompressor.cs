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
        /// データを圧縮（従来の線形補間ベース）
        /// </summary>
        public static CompressionResult CompressData(TimeValuePair[] originalData, CompressionParams parameters)
        {
            if (originalData == null || originalData.Length == 0)
            {
                Debug.LogWarning("圧縮対象のデータが空です");
                return null;
            }
            
            TimeValuePair[] compressedData;
            
            // 従来の圧縮手法（RDPベース）
            var weights = HybridCompressor.GetOptimalWeights(parameters.dataType, parameters.importanceWeights);
            compressedData = RDPAlgorithm.Simplify(originalData, parameters.tolerance, 
                parameters.importanceThreshold, weights);
            
            return new CompressionResult(originalData, compressedData);
        }
        
        /// <summary>
        /// データを圧縮（新しいデータ構造を使用）
        /// </summary>
        public static CompressionResult CompressDataAdvanced(TimeValuePair[] originalData, CompressionParams parameters)
        {
            if (originalData == null || originalData.Length == 0)
            {
                Debug.LogWarning("圧縮対象のデータが空です");
                return null;
            }
            
            // 新しい統一的な圧縮手法を使用
            CompressedCurveData compressedCurve = HybridCompressor.CompressAdvanced(originalData, parameters);
            
            return new CompressionResult(originalData, compressedCurve);
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
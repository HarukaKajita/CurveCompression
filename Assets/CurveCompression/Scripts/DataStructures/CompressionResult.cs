using UnityEngine;
using CurveCompression.Core;

namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 圧縮結果
    /// </summary>
    public class CompressionResult
    {
        public TimeValuePair[] compressedData;
        public CompressedCurveData compressedCurve;
        public float compressionRatio;
        public float maxError;
        public float avgError;
        public int originalCount;
        public int compressedCount;
        public float compressionTime; // 圧縮処理時間（ミリ秒）
        
        public CompressionResult(TimeValuePair[] original, TimeValuePair[] compressed)
        {
            compressedData = compressed;
            originalCount = original.Length;
            compressedCount = compressed.Length;
            compressionRatio = (float)compressedCount / originalCount;
            compressionTime = 0f; // デフォルト値（計測なし）
            CalculateErrors(original, compressed);
        }
        
        public CompressionResult(TimeValuePair[] original, CompressedCurveData compressed)
        {
            compressedCurve = compressed;
            compressedData = compressed.ToTimeValuePairs(original.Length); // 同じサンプル数でサンプリング
            originalCount = original.Length;
            compressedCount = compressed.segments.Length;
            compressionRatio = (float)compressedCount / originalCount;
            compressionTime = 0f; // デフォルト値（計測なし）
            CalculateErrorsWithCurve(original, compressed);
        }
        
        private void CalculateErrors(TimeValuePair[] original, TimeValuePair[] compressed)
        {
            float totalError = 0f;
            maxError = 0f;
            
            for (int i = 0; i < original.Length; i++)
            {
                float interpolatedValue = InterpolateValue(compressed, original[i].time);
                float error = Mathf.Abs(original[i].value - interpolatedValue);
                totalError += error;
                maxError = Mathf.Max(maxError, error);
            }
            
            avgError = totalError / original.Length;
        }
        
        private void CalculateErrorsWithCurve(TimeValuePair[] original, CompressedCurveData compressed)
        {
            float totalError = 0f;
            maxError = 0f;
            
            for (int i = 0; i < original.Length; i++)
            {
                float curveValue = compressed.Evaluate(original[i].time);
                float error = Mathf.Abs(original[i].value - curveValue);
                totalError += error;
                maxError = Mathf.Max(maxError, error);
            }
            
            avgError = totalError / original.Length;
        }
        
        private float InterpolateValue(TimeValuePair[] data, float time)
        {
            return InterpolationUtils.LinearInterpolate(data, time);
        }
    }
}
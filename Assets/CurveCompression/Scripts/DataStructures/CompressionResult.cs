using UnityEngine;

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
        
        public CompressionResult(TimeValuePair[] original, TimeValuePair[] compressed)
        {
            compressedData = compressed;
            originalCount = original.Length;
            compressedCount = compressed.Length;
            compressionRatio = (float)compressedCount / originalCount;
            CalculateErrors(original, compressed);
        }
        
        public CompressionResult(TimeValuePair[] original, CompressedCurveData compressed)
        {
            compressedCurve = compressed;
            compressedData = compressed.ToTimeValuePairs(original.Length); // 同じサンプル数でサンプリング
            originalCount = original.Length;
            compressedCount = compressed.segments.Length;
            compressionRatio = (float)compressedCount / originalCount;
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
            if (data.Length == 0) return 0f;
            if (data.Length == 1) return data[0].value;
            
            // データが単調増加していると仮定して線形補間
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (time >= data[i].time && time <= data[i + 1].time)
                {
                    float t = (time - data[i].time) / (data[i + 1].time - data[i].time);
                    return Mathf.Lerp(data[i].value, data[i + 1].value, t);
                }
            }
            
            return time < data[0].time ? data[0].value : data[^1].value;
        }
    }
}
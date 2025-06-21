using System;
using UnityEngine;

namespace CurveCompression
{
    // =============================================================================
    // DataTypes.cs - 基本データ構造とユーティリティ
    // =============================================================================
    /// <summary>
    /// 時間付きデータポイント
    /// </summary>
    [Serializable]
    public struct TimeValuePair : IComparable<TimeValuePair>
    {
        public float time;
        public float value;
        
        public TimeValuePair(float time, float value)
        {
            this.time = time;
            this.value = value;
        }
        
        public int CompareTo(TimeValuePair other)
        {
            return time.CompareTo(other.time);
        }
    }
    
    public enum CompressionDataType
    {
        Animation,       // アニメーションデータ
        SensorData,      // センサーデータ
        FinancialData,   // 金融データ
        Custom           // カスタムデータ
    }
    
    /// <summary>
    /// 圧縮手法の選択
    /// </summary>
    public enum CompressionMethod
    {
        RDP_Linear,        // RDPに基づく線形補間評価
        RDP_BSpline,       // RDPに基づくBSpline評価  
        RDP_Bezier,        // RDPに基づくBezier評価
        BSpline_Direct,    // BSplineによる直接近似
        Bezier_Direct      // Bezierによる直接近似
    }
    /// <summary>
    /// 圧縮パラメータ
    /// </summary>
    [Serializable]
    public class CompressionParams
    {
        [Range(0.001f, 1.0f)]
        public float tolerance = 0.01f;          // 許容誤差
        
        [Range(0.1f, 10.0f)]
        public float importanceThreshold = 1.0f; // 重要度閾値
        
        public CompressionMethod compressionMethod = CompressionMethod.Bezier_Direct; // 圧縮手法
        
        public CompressionDataType dataType = CompressionDataType.Animation;
        public ImportanceWeights importanceWeights = ImportanceWeights.Default;
    }
    
    /// <summary>
    /// 重要度計算のための重み設定
    /// </summary>
    [Serializable]
    public class ImportanceWeights
    {
        [Range(0f, 1f)] public float curvature = 0.4f;      // 曲率の重み
        [Range(0f, 1f)] public float changeRate = 0.25f;    // 変化率の重み
        [Range(0f, 1f)] public float localVariance = 0.2f;  // 局所分散の重み
        [Range(0f, 1f)] public float extremeValue = 0.15f;  // 極値の重み
        
        /// <summary>
        /// デフォルト設定（データタイプに基づく推奨値）
        /// </summary>
        public static ImportanceWeights Default => new ImportanceWeights();
        
        /// <summary>
        /// アニメーション用（滑らかさ重視）
        /// </summary>
        public static ImportanceWeights ForAnimation => new ImportanceWeights 
        { 
            curvature = 0.5f, changeRate = 0.2f, localVariance = 0.15f, extremeValue = 0.15f 
        };
        
        /// <summary>
        /// センサーデータ用（ノイズ耐性重視）
        /// </summary>
        public static ImportanceWeights ForSensorData => new ImportanceWeights 
        { 
            curvature = 0.3f, changeRate = 0.15f, localVariance = 0.35f, extremeValue = 0.2f 
        };
        
        /// <summary>
        /// 金融データ用（極値重視）
        /// </summary>
        public static ImportanceWeights ForFinancialData => new ImportanceWeights 
        { 
            curvature = 0.25f, changeRate = 0.3f, localVariance = 0.15f, extremeValue = 0.3f 
        };
        
        /// <summary>
        /// 重みの正規化
        /// </summary>
        public void Normalize()
        {
            float total = curvature + changeRate + localVariance + extremeValue;
            if (total > 0)
            {
                curvature /= total;
                changeRate /= total;
                localVariance /= total;
                extremeValue /= total;
            }
        }
    }
    
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

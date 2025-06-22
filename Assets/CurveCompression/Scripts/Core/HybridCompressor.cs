using System;
using System.Collections.Generic;
using UnityEngine;
using CurveCompression.DataStructures;
using CurveCompression.Algorithms;

namespace CurveCompression.Core
{
    // =============================================================================
    // HybridCompressor.cs - ハイブリッドアプローチ実装
    // =============================================================================

    /// <summary>
    /// RDPとB-スプラインを組み合わせたハイブリッド圧縮アルゴリズム
    /// </summary>
    public static class HybridCompressor
    {
        
        /// <summary>
        /// 指定された圧縮手法で圧縮を実行（標準インターフェース）
        /// </summary>
        public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
        {
            ValidationUtils.ValidatePoints(points, nameof(points));
            ValidationUtils.ValidateCompressionParams(parameters);
            
            if (points.Length <= 2)
            {
                var linearSegment = CurveSegment.CreateLinear(
                    points[0].time, points[0].value,
                    points[^1].time, points[^1].value
                );
                return new CompressedCurveData(new[] { linearSegment });
            }
            
            // データタイプに基づく重み設定の自動調整
            var weights = GetOptimalWeights(parameters.dataType, parameters.importanceWeights);
            
            return parameters.compressionMethod switch
            {
                CompressionMethod.RDP_Linear => RDPAlgorithm.CompressWithCurveEvaluation(
                    points, parameters.tolerance, CurveType.Linear, parameters.importanceThreshold, weights),
                    
                CompressionMethod.RDP_BSpline => RDPAlgorithm.CompressWithCurveEvaluation(
                    points, parameters.tolerance, CurveType.BSpline, parameters.importanceThreshold, weights),
                    
                CompressionMethod.RDP_Bezier => RDPAlgorithm.CompressWithCurveEvaluation(
                    points, parameters.tolerance, CurveType.Bezier, parameters.importanceThreshold, weights),
                    
                CompressionMethod.BSpline_Direct => BSplineAlgorithm.Compress(points, parameters.tolerance),
                
                CompressionMethod.Bezier_Direct => BezierAlgorithm.Compress(points, parameters.tolerance),
                
                _ => BezierAlgorithm.Compress(points, parameters.tolerance) // デフォルト
            };
        }
        
        /// <summary>
        /// 指定された圧縮手法で圧縮を実行（シンプルインターフェース）
        /// </summary>
        public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
        {
            var parameters = new CompressionParams { tolerance = tolerance };
            return Compress(points, parameters);
        }
        
        public static ImportanceWeights GetOptimalWeights(CompressionDataType dataType, ImportanceWeights userWeights)
        {
            return dataType switch
            {
                CompressionDataType.Animation => ImportanceWeights.ForAnimation,
                CompressionDataType.SensorData => ImportanceWeights.ForSensorData,
                CompressionDataType.FinancialData => ImportanceWeights.ForFinancialData,
                _ => userWeights ?? ImportanceWeights.Default
            };
        }
        
        private static float EvaluateQuality(TimeValuePair[] original, TimeValuePair[] compressed)
        {
            if (compressed.Length == 0) return float.MaxValue;
            
            float totalError = 0f;
            float compressionPenalty = (float)compressed.Length / original.Length;
            
            for (int i = 0; i < original.Length; i++)
            {
                float interpolatedValue = InterpolateValue(compressed, original[i].time);
                totalError += Mathf.Abs(original[i].value - interpolatedValue);
            }
            
            float avgError = totalError / original.Length;
            return avgError + compressionPenalty * 0.1f; // 圧縮率にペナルティを追加
        }
        
        private static float InterpolateValue(TimeValuePair[] data, float time)
        {
            return InterpolationUtils.LinearInterpolate(data, time);
        }
    }
}
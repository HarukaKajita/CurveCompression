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
        /// ハイブリッド圧縮を実行（従来互換性のため残存）
        /// </summary>
        public static TimeValuePair[] Compress(TimeValuePair[] points, CompressionParams parameters)
        {
            if (points.Length <= 2) return points;
            
            // データタイプに基づく重み設定の自動調整
            var weights = GetOptimalWeights(parameters.dataType, parameters.importanceWeights);
            
            // 1. RDPで重要なポイントを抽出
            var rdpResult = RDPAlgorithm.Simplify(points, parameters.tolerance, 
                parameters.importanceThreshold, weights);
            
            // 2. B-スプラインで滑らかな近似
            var splineResult = BSplineAlgorithm.ApproximateWithBSpline(points, 
                parameters.tolerance);
            
            // 3. 品質に基づく選択（固定ロジック）
            float rdpScore = EvaluateQuality(points, rdpResult);
            float splineScore = EvaluateQuality(points, splineResult);
            
            return rdpScore <= splineScore ? rdpResult : splineResult;
        }
        
        /// <summary>
        /// 指定された圧縮手法で圧縮を実行
        /// </summary>
        public static CompressedCurveData CompressAdvanced(TimeValuePair[] points, CompressionParams parameters)
        {
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
            if (data.Length == 0) return 0f;
            if (data.Length == 1) return data[0].value;
            
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
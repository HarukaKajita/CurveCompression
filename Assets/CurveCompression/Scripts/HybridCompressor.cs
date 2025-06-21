using System;
using System.Collections.Generic;
using UnityEngine;

namespace CurveCompression
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
        /// ハイブリッド圧縮を実行
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
                parameters.tolerance, parameters.maxSplineSegments);
            
            // 3. 適応的重み付けで最適解を選択
            return SelectOptimalResult(points, rdpResult, splineResult, parameters);
        }
        
        /// <summary>
        /// ハイブリッド圧縮を実行（新しいデータ構造を使用）
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
            
            // 1. B-スプライン近似
            var bsplineResult = BSplineAlgorithm.Compress(points, parameters.tolerance, parameters.maxSplineSegments);
            
            // 2. Bezier曲線近似
            var bezierResult = BezierAlgorithm.Compress(points, parameters.tolerance, parameters.maxSplineSegments);
            
            // 3. 品質評価に基づく選択
            return SelectOptimalCurveResult(points, bsplineResult, bezierResult, parameters);
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
        
        private static TimeValuePair[] SelectOptimalResult(TimeValuePair[] original, 
            TimeValuePair[] rdpResult, TimeValuePair[] splineResult, CompressionParams parameters)
        {
            if (!parameters.enableHybrid)
            {
                return parameters.adaptiveWeight < 0.5f ? rdpResult : splineResult;
            }
            
            // 各結果の品質評価
            float rdpScore = EvaluateQuality(original, rdpResult);
            float splineScore = EvaluateQuality(original, splineResult);
            
            // 適応的選択またはブレンド
            if (parameters.adaptiveWeight == 0.0f) return rdpResult;
            if (parameters.adaptiveWeight == 1.0f) return splineResult;
            
            // 品質に基づく動的選択
            float qualityRatio = rdpScore / (rdpScore + splineScore);
            float finalWeight = Mathf.Lerp(qualityRatio, parameters.adaptiveWeight, 0.5f);
            
            return finalWeight < 0.5f ? rdpResult : splineResult;
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
        
        private static CompressedCurveData SelectOptimalCurveResult(TimeValuePair[] original, 
            CompressedCurveData bsplineResult, CompressedCurveData bezierResult, CompressionParams parameters)
        {
            if (!parameters.enableHybrid)
            {
                return parameters.adaptiveWeight < 0.5f ? bsplineResult : bezierResult;
            }
            
            // 各結果の品質評価
            float bsplineScore = EvaluateCurveQuality(original, bsplineResult);
            float bezierScore = EvaluateCurveQuality(original, bezierResult);
            
            // 適応的選択
            if (parameters.adaptiveWeight == 0.0f) return bsplineResult;
            if (parameters.adaptiveWeight == 1.0f) return bezierResult;
            
            // 品質に基づく動的選択
            return bsplineScore <= bezierScore ? bsplineResult : bezierResult;
        }
        
        private static float EvaluateCurveQuality(TimeValuePair[] original, CompressedCurveData compressed)
        {
            if (compressed.segments == null || compressed.segments.Length == 0) return float.MaxValue;
            
            float totalError = 0f;
            float compressionPenalty = (float)compressed.segments.Length / original.Length;
            
            for (int i = 0; i < original.Length; i++)
            {
                float curveValue = compressed.Evaluate(original[i].time);
                totalError += Mathf.Abs(original[i].value - curveValue);
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
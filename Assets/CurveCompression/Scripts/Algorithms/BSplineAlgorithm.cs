using System.Collections.Generic;
using UnityEngine;
using CurveCompression.DataStructures;
using CurveCompression.Core;

namespace CurveCompression.Algorithms
{
	/// <summary>
    /// 適応的B-スプラインアルゴリズム
    /// </summary>
    public static class BSplineAlgorithm
    {
        /// <summary>
        /// 適応的B-スプライン近似（標準インターフェース）
        /// </summary>
        public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
        {
            ValidationUtils.ValidatePoints(points, nameof(points));
            ValidationUtils.ValidateCompressionParams(parameters);
            
            return Compress(points, parameters.tolerance);
        }
        
        /// <summary>
        /// 適応的B-スプライン近似（シンプルインターフェース）
        /// </summary>
        public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
        {
            ValidationUtils.ValidatePoints(points, nameof(points));
            ValidationUtils.ValidateTolerance(tolerance, nameof(tolerance));
            
            if (points.Length <= 2)
            {
                var linearSegment = CurveSegment.CreateLinear(
                    points[0].time, points[0].value,
                    points[^1].time, points[^1].value
                );
                return new CompressedCurveData(new[] { linearSegment });
            }
            
            var segments = new List<CurveSegment>();
            AdaptiveSegmentation(points, 0, points.Length - 1, tolerance, segments);
            
            return new CompressedCurveData(segments.ToArray());
        }
        
        private static void AdaptiveSegmentation(TimeValuePair[] points, int start, int end, 
            float tolerance, List<CurveSegment> segments)
        {
            if (end - start <= 3)
            {
                // 最小セグメントサイズに達したら線形補間
                segments.Add(CurveSegment.CreateLinear(
                    points[start].time, points[start].value,
                    points[end].time, points[end].value
                ));
                return;
            }
            
            // B-スプラインでフィット
            var segment = FitBSpline(points, start, end);
            float maxError = CalculateMaxError(points, start, end, segment);
            
            if (maxError <= tolerance)
            {
                segments.Add(segment);
            }
            else
            {
                // セグメントを分割
                int mid = (start + end) / 2;
                AdaptiveSegmentation(points, start, mid, tolerance, segments);
                AdaptiveSegmentation(points, mid, end, tolerance, segments);
            }
        }
        
        private static CurveSegment FitBSpline(TimeValuePair[] points, int start, int end)
        {
            int count = end - start + 1;
            if (count < 4)
            {
                return CurveSegment.CreateLinear(
                    points[start].time, points[start].value,
                    points[end].time, points[end].value
                );
            }
            
            // コントロールポイントの計算（簡略化版）
            Vector2[] controlPoints = new Vector2[4];
            controlPoints[0] = new Vector2(points[start].time, points[start].value);
            controlPoints[3] = new Vector2(points[end].time, points[end].value);
            
            // 中間コントロールポイントを推定
            int mid1 = start + count / 3;
            int mid2 = start + 2 * count / 3;
            controlPoints[1] = new Vector2(points[mid1].time, points[mid1].value);
            controlPoints[2] = new Vector2(points[mid2].time, points[mid2].value);
            
            return CurveSegment.CreateBSpline(controlPoints);
        }
        
        private static float CalculateMaxError(TimeValuePair[] points, int start, int end, CurveSegment segment)
        {
            float maxError = 0f;
            
            for (int i = start; i <= end; i++)
            {
                float segmentValue = segment.Evaluate(points[i].time);
                float error = Mathf.Abs(points[i].value - segmentValue);
                maxError = Mathf.Max(maxError, error);
            }
            
            return maxError;
        }
        
        /// <summary>
        /// 固定数のコントロールポイントでB-スプライン近似
        /// </summary>
        public static TimeValuePair[] ApproximateWithFixedPoints(TimeValuePair[] points, int numControlPoints)
        {
            ValidationUtils.ValidatePoints(points, nameof(points));
            ValidationUtils.ValidateControlPointCount(numControlPoints, points.Length, nameof(numControlPoints));
            
            if (points.Length <= 2 || numControlPoints >= points.Length)
                return points;
            
            // コントロールポイントのインデックスを均等に配置
            var controlIndices = new int[numControlPoints];
            for (int i = 0; i < numControlPoints; i++)
            {
                float t = (float)i / (numControlPoints - 1);
                controlIndices[i] = Mathf.RoundToInt(t * (points.Length - 1));
            }
            
            // コントロールポイントを抽出
            var result = new TimeValuePair[numControlPoints];
            for (int i = 0; i < numControlPoints; i++)
            {
                result[i] = points[controlIndices[i]];
            }
            
            // より正確な近似のため、最小二乗法を適用（簡略版）
            OptimizeControlPoints(points, result);
            
            return result;
        }
        
        /// <summary>
        /// コントロールポイントを最適化（簡略版）
        /// </summary>
        private static void OptimizeControlPoints(TimeValuePair[] originalPoints, TimeValuePair[] controlPoints)
        {
            // 簡単な最適化: 各コントロールポイント周辺のデータの平均値を使用
            int windowSize = originalPoints.Length / (controlPoints.Length * 2);
            windowSize = Mathf.Max(1, windowSize);
            
            for (int i = 1; i < controlPoints.Length - 1; i++) // 端点は固定
            {
                float sumTime = 0;
                float sumValue = 0;
                int count = 0;
                
                // 元のデータから対応する範囲を見つける
                float targetTime = controlPoints[i].time;
                
                for (int j = 0; j < originalPoints.Length; j++)
                {
                    if (Mathf.Abs(originalPoints[j].time - targetTime) < windowSize * (originalPoints[1].time - originalPoints[0].time))
                    {
                        sumTime += originalPoints[j].time;
                        sumValue += originalPoints[j].value;
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    controlPoints[i] = new TimeValuePair(sumTime / count, sumValue / count);
                }
            }
        }
    }
}
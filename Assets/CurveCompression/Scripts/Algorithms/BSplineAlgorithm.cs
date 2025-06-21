using System.Collections.Generic;
using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Algorithms
{
	/// <summary>
    /// 適応的B-スプラインアルゴリズム
    /// </summary>
    public static class BSplineAlgorithm
    {
        /// <summary>
        /// 適応的B-スプライン近似
        /// </summary>
        public static TimeValuePair[] ApproximateWithBSpline(TimeValuePair[] points, float tolerance)
        {
            if (points.Length <= 2) return points;
            
            var segments = new List<BSplineSegment>();
            AdaptiveSegmentation(points, 0, points.Length - 1, tolerance, segments);
            
            return ConvertSegmentsToPoints(segments, points[0].time, points[^1].time);
        }
        
        /// <summary>
        /// 適応的B-スプライン近似（新しいデータ構造を使用）
        /// </summary>
        public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
        {
            if (points.Length <= 2)
            {
                var linearSegment = CurveSegment.CreateLinear(
                    points[0].time, points[0].value,
                    points[^1].time, points[^1].value
                );
                return new CompressedCurveData(new[] { linearSegment });
            }
            
            var segments = new List<CurveSegment>();
            AdaptiveSegmentationNew(points, 0, points.Length - 1, tolerance, segments);
            
            return new CompressedCurveData(segments.ToArray());
        }
        
        private static void AdaptiveSegmentation(TimeValuePair[] points, int start, int end, 
            float tolerance, List<BSplineSegment> segments)
        {
            if (end - start <= 3)
            {
                // 最小セグメントサイズに達したら線形補間
                segments.Add(new BSplineSegment(points[start], points[end]));
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
        
        private static BSplineSegment FitBSpline(TimeValuePair[] points, int start, int end)
        {
            int count = end - start + 1;
            if (count < 4)
            {
                return new BSplineSegment(points[start], points[end]);
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
            
            return new BSplineSegment(controlPoints);
        }
        
        private static float CalculateMaxError(TimeValuePair[] points, int start, int end, BSplineSegment segment)
        {
            float maxError = 0f;
            
            for (int i = start; i <= end; i++)
            {
                float t = (points[i].time - points[start].time) / (points[end].time - points[start].time);
                float splineValue = segment.Evaluate(t);
                float error = Mathf.Abs(points[i].value - splineValue);
                maxError = Mathf.Max(maxError, error);
            }
            
            return maxError;
        }
        
        private static void AdaptiveSegmentationNew(TimeValuePair[] points, int start, int end, 
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
            var segment = FitBSplineNew(points, start, end);
            float maxError = CalculateMaxErrorNew(points, start, end, segment);
            
            if (maxError <= tolerance)
            {
                segments.Add(segment);
            }
            else
            {
                // セグメントを分割
                int mid = (start + end) / 2;
                AdaptiveSegmentationNew(points, start, mid, tolerance, segments);
                AdaptiveSegmentationNew(points, mid, end, tolerance, segments);
            }
        }
        
        private static CurveSegment FitBSplineNew(TimeValuePair[] points, int start, int end)
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
        
        private static float CalculateMaxErrorNew(TimeValuePair[] points, int start, int end, CurveSegment segment)
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
        
        private static TimeValuePair[] ConvertSegmentsToPoints(List<BSplineSegment> segments, float startTime, float endTime)
        {
            var result = new List<TimeValuePair>();
            
            foreach (var segment in segments)
            {
                result.Add(new TimeValuePair(segment.StartTime, segment.StartValue));
            }
            
            if (segments.Count > 0)
            {
                var lastSegment = segments[^1];
                result.Add(new TimeValuePair(lastSegment.EndTime, lastSegment.EndValue));
            }
            
            return result.ToArray();
        }
        
        /// <summary>
        /// 固定数のコントロールポイントでB-スプライン近似
        /// </summary>
        public static TimeValuePair[] ApproximateWithFixedPoints(TimeValuePair[] points, int numControlPoints)
        {
            if (points.Length <= 2 || numControlPoints >= points.Length)
                return points;
                
            if (numControlPoints < 2)
                numControlPoints = 2;
            
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
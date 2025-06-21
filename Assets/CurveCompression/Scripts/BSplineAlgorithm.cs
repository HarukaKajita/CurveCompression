using System.Collections.Generic;
using UnityEngine;

namespace CurveCompression
{
	/// <summary>
    /// 適応的B-スプラインアルゴリズム
    /// </summary>
    public static class BSplineAlgorithm
    {
        /// <summary>
        /// 適応的B-スプライン近似
        /// </summary>
        public static TimeValuePair[] ApproximateWithBSpline(TimeValuePair[] points, float tolerance, int maxSegments)
        {
            if (points.Length <= 2) return points;
            
            var segments = new List<BSplineSegment>();
            AdaptiveSegmentation(points, 0, points.Length - 1, tolerance, maxSegments, segments);
            
            return ConvertSegmentsToPoints(segments, points[0].time, points[^1].time);
        }
        
        private static void AdaptiveSegmentation(TimeValuePair[] points, int start, int end, 
            float tolerance, int maxSegments, List<BSplineSegment> segments)
        {
            if (segments.Count >= maxSegments || end - start <= 3)
            {
                // 線形補間で近似
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
                AdaptiveSegmentation(points, start, mid, tolerance, maxSegments, segments);
                AdaptiveSegmentation(points, mid, end, tolerance, maxSegments, segments);
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
    }
}
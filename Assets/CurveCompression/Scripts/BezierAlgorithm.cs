using System.Collections.Generic;
using UnityEngine;

namespace CurveCompression
{
    /// <summary>
    /// Bezier曲線による適応的圧縮アルゴリズム
    /// Unity AnimationCurveと互換性を持つ
    /// </summary>
    public static class BezierAlgorithm
    {
        /// <summary>
        /// 適応的Bezier曲線近似
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
            AdaptiveBezierSegmentation(points, 0, points.Length - 1, tolerance, segments);
            
            return new CompressedCurveData(segments.ToArray());
        }
        
        /// <summary>
        /// TimeValuePairからAnimationCurveを作成
        /// </summary>
        public static AnimationCurve ToAnimationCurve(TimeValuePair[] points)
        {
            if (points == null || points.Length == 0)
                return new AnimationCurve();
                
            var curve = new AnimationCurve();
            
            for (int i = 0; i < points.Length; i++)
            {
                float inTangent = 0f;
                float outTangent = 0f;
                
                // タンジェントの計算
                if (i > 0 && i < points.Length - 1)
                {
                    // 中点の場合、前後の点から傾きを計算
                    float prevSlope = (points[i].value - points[i - 1].value) / (points[i].time - points[i - 1].time);
                    float nextSlope = (points[i + 1].value - points[i].value) / (points[i + 1].time - points[i].time);
                    
                    // 平均を取って滑らかにする
                    float avgSlope = (prevSlope + nextSlope) * 0.5f;
                    inTangent = avgSlope;
                    outTangent = avgSlope;
                }
                else if (i == 0 && points.Length > 1)
                {
                    // 最初の点
                    outTangent = (points[1].value - points[0].value) / (points[1].time - points[0].time);
                    inTangent = outTangent;
                }
                else if (i == points.Length - 1 && points.Length > 1)
                {
                    // 最後の点
                    inTangent = (points[i].value - points[i - 1].value) / (points[i].time - points[i - 1].time);
                    outTangent = inTangent;
                }
                
                curve.AddKey(new Keyframe(points[i].time, points[i].value, inTangent, outTangent));
            }
            
            return curve;
        }
        
        /// <summary>
        /// CompressedCurveDataからAnimationCurveを作成
        /// </summary>
        public static AnimationCurve ToAnimationCurve(CompressedCurveData curveData)
        {
            if (curveData?.segments == null || curveData.segments.Length == 0)
                return new AnimationCurve();
                
            var curve = new AnimationCurve();
            
            for (int i = 0; i < curveData.segments.Length; i++)
            {
                var segment = curveData.segments[i];
                
                if (segment.curveType == CurveType.Bezier)
                {
                    // ベジェセグメントの場合
                    curve.AddKey(new Keyframe(segment.startTime, segment.startValue, segment.inTangent, segment.outTangent));
                    
                    // 最後のセグメントの場合、終点も追加
                    if (i == curveData.segments.Length - 1)
                    {
                        curve.AddKey(new Keyframe(segment.endTime, segment.endValue, segment.inTangent, segment.outTangent));
                    }
                }
                else
                {
                    // 線形セグメントの場合
                    float tangent = (segment.endValue - segment.startValue) / (segment.endTime - segment.startTime);
                    curve.AddKey(new Keyframe(segment.startTime, segment.startValue, tangent, tangent));
                    
                    if (i == curveData.segments.Length - 1)
                    {
                        curve.AddKey(new Keyframe(segment.endTime, segment.endValue, tangent, tangent));
                    }
                }
            }
            
            return curve;
        }
        
        /// <summary>
        /// AnimationCurveからCompressedCurveDataを作成
        /// </summary>
        public static CompressedCurveData FromAnimationCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return new CompressedCurveData(new CurveSegment[0]);
                
            var segments = new List<CurveSegment>();
            
            for (int i = 0; i < curve.length - 1; i++)
            {
                var keyframe1 = curve[i];
                var keyframe2 = curve[i + 1];
                
                var segment = CurveSegment.CreateBezier(
                    keyframe1.time, keyframe1.value,
                    keyframe2.time, keyframe2.value,
                    keyframe1.outTangent, keyframe2.inTangent
                );
                
                segments.Add(segment);
            }
            
            return new CompressedCurveData(segments.ToArray());
        }
        
        private static void AdaptiveBezierSegmentation(TimeValuePair[] points, int start, int end, 
            float tolerance, List<CurveSegment> segments)
        {
            if (end - start <= 1)
            {
                // 最小セグメントサイズに達したら線形補間
                segments.Add(CurveSegment.CreateLinear(
                    points[start].time, points[start].value,
                    points[end].time, points[end].value
                ));
                return;
            }
            
            // ベジェ曲線でフィット
            var segment = FitBezierSegment(points, start, end);
            float maxError = CalculateMaxError(points, start, end, segment);
            
            if (maxError <= tolerance)
            {
                segments.Add(segment);
            }
            else
            {
                // セグメントを分割
                int mid = (start + end) / 2;
                AdaptiveBezierSegmentation(points, start, mid, tolerance, segments);
                AdaptiveBezierSegmentation(points, mid, end, tolerance, segments);
            }
        }
        
        private static CurveSegment FitBezierSegment(TimeValuePair[] points, int start, int end)
        {
            if (end - start <= 1)
            {
                return CurveSegment.CreateLinear(
                    points[start].time, points[start].value,
                    points[end].time, points[end].value
                );
            }
            
            float startTime = points[start].time;
            float endTime = points[end].time;
            float startValue = points[start].value;
            float endValue = points[end].value;
            
            // タンジェントの計算
            float inTangent = CalculateInTangent(points, start, end);
            float outTangent = CalculateOutTangent(points, start, end);
            
            return CurveSegment.CreateBezier(startTime, startValue, endTime, endValue, inTangent, outTangent);
        }
        
        private static float CalculateInTangent(TimeValuePair[] points, int start, int end)
        {
            if (end - start < 2) return 0f;
            
            // 最初の数点を使って傾きを推定
            int sampleCount = Mathf.Min(3, end - start);
            float totalWeight = 0f;
            float weightedSlope = 0f;
            
            for (int i = 0; i < sampleCount; i++)
            {
                int idx1 = start + i;
                int idx2 = start + i + 1;
                
                if (idx2 > end) break;
                
                float dt = points[idx2].time - points[idx1].time;
                if (dt > 0f)
                {
                    float slope = (points[idx2].value - points[idx1].value) / dt;
                    float weight = 1f / (i + 1); // 近い点により大きな重み
                    
                    weightedSlope += slope * weight;
                    totalWeight += weight;
                }
            }
            
            return totalWeight > 0f ? weightedSlope / totalWeight : 0f;
        }
        
        private static float CalculateOutTangent(TimeValuePair[] points, int start, int end)
        {
            if (end - start < 2) return 0f;
            
            // 最後の数点を使って傾きを推定
            int sampleCount = Mathf.Min(3, end - start);
            float totalWeight = 0f;
            float weightedSlope = 0f;
            
            for (int i = 0; i < sampleCount; i++)
            {
                int idx1 = end - i - 1;
                int idx2 = end - i;
                
                if (idx1 < start) break;
                
                float dt = points[idx2].time - points[idx1].time;
                if (dt > 0f)
                {
                    float slope = (points[idx2].value - points[idx1].value) / dt;
                    float weight = 1f / (i + 1); // 近い点により大きな重み
                    
                    weightedSlope += slope * weight;
                    totalWeight += weight;
                }
            }
            
            return totalWeight > 0f ? weightedSlope / totalWeight : 0f;
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
        /// 固定数のコントロールポイントでBezier曲線近似
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
            
            // タンジェントを最適化
            OptimizeBezierTangents(points, result);
            
            return result;
        }
        
        /// <summary>
        /// Bezierタンジェントを最適化
        /// </summary>
        private static void OptimizeBezierTangents(TimeValuePair[] originalPoints, TimeValuePair[] controlPoints)
        {
            // 各コントロールポイントでの最適なタンジェントを計算
            for (int i = 0; i < controlPoints.Length; i++)
            {
                float inTangent = 0f;
                float outTangent = 0f;
                
                if (i > 0 && i < controlPoints.Length - 1)
                {
                    // 中間点: 前後の点から傾きを計算
                    float prevSlope = (controlPoints[i].value - controlPoints[i - 1].value) / 
                                    (controlPoints[i].time - controlPoints[i - 1].time);
                    float nextSlope = (controlPoints[i + 1].value - controlPoints[i].value) / 
                                    (controlPoints[i + 1].time - controlPoints[i].time);
                    
                    // 平均を取って滑らかにする
                    float avgSlope = (prevSlope + nextSlope) * 0.5f;
                    inTangent = avgSlope;
                    outTangent = avgSlope;
                }
                else if (i == 0 && controlPoints.Length > 1)
                {
                    // 最初の点
                    outTangent = (controlPoints[1].value - controlPoints[0].value) / 
                               (controlPoints[1].time - controlPoints[0].time);
                    inTangent = outTangent;
                }
                else if (i == controlPoints.Length - 1 && controlPoints.Length > 1)
                {
                    // 最後の点
                    inTangent = (controlPoints[i].value - controlPoints[i - 1].value) / 
                              (controlPoints[i].time - controlPoints[i - 1].time);
                    outTangent = inTangent;
                }
                
                // タンジェント情報を保持（実際の使用時に適用）
                // 注: TimeValuePairにはタンジェント情報がないので、
                // 実際の実装では別の方法で保持する必要があります
            }
        }
    }
}
using System;
using UnityEngine;

namespace CurveCompression
{
    /// <summary>
    /// 曲線セグメントの種類
    /// </summary>
    public enum CurveType
    {
        Linear,     // 線形補間
        BSpline,    // B-スプライン曲線
        Bezier      // ベジェ曲線
    }

    /// <summary>
    /// 曲線セグメントを表すクラス
    /// </summary>
    [Serializable]
    public class CurveSegment
    {
        public CurveType curveType;
        public float startTime;
        public float endTime;
        
        // 線形補間用
        public float startValue;
        public float endValue;
        
        // B-スプライン用のコントロールポイント（4点制御）
        public Vector2[] bsplineControlPoints;
        
        // ベジェ曲線用のハンドル
        public float inTangent;   // 始点での接線
        public float outTangent;  // 終点での接線
        
        /// <summary>
        /// 線形セグメントの作成
        /// </summary>
        public static CurveSegment CreateLinear(float startTime, float startValue, float endTime, float endValue)
        {
            return new CurveSegment
            {
                curveType = CurveType.Linear,
                startTime = startTime,
                endTime = endTime,
                startValue = startValue,
                endValue = endValue
            };
        }
        
        /// <summary>
        /// B-スプラインセグメントの作成
        /// </summary>
        public static CurveSegment CreateBSpline(Vector2[] controlPoints)
        {
            if (controlPoints == null || controlPoints.Length < 2)
                throw new ArgumentException("B-スプラインには最低2つのコントロールポイントが必要です");
                
            return new CurveSegment
            {
                curveType = CurveType.BSpline,
                startTime = controlPoints[0].x,
                endTime = controlPoints[^1].x,
                startValue = controlPoints[0].y,
                endValue = controlPoints[^1].y,
                bsplineControlPoints = controlPoints
            };
        }
        
        /// <summary>
        /// ベジェセグメントの作成
        /// </summary>
        public static CurveSegment CreateBezier(float startTime, float startValue, float endTime, float endValue, 
            float inTangent, float outTangent)
        {
            return new CurveSegment
            {
                curveType = CurveType.Bezier,
                startTime = startTime,
                endTime = endTime,
                startValue = startValue,
                endValue = endValue,
                inTangent = inTangent,
                outTangent = outTangent
            };
        }
        
        /// <summary>
        /// 指定された時刻での値を評価
        /// </summary>
        public float Evaluate(float time)
        {
            if (time <= startTime) return startValue;
            if (time >= endTime) return endValue;
            
            float t = (time - startTime) / (endTime - startTime);
            
            switch (curveType)
            {
                case CurveType.Linear:
                    return Mathf.Lerp(startValue, endValue, t);
                    
                case CurveType.BSpline:
                    return EvaluateBSpline(t);
                    
                case CurveType.Bezier:
                    return EvaluateBezier(time, t);
                    
                default:
                    return startValue;
            }
        }
        
        private float EvaluateBSpline(float t)
        {
            if (bsplineControlPoints == null || bsplineControlPoints.Length < 2)
                return Mathf.Lerp(startValue, endValue, t);
                
            if (bsplineControlPoints.Length == 2)
                return Mathf.Lerp(bsplineControlPoints[0].y, bsplineControlPoints[1].y, t);
                
            // Cubic B-Spline evaluation
            if (bsplineControlPoints.Length >= 4)
            {
                float u = Mathf.Clamp01(t);
                float u2 = u * u;
                float u3 = u2 * u;
                
                float b0 = (1 - u) * (1 - u) * (1 - u) / 6.0f;
                float b1 = (3 * u3 - 6 * u2 + 4) / 6.0f;
                float b2 = (-3 * u3 + 3 * u2 + 3 * u + 1) / 6.0f;
                float b3 = u3 / 6.0f;
                
                return b0 * bsplineControlPoints[0].y + b1 * bsplineControlPoints[1].y +
                       b2 * bsplineControlPoints[2].y + b3 * bsplineControlPoints[3].y;
            }
            
            // 3点の場合は二次B-スプライン
            float t2 = t * t;
            float b0_2 = (1 - t) * (1 - t) / 2.0f;
            float b1_2 = (1 + 2 * t - 2 * t2) / 2.0f;
            float b2_2 = t2 / 2.0f;
            
            return b0_2 * bsplineControlPoints[0].y + b1_2 * bsplineControlPoints[1].y + 
                   b2_2 * bsplineControlPoints[2].y;
        }
        
        private float EvaluateBezier(float time, float t)
        {
            // Unity AnimationCurve互換のベジェ曲線評価
            float dt = endTime - startTime;
            float m0 = inTangent * dt;
            float m1 = outTangent * dt;
            
            float t2 = t * t;
            float t3 = t2 * t;
            
            float a = 2 * t3 - 3 * t2 + 1;
            float b = t3 - 2 * t2 + t;
            float c = t3 - t2;
            float d = -2 * t3 + 3 * t2;
            
            return a * startValue + b * m0 + c * m1 + d * endValue;
        }
    }
    
    /// <summary>
    /// 圧縮された曲線データ
    /// </summary>
    [Serializable]
    public class CompressedCurveData
    {
        public CurveSegment[] segments;
        public float totalDuration;
        
        public CompressedCurveData(CurveSegment[] segments)
        {
            this.segments = segments;
            if (segments != null && segments.Length > 0)
            {
                totalDuration = segments[^1].endTime - segments[0].startTime;
            }
        }
        
        /// <summary>
        /// 指定された時刻での値を評価
        /// </summary>
        public float Evaluate(float time)
        {
            if (segments == null || segments.Length == 0)
                return 0f;
                
            // 該当するセグメントを二分探索で見つける
            int left = 0;
            int right = segments.Length - 1;
            
            while (left <= right)
            {
                int mid = (left + right) / 2;
                
                if (time < segments[mid].startTime)
                {
                    right = mid - 1;
                }
                else if (time > segments[mid].endTime)
                {
                    left = mid + 1;
                }
                else
                {
                    return segments[mid].Evaluate(time);
                }
            }
            
            // セグメントの範囲外の場合
            if (time <= segments[0].startTime)
                return segments[0].startValue;
            else
                return segments[^1].endValue;
        }
        
        /// <summary>
        /// TimeValuePair配列に変換（サンプリング）
        /// </summary>
        public TimeValuePair[] ToTimeValuePairs(int sampleCount)
        {
            if (segments == null || segments.Length == 0 || sampleCount <= 0)
                return new TimeValuePair[0];
                
            var result = new TimeValuePair[sampleCount];
            float startTime = segments[0].startTime;
            float endTime = segments[^1].endTime;
            float timeStep = (endTime - startTime) / (sampleCount - 1);
            
            for (int i = 0; i < sampleCount; i++)
            {
                float time = startTime + i * timeStep;
                result[i] = new TimeValuePair(time, Evaluate(time));
            }
            
            return result;
        }
    }
}
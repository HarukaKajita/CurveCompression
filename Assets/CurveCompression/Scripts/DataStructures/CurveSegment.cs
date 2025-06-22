using System;
using UnityEngine;
using CurveCompression.Core;

namespace CurveCompression.DataStructures
{
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
            ValidationUtils.ValidateControlPoints(controlPoints, nameof(controlPoints));
                
            return new CurveSegment
            {
                curveType = CurveType.BSpline,
                startTime = controlPoints[0].x,
                endTime = controlPoints[controlPoints.Length - 1].x,
                bsplineControlPoints = controlPoints,
                startValue = controlPoints[0].y,
                endValue = controlPoints[controlPoints.Length - 1].y
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
        /// 指定した時間での値を評価
        /// </summary>
        public float Evaluate(float time)
        {
            switch (curveType)
            {
                case CurveType.Linear:
                    return EvaluateLinear(time);
                    
                case CurveType.BSpline:
                    return EvaluateBSpline(time);
                    
                case CurveType.Bezier:
                    return EvaluateBezier(time);
                    
                default:
                    return 0f;
            }
        }
        
        private float EvaluateLinear(float time)
        {
            if (time <= startTime) return startValue;
            if (time >= endTime) return endValue;
            
            float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
            return Mathf.Lerp(startValue, endValue, t);
        }
        
        private float EvaluateBSpline(float time)
        {
            if (bsplineControlPoints == null || bsplineControlPoints.Length < 2)
                return EvaluateLinear(time);
                
            if (time <= startTime) return startValue;
            if (time >= endTime) return endValue;
            
            // 簡略化されたB-スプライン評価（実際には適切なB-スプライン補間を実装する必要があります）
            float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
            
            if (bsplineControlPoints.Length == 2)
            {
                // 2点の場合は線形補間
                return Mathf.Lerp(bsplineControlPoints[0].y, bsplineControlPoints[1].y, t);
            }
            else if (bsplineControlPoints.Length == 4)
            {
                // 4点制御の3次B-スプライン
                float t2 = t * t;
                float t3 = t2 * t;
                
                float b0 = (1 - t) * (1 - t) * (1 - t) / 6f;
                float b1 = (3 * t3 - 6 * t2 + 4) / 6f;
                float b2 = (-3 * t3 + 3 * t2 + 3 * t + 1) / 6f;
                float b3 = t3 / 6f;
                
                return b0 * bsplineControlPoints[0].y + 
                       b1 * bsplineControlPoints[1].y + 
                       b2 * bsplineControlPoints[2].y + 
                       b3 * bsplineControlPoints[3].y;
            }
            else
            {
                // その他の場合は線形補間で近似
                int segmentCount = bsplineControlPoints.Length - 1;
                float segmentT = t * segmentCount;
                int segmentIndex = Mathf.FloorToInt(segmentT);
                segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);
                
                float localT = segmentT - segmentIndex;
                return Mathf.Lerp(bsplineControlPoints[segmentIndex].y, 
                                 bsplineControlPoints[segmentIndex + 1].y, localT);
            }
        }
        
        private float EvaluateBezier(float time)
        {
            if (time <= startTime) return startValue;
            if (time >= endTime) return endValue;
            
            float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
            float t2 = t * t;
            float t3 = t2 * t;
            
            // 3次ベジェ曲線（エルミート形式）
            float h1 = 2 * t3 - 3 * t2 + 1;      // 始点の値の影響
            float h2 = -2 * t3 + 3 * t2;         // 終点の値の影響
            float h3 = t3 - 2 * t2 + t;          // 始点の接線の影響
            float h4 = t3 - t2;                  // 終点の接線の影響
            
            float dt = endTime - startTime;
            
            return h1 * startValue + 
                   h2 * endValue + 
                   h3 * inTangent * dt + 
                   h4 * outTangent * dt;
        }
    }
}
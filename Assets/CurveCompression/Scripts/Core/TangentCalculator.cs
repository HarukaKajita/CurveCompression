using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Core
{
    /// <summary>
    /// タンジェント計算ユーティリティ
    /// </summary>
    public static class TangentCalculator
    {
        public enum TangentMode
        {
            Linear,         // 単純な線形補間
            Smooth,         // 平滑化（デフォルト）
            CatmullRom,     // Catmull-Rom スプライン
            Cardinal        // Cardinal スプライン（テンション付き）
        }
        
        /// <summary>
        /// 点列に対して滑らかなタンジェントを計算
        /// </summary>
        public static float[] CalculateSmoothTangents(TimeValuePair[] points, TangentMode mode = TangentMode.Smooth, float tension = 0.5f)
        {
            if (points == null || points.Length == 0)
                return new float[0];
                
            float[] tangents = new float[points.Length];
            
            for (int i = 0; i < points.Length; i++)
            {
                tangents[i] = CalculateTangentAtPoint(points, i, mode, tension);
            }
            
            return tangents;
        }
        
        /// <summary>
        /// 特定の点でのタンジェントを計算
        /// </summary>
        private static float CalculateTangentAtPoint(TimeValuePair[] points, int index, TangentMode mode, float tension)
        {
            if (points.Length < 2)
                return 0f;
                
            switch (mode)
            {
                case TangentMode.Linear:
                    return CalculateLinearTangent(points, index);
                    
                case TangentMode.CatmullRom:
                    return CalculateCatmullRomTangent(points, index);
                    
                case TangentMode.Cardinal:
                    return CalculateCardinalTangent(points, index, tension);
                    
                case TangentMode.Smooth:
                default:
                    return CalculateSmoothTangent(points, index);
            }
        }
        
        /// <summary>
        /// 線形タンジェント（単純な傾き）
        /// </summary>
        private static float CalculateLinearTangent(TimeValuePair[] points, int index)
        {
            if (index == 0)
            {
                // 最初の点：前方差分
                return (points[1].value - points[0].value) / (points[1].time - points[0].time);
            }
            else if (index == points.Length - 1)
            {
                // 最後の点：後方差分
                return (points[index].value - points[index - 1].value) / 
                       (points[index].time - points[index - 1].time);
            }
            else
            {
                // 中間点：次の点への傾き
                return (points[index + 1].value - points[index].value) / 
                       (points[index + 1].time - points[index].time);
            }
        }
        
        /// <summary>
        /// 平滑化タンジェント（前後の傾きの加重平均）
        /// </summary>
        private static float CalculateSmoothTangent(TimeValuePair[] points, int index)
        {
            if (index == 0)
            {
                // 最初の点：前方差分
                return (points[1].value - points[0].value) / (points[1].time - points[0].time);
            }
            else if (index == points.Length - 1)
            {
                // 最後の点：後方差分
                return (points[index].value - points[index - 1].value) / 
                       (points[index].time - points[index - 1].time);
            }
            else
            {
                // 中間点：前後の傾きの加重平均
                float prevSlope = (points[index].value - points[index - 1].value) / 
                                 (points[index].time - points[index - 1].time);
                float nextSlope = (points[index + 1].value - points[index].value) / 
                                 (points[index + 1].time - points[index].time);
                
                // 時間間隔に基づく重み付け
                float prevDt = points[index].time - points[index - 1].time;
                float nextDt = points[index + 1].time - points[index].time;
                float totalDt = prevDt + nextDt;
                
                // 距離に反比例する重み（近い点により大きな影響）
                float prevWeight = nextDt / totalDt;
                float nextWeight = prevDt / totalDt;
                
                return prevSlope * prevWeight + nextSlope * nextWeight;
            }
        }
        
        /// <summary>
        /// Catmull-Romタンジェント（テンション = 0）
        /// </summary>
        private static float CalculateCatmullRomTangent(TimeValuePair[] points, int index)
        {
            if (index == 0 || index == points.Length - 1)
            {
                // 端点は線形タンジェントを使用
                return CalculateLinearTangent(points, index);
            }
            
            // Catmull-Rom: tangent = (P[i+1] - P[i-1]) / (t[i+1] - t[i-1])
            return (points[index + 1].value - points[index - 1].value) / 
                   (points[index + 1].time - points[index - 1].time);
        }
        
        /// <summary>
        /// Cardinalタンジェント（調整可能なテンション）
        /// </summary>
        private static float CalculateCardinalTangent(TimeValuePair[] points, int index, float tension)
        {
            if (index == 0 || index == points.Length - 1)
            {
                // 端点は線形タンジェントを使用
                return CalculateLinearTangent(points, index);
            }
            
            // Cardinal: tangent = (1 - tension) * (P[i+1] - P[i-1]) / (t[i+1] - t[i-1])
            float catmullRomTangent = (points[index + 1].value - points[index - 1].value) / 
                                     (points[index + 1].time - points[index - 1].time);
            
            return (1f - tension) * catmullRomTangent;
        }
        
        /// <summary>
        /// タンジェントの連続性を保証（オプション）
        /// </summary>
        public static void EnsureTangentContinuity(float[] tangents, TimeValuePair[] points)
        {
            if (tangents.Length != points.Length)
                return;
                
            // 各内部点で、前のセグメントの終了タンジェントと
            // 次のセグメントの開始タンジェントを平均化
            for (int i = 1; i < tangents.Length - 1; i++)
            {
                float prevSegmentEndTangent = tangents[i];
                float nextSegmentStartTangent = tangents[i];
                
                // すでに同じ値なので、この実装では変更不要
                // より複雑な実装では、ここで調整が必要な場合がある
            }
        }
    }
}
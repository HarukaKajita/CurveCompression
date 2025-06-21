using System.Collections.Generic;
using UnityEngine;

namespace CurveCompression
{
    // =============================================================================
    // RDPAlgorithm.cs - Ramer-Douglas-Peucker アルゴリズム実装
    // =============================================================================
	/// <summary>
    /// 最適化されたRamer-Douglas-Peuckerアルゴリズム
    /// </summary>
    public static class RDPAlgorithm
    {
        /// <summary>
        /// RDPアルゴリズムによる点列簡約
        /// </summary>
        public static TimeValuePair[] Simplify(TimeValuePair[] points, float tolerance, 
            float importanceThreshold = 1.0f, ImportanceWeights weights = null)
        {
            if (points.Length <= 2) return points;
            
            var result = new List<TimeValuePair>();
            SimplifyRecursive(points, 0, points.Length - 1, tolerance, importanceThreshold, weights, result);
            
            // 結果をソートして返す
            result.Sort();
            return result.ToArray();
        }
        
        private static void SimplifyRecursive(TimeValuePair[] points, int start, int end, 
            float tolerance, float importanceThreshold, ImportanceWeights weights, List<TimeValuePair> result)
        {
            if (end - start <= 1)
            {
                if (!result.Contains(points[start]))
                    result.Add(points[start]);
                if (!result.Contains(points[end]))
                    result.Add(points[end]);
                return;
            }
            
            float maxDistance = 0f;
            int maxIndex = start;
            
            // 最大距離の点を見つける
            for (int i = start + 1; i < end; i++)
            {
                float distance = PerpendicularDistance(points[i], points[start], points[end]);
                
                // 重要度に基づく重み付け
                float importance = CalculateImportance(points, i, weights);
                distance *= (1.0f + importance * importanceThreshold);
                
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }
            
            // 許容範囲内なら線分で近似
            if (maxDistance <= tolerance)
            {
                if (!result.Contains(points[start]))
                    result.Add(points[start]);
                if (!result.Contains(points[end]))
                    result.Add(points[end]);
            }
            else
            {
                // 再帰的に処理
                SimplifyRecursive(points, start, maxIndex, tolerance, importanceThreshold, weights, result);
                SimplifyRecursive(points, maxIndex, end, tolerance, importanceThreshold, weights, result);
            }
        }
        
        /// <summary>
        /// 点から線分への垂直距離を計算
        /// </summary>
        private static float PerpendicularDistance(TimeValuePair point, TimeValuePair lineStart, TimeValuePair lineEnd)
        {
            float dx = lineEnd.time - lineStart.time;
            float dy = lineEnd.value - lineStart.value;
            
            if (dx == 0 && dy == 0)
                return Vector2.Distance(new Vector2(point.time, point.value), new Vector2(lineStart.time, lineStart.value));
            
            float t = ((point.time - lineStart.time) * dx + (point.value - lineStart.value) * dy) / (dx * dx + dy * dy);
            t = Mathf.Clamp01(t);
            
            Vector2 projection = new Vector2(lineStart.time + t * dx, lineStart.value + t * dy);
            return Vector2.Distance(new Vector2(point.time, point.value), projection);
        }
        
        /// <summary>
        /// ポイントの重要度を計算（複数の指標に基づく適応的重み付け）
        /// </summary>
        private static float CalculateImportance(TimeValuePair[] points, int index, ImportanceWeights weights = null)
        {
            if (index <= 0 || index >= points.Length - 1) return 1.0f;
            
            weights = weights ?? ImportanceWeights.Default;
            
            Vector2 prev = new Vector2(points[index - 1].time, points[index - 1].value);
            Vector2 curr = new Vector2(points[index].time, points[index].value);
            Vector2 next = new Vector2(points[index + 1].time, points[index + 1].value);
            
            // 1. 曲率の計算
            float curvature = CalculateCurvature(prev, curr, next);
            
            // 2. 正規化された変化率
            float changeRate = CalculateNormalizedChangeRate(points, index);
            
            // 3. 局所的分散（データの不規則性）
            float localVariance = CalculateLocalVariance(points, index);
            
            // 4. 極値検出（ピーク・谷の重要度）
            float extremeValue = CalculateExtremeValue(points, index);
            
            // 5. 適応的重み付け
            return weights.curvature * curvature +
                   weights.changeRate * changeRate +
                   weights.localVariance * localVariance +
                   weights.extremeValue * extremeValue;
        }
        
        private static float CalculateCurvature(Vector2 prev, Vector2 curr, Vector2 next)
        {
            Vector2 v1 = (curr - prev).normalized;
            Vector2 v2 = (next - curr).normalized;
            
            // より安定した曲率計算
            float dot = Vector2.Dot(v1, v2);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
            return angle / Mathf.PI; // 0-1に正規化
        }
        
        private static float CalculateNormalizedChangeRate(TimeValuePair[] points, int index)
        {
            // データ全体の値の範囲を考慮した正規化
            float valueRange = GetValueRange(points);
            if (valueRange == 0) return 0f;
            
            float changeRate = Mathf.Abs(points[index + 1].value - points[index - 1].value) / 
                              (points[index + 1].time - points[index - 1].time);
            
            return Mathf.Clamp01(changeRate / valueRange);
        }
        
        private static float CalculateLocalVariance(TimeValuePair[] points, int index)
        {
            // 近傍ポイントでの分散を計算
            int windowSize = Mathf.Min(5, points.Length / 10); // 適応的ウィンドウサイズ
            int start = Mathf.Max(0, index - windowSize);
            int end = Mathf.Min(points.Length - 1, index + windowSize);
            
            float mean = 0f;
            int count = end - start + 1;
            
            for (int i = start; i <= end; i++)
            {
                mean += points[i].value;
            }
            mean /= count;
            
            float variance = 0f;
            for (int i = start; i <= end; i++)
            {
                variance += (points[i].value - mean) * (points[i].value - mean);
            }
            
            float globalVariance = GetGlobalVariance(points);
            return globalVariance > 0 ? Mathf.Clamp01((variance / count) / globalVariance) : 0f;
        }
        
        private static float CalculateExtremeValue(TimeValuePair[] points, int index)
        {
            // 局所的な極値の検出
            float prevValue = points[index - 1].value;
            float currValue = points[index].value;
            float nextValue = points[index + 1].value;
            
            bool isLocalMax = currValue > prevValue && currValue > nextValue;
            bool isLocalMin = currValue < prevValue && currValue < nextValue;
            
            if (isLocalMax || isLocalMin)
            {
                // 極値の顕著性を計算
                float prominence = Mathf.Min(Mathf.Abs(currValue - prevValue), 
                                           Mathf.Abs(currValue - nextValue));
                float valueRange = GetValueRange(points);
                return valueRange > 0 ? Mathf.Clamp01(prominence / valueRange) : 0f;
            }
            
            return 0f;
        }
        
        private static float GetValueRange(TimeValuePair[] points)
        {
            if (points.Length == 0) return 0f;
            
            float min = points[0].value;
            float max = points[0].value;
            
            for (int i = 1; i < points.Length; i++)
            {
                min = Mathf.Min(min, points[i].value);
                max = Mathf.Max(max, points[i].value);
            }
            
            return max - min;
        }
        
        private static float GetGlobalVariance(TimeValuePair[] points)
        {
            if (points.Length <= 1) return 0f;
            
            float mean = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                mean += points[i].value;
            }
            mean /= points.Length;
            
            float variance = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                variance += (points[i].value - mean) * (points[i].value - mean);
            }
            
            return variance / points.Length;
        }
    }
}
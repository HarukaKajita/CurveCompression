using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Core
{
    /// <summary>
    /// 補間処理のユーティリティクラス
    /// </summary>
    public static class InterpolationUtils
    {
        /// <summary>
        /// TimeValuePair配列から指定時間での値を線形補間で取得
        /// </summary>
        /// <param name="data">データ配列（時間順でソート済みであることを前提）</param>
        /// <param name="time">補間したい時間</param>
        /// <returns>補間された値</returns>
        public static float LinearInterpolate(TimeValuePair[] data, float time)
        {
            ValidationUtils.ValidatePoints(data, nameof(data), 1);
            
            if (data.Length == 1) 
                return data[0].value;
            
            // 範囲外の場合は端点の値を返す
            if (time <= data[0].time) 
                return data[0].value;
            if (time >= data[^1].time) 
                return data[^1].value;
            
            // 該当する区間を二分探索で見つける
            int index = FindIntervalIndex(data, time);
            
            if (index >= data.Length - 1)
                return data[^1].value;
                
            // 線形補間
            float t = MathUtils.SafeLerpParameter(time, data[index].time, data[index + 1].time);
            return Mathf.Lerp(data[index].value, data[index + 1].value, t);
        }
        
        /// <summary>
        /// 指定時間が含まれる区間のインデックスを取得
        /// </summary>
        /// <param name="data">データ配列</param>
        /// <param name="time">時間</param>
        /// <returns>区間の開始インデックス</returns>
        public static int FindIntervalIndex(TimeValuePair[] data, float time)
        {
            // 線形探索（データが小さい場合）
            if (data.Length < 16)
            {
                for (int i = 0; i < data.Length - 1; i++)
                {
                    if (time >= data[i].time && time <= data[i + 1].time)
                        return i;
                }
                return data.Length - 2; // 最後の区間
            }
            
            // 二分探索（データが大きい場合）
            int left = 0;
            int right = data.Length - 2;
            
            while (left <= right)
            {
                int mid = (left + right) / 2;
                
                if (time < data[mid].time)
                {
                    right = mid - 1;
                }
                else if (time > data[mid + 1].time)
                {
                    left = mid + 1;
                }
                else
                {
                    return mid;
                }
            }
            
            return Mathf.Clamp(left, 0, data.Length - 2);
        }
        
        /// <summary>
        /// 3点を通る2次ベジェ曲線の補間
        /// </summary>
        /// <param name="p0">開始点</param>
        /// <param name="p1">制御点</param>
        /// <param name="p2">終了点</param>
        /// <param name="t">補間パラメータ（0-1）</param>
        /// <returns>補間された点</returns>
        public static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            
            return uu * p0 + 2f * u * t * p1 + tt * p2;
        }
        
        /// <summary>
        /// 4点を通る3次ベジェ曲線の補間
        /// </summary>
        /// <param name="p0">開始点</param>
        /// <param name="p1">制御点1</param>
        /// <param name="p2">制御点2</param>
        /// <param name="p3">終了点</param>
        /// <param name="t">補間パラメータ（0-1）</param>
        /// <returns>補間された点</returns>
        public static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float uu = u * u;
            float uuu = uu * u;
            float tt = t * t;
            float ttt = tt * t;
            
            return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
        }
        
        /// <summary>
        /// Catmull-Rom スプライン補間
        /// </summary>
        /// <param name="p0">制御点0</param>
        /// <param name="p1">開始点</param>
        /// <param name="p2">終了点</param>
        /// <param name="p3">制御点3</param>
        /// <param name="t">補間パラメータ（0-1）</param>
        /// <returns>補間された点</returns>
        public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
        
        /// <summary>
        /// エルミート補間（3次）
        /// </summary>
        /// <param name="startValue">開始値</param>
        /// <param name="endValue">終了値</param>
        /// <param name="startTangent">開始タンジェント</param>
        /// <param name="endTangent">終了タンジェント</param>
        /// <param name="t">補間パラメータ（0-1）</param>
        /// <returns>補間された値</returns>
        public static float HermiteInterpolate(float startValue, float endValue, float startTangent, float endTangent, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            // エルミート基底関数
            float h1 = 2f * t3 - 3f * t2 + 1f;      // 開始値の影響
            float h2 = -2f * t3 + 3f * t2;          // 終了値の影響
            float h3 = t3 - 2f * t2 + t;            // 開始タンジェントの影響
            float h4 = t3 - t2;                     // 終了タンジェントの影響
            
            return h1 * startValue + h2 * endValue + h3 * startTangent + h4 * endTangent;
        }
        
        /// <summary>
        /// 単調3次補間（値が単調性を保つ）
        /// </summary>
        /// <param name="points">データ点（最低4点必要）</param>
        /// <param name="time">補間したい時間</param>
        /// <returns>補間された値</returns>
        public static float MonotonicCubicInterpolate(TimeValuePair[] points, float time)
        {
            ValidationUtils.ValidatePoints(points, nameof(points), 4);
            
            int index = FindIntervalIndex(points, time);
            if (index >= points.Length - 1)
                return points[^1].value;
                
            // 単調性を保つタンジェント計算
            float[] tangents = CalculateMonotonicTangents(points);
            
            float localTime = MathUtils.SafeLerpParameter(time, points[index].time, points[index + 1].time);
            float dt = points[index + 1].time - points[index].time;
            
            return HermiteInterpolate(
                points[index].value,
                points[index + 1].value,
                tangents[index] * dt,
                tangents[index + 1] * dt,
                localTime
            );
        }
        
        /// <summary>
        /// 単調性を保つタンジェント計算
        /// </summary>
        /// <param name="points">データ点</param>
        /// <returns>各点でのタンジェント</returns>
        private static float[] CalculateMonotonicTangents(TimeValuePair[] points)
        {
            int n = points.Length;
            float[] tangents = new float[n];
            float[] slopes = new float[n - 1];
            
            // 各区間の傾きを計算
            for (int i = 0; i < n - 1; i++)
            {
                slopes[i] = MathUtils.SafeSlope(points[i].time, points[i].value, 
                                              points[i + 1].time, points[i + 1].value);
            }
            
            // 端点のタンジェント
            tangents[0] = slopes[0];
            tangents[n - 1] = slopes[n - 2];
            
            // 内部点のタンジェント（単調性を保つ）
            for (int i = 1; i < n - 1; i++)
            {
                float s0 = slopes[i - 1];
                float s1 = slopes[i];
                
                if (s0 * s1 <= 0f)
                {
                    // 極値の場合はタンジェントを0にして単調性を保つ
                    tangents[i] = 0f;
                }
                else
                {
                    // 調和平均を使用
                    tangents[i] = 2f * s0 * s1 / (s0 + s1);
                }
            }
            
            return tangents;
        }
    }
}
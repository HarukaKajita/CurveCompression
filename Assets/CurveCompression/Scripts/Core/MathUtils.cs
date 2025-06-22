using UnityEngine;

namespace CurveCompression.Core
{
    /// <summary>
    /// 数学演算のユーティリティクラス
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// 小さな値（ゼロに近い値）の判定に使用する閾値
        /// </summary>
        public const float Epsilon = 1e-6f;
        
        /// <summary>
        /// 安全な除算（ゼロ除算を回避）
        /// </summary>
        /// <param name="numerator">分子</param>
        /// <param name="denominator">分母</param>
        /// <param name="defaultValue">分母がゼロの場合の戻り値</param>
        /// <returns>除算結果またはデフォルト値</returns>
        public static float SafeDivide(float numerator, float denominator, float defaultValue = 0f)
        {
            return Mathf.Abs(denominator) < Epsilon ? defaultValue : numerator / denominator;
        }
        
        /// <summary>
        /// 時間間隔が有効かチェック
        /// </summary>
        /// <param name="dt">時間間隔</param>
        /// <returns>有効な時間間隔の場合true</returns>
        public static bool IsValidTimeInterval(float dt)
        {
            return dt > Epsilon;
        }
        
        /// <summary>
        /// 2点間の傾きを安全に計算
        /// </summary>
        /// <param name="x1">開始点X</param>
        /// <param name="y1">開始点Y</param>
        /// <param name="x2">終了点X</param>
        /// <param name="y2">終了点Y</param>
        /// <returns>傾き（垂直線の場合は0）</returns>
        public static float SafeSlope(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            return SafeDivide(dy, dx, 0f);
        }
        
        /// <summary>
        /// 線形補間パラメータを安全に計算
        /// </summary>
        /// <param name="value">補間したい値</param>
        /// <param name="start">開始値</param>
        /// <param name="end">終了値</param>
        /// <returns>補間パラメータ（0-1の範囲外も可能）</returns>
        public static float SafeLerpParameter(float value, float start, float end)
        {
            float range = end - start;
            if (Mathf.Abs(range) < Epsilon)
                return 0f; // 開始値と終了値が同じ場合
                
            return (value - start) / range;
        }
        
        /// <summary>
        /// ベクトルの長さの二乗を計算（平方根を避けるため）
        /// </summary>
        /// <param name="dx">X成分</param>
        /// <param name="dy">Y成分</param>
        /// <returns>長さの二乗</returns>
        public static float DistanceSquared(float dx, float dy)
        {
            return dx * dx + dy * dy;
        }
        
        /// <summary>
        /// 値が範囲内にあるかチェック
        /// </summary>
        /// <param name="value">チェックする値</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>範囲内の場合true</returns>
        public static bool IsInRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }
    }
}
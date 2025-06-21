using System;
using UnityEngine;

namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 重要度計算のための重み設定
    /// </summary>
    [Serializable]
    public class ImportanceWeights
    {
        [Range(0f, 1f)] public float curvature = 0.4f;      // 曲率の重み
        [Range(0f, 1f)] public float changeRate = 0.25f;    // 変化率の重み
        [Range(0f, 1f)] public float localVariance = 0.2f;  // 局所分散の重み
        [Range(0f, 1f)] public float extremeValue = 0.15f;  // 極値の重み
        
        /// <summary>
        /// デフォルト設定（データタイプに基づく推奨値）
        /// </summary>
        public static ImportanceWeights Default => new ImportanceWeights();
        
        /// <summary>
        /// アニメーション用（滑らかさ重視）
        /// </summary>
        public static ImportanceWeights ForAnimation => new ImportanceWeights 
        { 
            curvature = 0.5f, changeRate = 0.2f, localVariance = 0.15f, extremeValue = 0.15f 
        };
        
        /// <summary>
        /// センサーデータ用（ノイズ耐性重視）
        /// </summary>
        public static ImportanceWeights ForSensorData => new ImportanceWeights 
        { 
            curvature = 0.3f, changeRate = 0.15f, localVariance = 0.35f, extremeValue = 0.2f 
        };
        
        /// <summary>
        /// 金融データ用（極値重視）
        /// </summary>
        public static ImportanceWeights ForFinancialData => new ImportanceWeights 
        { 
            curvature = 0.25f, changeRate = 0.3f, localVariance = 0.15f, extremeValue = 0.3f 
        };
        
        /// <summary>
        /// 重みの正規化
        /// </summary>
        public void Normalize()
        {
            float total = curvature + changeRate + localVariance + extremeValue;
            if (total > 0)
            {
                curvature /= total;
                changeRate /= total;
                localVariance /= total;
                extremeValue /= total;
            }
        }
    }
}
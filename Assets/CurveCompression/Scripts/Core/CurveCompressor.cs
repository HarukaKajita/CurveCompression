using UnityEngine;
using CurveCompression.DataStructures;
using CurveCompression.Algorithms;

namespace CurveCompression.Core
{
    /// <summary>
    /// カーブ圧縮のメインクラス
    /// </summary>
    public static class CurveCompressor
    {
        /// <summary>
        /// データを圧縮（標準インターフェース）
        /// </summary>
        public static CompressionResult Compress(TimeValuePair[] originalData, CompressionParams parameters)
        {
            ValidationUtils.ValidatePoints(originalData, nameof(originalData));
            ValidationUtils.ValidateCompressionParams(parameters);
            
            // 統一的な圧縮手法を使用
            CompressedCurveData compressedCurve = HybridCompressor.Compress(originalData, parameters);
            
            return new CompressionResult(originalData, compressedCurve);
        }
        
        /// <summary>
        /// データを圧縮（シンプルインターフェース）
        /// </summary>
        public static CompressionResult Compress(TimeValuePair[] originalData, float tolerance)
        {
            var parameters = new CompressionParams { tolerance = tolerance };
            return Compress(originalData, parameters);
        }
        
    }
}
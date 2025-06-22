using UnityEngine;
using CurveCompression.DataStructures;
using CurveCompression.Algorithms;
using System.Diagnostics;

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
            
            // 時間計測の開始
            Stopwatch stopwatch = null;
            if (parameters.enableTimeMeasurement)
            {
                stopwatch = Stopwatch.StartNew();
            }
            
            // 統一的な圧縮手法を使用
            CompressedCurveData compressedCurve = HybridCompressor.Compress(originalData, parameters);
            
            // 時間計測の終了
            float compressionTime = 0f;
            if (stopwatch != null)
            {
                stopwatch.Stop();
                compressionTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            }
            
            var result = new CompressionResult(originalData, compressedCurve);
            result.compressionTime = compressionTime;
            
            return result;
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
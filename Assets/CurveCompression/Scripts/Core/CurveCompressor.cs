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
            
            // 圧縮手法に応じて処理を分岐
            CompressedCurveData compressedCurve = CompressInternal(originalData, parameters);
            
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
        
        /// <summary>
        /// 内部圧縮処理
        /// </summary>
        private static CompressedCurveData CompressInternal(TimeValuePair[] points, CompressionParams parameters)
        {
            if (points.Length <= 2)
            {
                var linearSegment = CurveSegment.CreateLinear(
                    points[0].time, points[0].value,
                    points[^1].time, points[^1].value
                );
                return new CompressedCurveData(new[] { linearSegment });
            }
            
            return parameters.compressionMethod switch
            {
                CompressionMethod.RDP => RDPAlgorithm.Compress(points, parameters),
                    
                CompressionMethod.BSpline => BSplineAlgorithm.Compress(points, parameters.tolerance),
                
                CompressionMethod.Bezier => BezierAlgorithm.Compress(points, parameters.tolerance),
                
                _ => BezierAlgorithm.Compress(points, parameters.tolerance) // デフォルト
            };
        }
    }
}
using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Core
{
    /// <summary>
    /// Unity固有の圧縮ユーティリティクラス
    /// </summary>
    public static class UnityCompressionUtils
    {
        /// <summary>
        /// AnimationCurveからTimeValuePair配列に変換
        /// </summary>
        /// <param name="curve">変換元のAnimationCurve</param>
        /// <param name="sampleCount">サンプリング数</param>
        /// <returns>TimeValuePair配列</returns>
        public static TimeValuePair[] FromAnimationCurve(AnimationCurve curve, int sampleCount)
        {
            ValidationUtils.ValidateRange(sampleCount, 2, 10000, nameof(sampleCount));
            
            if (curve == null || curve.length == 0)
                return new TimeValuePair[0];
            
            var result = new TimeValuePair[sampleCount];
            float startTime = curve.keys[0].time;
            float endTime = curve.keys[curve.length - 1].time;
            float duration = endTime - startTime;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                float time = startTime + t * duration;
                float value = curve.Evaluate(time);
                result[i] = new TimeValuePair(time, value);
            }
            
            return result;
        }
        
        /// <summary>
        /// TimeValuePair配列からAnimationCurveに変換
        /// </summary>
        /// <param name="data">変換元のTimeValuePair配列</param>
        /// <returns>AnimationCurve</returns>
        public static AnimationCurve ToAnimationCurve(TimeValuePair[] data)
        {
            ValidationUtils.ValidatePoints(data, nameof(data), 1);
            
            var curve = new AnimationCurve();
            
            foreach (var point in data)
            {
                curve.AddKey(point.time, point.value);
            }
            
            return curve;
        }
        
        /// <summary>
        /// CompressedCurveDataからAnimationCurveに変換
        /// </summary>
        /// <param name="compressedData">圧縮されたカーブデータ</param>
        /// <param name="sampleCount">サンプリング数（省略時は100）</param>
        /// <returns>AnimationCurve</returns>
        public static AnimationCurve ToAnimationCurve(CompressedCurveData compressedData, int sampleCount = 100)
        {
            if (compressedData == null || compressedData.segments.Length == 0)
                return new AnimationCurve();
                
            var points = compressedData.ToTimeValuePairs(sampleCount);
            return ToAnimationCurve(points);
        }
        
        /// <summary>
        /// AnimationCurveからCompressedCurveDataに変換
        /// </summary>
        /// <param name="curve">変換元のAnimationCurve</param>
        /// <param name="tolerance">圧縮許容誤差</param>
        /// <returns>CompressedCurveData</returns>
        public static CompressedCurveData FromAnimationCurve(AnimationCurve curve, float tolerance = 0.01f)
        {
            if (curve == null || curve.length == 0)
                return new CompressedCurveData(new CurveSegment[0]);
            
            // AnimationCurveのキーポイントを使ってBezierセグメントを作成
            var segments = new System.Collections.Generic.List<CurveSegment>();
            
            for (int i = 0; i < curve.length - 1; i++)
            {
                var startKey = curve.keys[i];
                var endKey = curve.keys[i + 1];
                
                var segment = CurveSegment.CreateBezier(
                    startKey.time, startKey.value,
                    endKey.time, endKey.value,
                    startKey.outTangent,
                    endKey.inTangent
                );
                
                segments.Add(segment);
            }
            
            return new CompressedCurveData(segments.ToArray());
        }
        
        /// <summary>
        /// AnimationCurveを圧縮
        /// </summary>
        /// <param name="curve">圧縮対象のAnimationCurve</param>
        /// <param name="parameters">圧縮パラメータ</param>
        /// <param name="sampleCount">サンプリング数</param>
        /// <returns>圧縮結果</returns>
        public static CompressionResult CompressAnimationCurve(AnimationCurve curve, CompressionParams parameters, int sampleCount = 1000)
        {
            var timeValuePairs = FromAnimationCurve(curve, sampleCount);
            return CurveCompressor.Compress(timeValuePairs, parameters);
        }
    }
}
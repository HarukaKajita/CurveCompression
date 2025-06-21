using System;
using UnityEngine;

namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 圧縮されたカーブデータ
    /// </summary>
    [Serializable]
    public class CompressedCurveData
    {
        public CurveSegment[] segments;
        
        public CompressedCurveData(CurveSegment[] segments)
        {
            this.segments = segments;
        }
        
        /// <summary>
        /// 指定した時間での値を評価
        /// </summary>
        public float Evaluate(float time)
        {
            if (segments == null || segments.Length == 0) return 0f;
            
            // 該当するセグメントを探す
            foreach (var segment in segments)
            {
                if (time >= segment.startTime && time <= segment.endTime)
                {
                    return segment.Evaluate(time);
                }
            }
            
            // セグメント外の場合は最も近いセグメントの端点を返す
            if (time < segments[0].startTime)
                return segments[0].startValue;
            else
                return segments[segments.Length - 1].endValue;
        }
        
        /// <summary>
        /// TimeValuePair配列に変換（サンプリング）
        /// </summary>
        public TimeValuePair[] ToTimeValuePairs(int sampleCount)
        {
            if (segments == null || segments.Length == 0 || sampleCount <= 0)
                return new TimeValuePair[0];
                
            var result = new TimeValuePair[sampleCount];
            float startTime = segments[0].startTime;
            float endTime = segments[segments.Length - 1].endTime;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                float time = Mathf.Lerp(startTime, endTime, t);
                float value = Evaluate(time);
                result[i] = new TimeValuePair(time, value);
            }
            
            return result;
        }
    }
}
using System;
using UnityEngine;

namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 時間付きデータポイント
    /// </summary>
    [Serializable]
    public struct TimeValuePair : IComparable<TimeValuePair>
    {
        public float time;
        public float value;
        
        public TimeValuePair(float time, float value)
        {
            this.time = time;
            this.value = value;
        }
        
        public int CompareTo(TimeValuePair other)
        {
            return time.CompareTo(other.time);
        }
    }
}
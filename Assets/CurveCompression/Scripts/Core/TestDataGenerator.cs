using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Core
{
    /// <summary>
    /// テストデータ生成ユーティリティ
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// サイン波ベースの複雑なテストデータを生成
        /// </summary>
        public static TimeValuePair[] GenerateComplexWaveform(int dataPoints, float duration)
        {
            var data = new TimeValuePair[dataPoints];
            
            for (int i = 0; i < dataPoints; i++)
            {
                float time = (float)i / (dataPoints - 1) * duration;
                
                // 複数の周波数成分を含む複雑な波形
                float value = Mathf.Sin(time * 2.0f) * 0.5f +
                             Mathf.Sin(time * 5.0f) * 0.3f +
                             Mathf.Sin(time * 10.0f) * 0.2f +
                             Mathf.PerlinNoise(time * 0.5f, 0) * 0.4f;
                
                data[i] = new TimeValuePair(time, value);
            }
            
            return data;
        }
        
        /// <summary>
        /// ステップ関数のテストデータを生成
        /// </summary>
        public static TimeValuePair[] GenerateStepFunction(int dataPoints, float duration, int steps)
        {
            var data = new TimeValuePair[dataPoints];
            float stepDuration = duration / steps;
            
            for (int i = 0; i < dataPoints; i++)
            {
                float time = (float)i / (dataPoints - 1) * duration;
                int currentStep = Mathf.FloorToInt(time / stepDuration);
                float value = currentStep * 0.5f;
                
                data[i] = new TimeValuePair(time, value);
            }
            
            return data;
        }
        
        /// <summary>
        /// ノイズを含むデータを生成
        /// </summary>
        public static TimeValuePair[] GenerateNoisyData(int dataPoints, float duration, float noiseLevel)
        {
            var data = new TimeValuePair[dataPoints];
            
            for (int i = 0; i < dataPoints; i++)
            {
                float time = (float)i / (dataPoints - 1) * duration;
                float baseValue = Mathf.Sin(time * 2.0f);
                float noise = (Random.value - 0.5f) * 2.0f * noiseLevel;
                
                data[i] = new TimeValuePair(time, baseValue + noise);
            }
            
            return data;
        }
        
        /// <summary>
        /// 指数関数的な変化のデータを生成
        /// </summary>
        public static TimeValuePair[] GenerateExponentialData(int dataPoints, float duration, float rate)
        {
            var data = new TimeValuePair[dataPoints];
            
            for (int i = 0; i < dataPoints; i++)
            {
                float time = (float)i / (dataPoints - 1) * duration;
                float value = Mathf.Exp(time * rate) - 1.0f;
                
                data[i] = new TimeValuePair(time, value);
            }
            
            return data;
        }
    }
}
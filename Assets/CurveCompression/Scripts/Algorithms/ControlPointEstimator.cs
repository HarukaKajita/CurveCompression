using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Algorithms
{
    /// <summary>
    /// コントロールポイント数推定アルゴリズム
    /// </summary>
    public static class ControlPointEstimator
    {
        /// <summary>
        /// 推定結果
        /// </summary>
        public class EstimationResult
        {
            public int optimalPoints;
            public float score;
            public string method;
            public Dictionary<string, float> metrics;

            public EstimationResult(int points, float score, string method)
            {
                this.optimalPoints = points;
                this.score = score;
                this.method = method;
                this.metrics = new Dictionary<string, float>();
            }
        }

        /// <summary>
        /// 全ての推定アルゴリズムを実行
        /// </summary>
        public static Dictionary<string, EstimationResult> EstimateAll(TimeValuePair[] data, float tolerance, int minPoints = 2, int maxPoints = 50)
        {
            var results = new Dictionary<string, EstimationResult>();

            // 5つの推定アルゴリズム
            results["Elbow"] = EstimateByElbowMethod(data, tolerance, minPoints, maxPoints);
            results["Curvature"] = EstimateByCurvature(data, tolerance, minPoints, maxPoints);
            results["Entropy"] = EstimateByInformationEntropy(data, tolerance, minPoints, maxPoints);
            results["DouglasPeucker"] = EstimateByDouglasPeuckerAdaptive(data, tolerance, minPoints, maxPoints);
            results["TotalVariation"] = EstimateByTotalVariation(data, tolerance, minPoints, maxPoints);

            // 2つの上限決定アルゴリズム
            results["ErrorBound"] = DetermineByErrorBound(data, tolerance);
            results["Statistical"] = DetermineByStatistical(data, tolerance);

            return results;
        }

        /// <summary>
        /// 1. Elbow法（改良版）
        /// </summary>
        public static EstimationResult EstimateByElbowMethod(TimeValuePair[] data, float tolerance, int minPoints, int maxPoints)
        {
            var errors = new List<float>();
            var secondDerivatives = new List<float>();

            // 各コントロールポイント数での誤差を計算
            for (int n = minPoints; n <= maxPoints; n++)
            {
                var compressed = BSplineAlgorithm.ApproximateWithFixedPoints(data, n);
                float error = CalculateMeanSquaredError(data, compressed);
                errors.Add(error);
            }

            // 二階微分を計算（誤差の変化率の変化）
            for (int i = 1; i < errors.Count - 1; i++)
            {
                float d2 = errors[i + 1] - 2 * errors[i] + errors[i - 1];
                secondDerivatives.Add(Mathf.Abs(d2));
            }

            // 最大の二階微分を持つ点を見つける
            int elbowIndex = 1;
            float maxD2 = 0;
            for (int i = 0; i < secondDerivatives.Count; i++)
            {
                if (secondDerivatives[i] > maxD2)
                {
                    maxD2 = secondDerivatives[i];
                    elbowIndex = i + 1; // インデックス調整
                }
            }

            int optimalPoints = minPoints + elbowIndex;
            var result = new EstimationResult(optimalPoints, errors[elbowIndex], "Elbow Method");
            result.metrics["error"] = errors[elbowIndex];
            result.metrics["second_derivative"] = maxD2;

            return result;
        }

        /// <summary>
        /// 2. 曲率ベースの推定（拡張版）
        /// </summary>
        public static EstimationResult EstimateByCurvature(TimeValuePair[] data, float tolerance, int minPoints, int maxPoints)
        {
            // データの総曲率を計算
            float totalCurvature = 0;
            var curvatures = new List<float>();

            for (int i = 1; i < data.Length - 1; i++)
            {
                float curvature = CalculateLocalCurvature(data, i);
                curvatures.Add(curvature);
                totalCurvature += curvature;
            }

            // 曲率の分布を分析
            curvatures.Sort((a, b) => b.CompareTo(a)); // 降順ソート
            
            // 累積曲率が全体の90%に達するまでのポイント数を推定
            float cumulativeCurvature = 0;
            int significantPoints = 0;
            float threshold = totalCurvature * 0.9f;

            foreach (float c in curvatures)
            {
                cumulativeCurvature += c;
                significantPoints++;
                if (cumulativeCurvature >= threshold)
                    break;
            }

            // データ長に対する比率で調整
            int optimalPoints = Mathf.Clamp(
                Mathf.RoundToInt(significantPoints * 0.5f + minPoints),
                minPoints,
                maxPoints
            );

            var result = new EstimationResult(optimalPoints, totalCurvature, "Curvature Based");
            result.metrics["total_curvature"] = totalCurvature;
            result.metrics["significant_points"] = significantPoints;

            return result;
        }

        /// <summary>
        /// 3. 情報エントロピーベースの手法
        /// </summary>
        public static EstimationResult EstimateByInformationEntropy(TimeValuePair[] data, float tolerance, int minPoints, int maxPoints)
        {
            // データの情報エントロピーを計算
            float dataEntropy = CalculateEntropy(data);
            
            // 各コントロールポイント数での情報保持率を計算
            var informationRates = new List<float>();
            
            for (int n = minPoints; n <= maxPoints; n++)
            {
                var compressed = BSplineAlgorithm.ApproximateWithFixedPoints(data, n);
                float compressedEntropy = CalculateEntropy(compressed);
                float rate = compressedEntropy / dataEntropy;
                informationRates.Add(rate);
                
                // 95%の情報を保持できる最小のポイント数を見つける
                if (rate >= 0.95f)
                {
                    var result = new EstimationResult(n, rate, "Information Entropy");
                    result.metrics["original_entropy"] = dataEntropy;
                    result.metrics["compressed_entropy"] = compressedEntropy;
                    result.metrics["information_rate"] = rate;
                    return result;
                }
            }

            // 95%に達しない場合は最大値を返す
            var finalResult = new EstimationResult(maxPoints, informationRates.Last(), "Information Entropy");
            finalResult.metrics["original_entropy"] = dataEntropy;
            finalResult.metrics["information_rate"] = informationRates.Last();
            return finalResult;
        }

        /// <summary>
        /// 4. Douglas-Peucker派生の適応的手法
        /// </summary>
        public static EstimationResult EstimateByDouglasPeuckerAdaptive(TimeValuePair[] data, float tolerance, int minPoints, int maxPoints)
        {
            // 異なる許容誤差でRDPを実行し、ポイント数の変化を分析
            var tolerancePoints = new List<(float tol, int points)>();
            
            float minTol = tolerance * 0.1f;
            float maxTol = tolerance * 10f;
            int steps = 20;
            
            for (int i = 0; i < steps; i++)
            {
                float t = minTol * Mathf.Pow(maxTol / minTol, (float)i / (steps - 1));
                var simplified = RDPAlgorithm.Simplify(data, t);
                tolerancePoints.Add((t, simplified.Length));
            }

            // 目標許容誤差でのポイント数を補間
            float targetPoints = InterpolatePoints(tolerancePoints, tolerance);
            int optimalPoints = Mathf.Clamp(Mathf.RoundToInt(targetPoints), minPoints, maxPoints);

            var result = new EstimationResult(optimalPoints, tolerance, "Douglas-Peucker Adaptive");
            result.metrics["tolerance"] = tolerance;
            result.metrics["interpolated_points"] = targetPoints;

            return result;
        }

        /// <summary>
        /// 5. 総変動ベースの手法
        /// </summary>
        public static EstimationResult EstimateByTotalVariation(TimeValuePair[] data, float tolerance, int minPoints, int maxPoints)
        {
            // データの総変動を計算
            float totalVariation = CalculateTotalVariation(data);
            
            // 各コントロールポイント数での変動保持率を計算
            var variationRates = new List<float>();
            
            for (int n = minPoints; n <= maxPoints; n++)
            {
                var compressed = BSplineAlgorithm.ApproximateWithFixedPoints(data, n);
                float compressedVariation = CalculateTotalVariation(compressed);
                float rate = compressedVariation / totalVariation;
                variationRates.Add(rate);
                
                // 90%の変動を保持できる最小のポイント数を見つける
                if (rate >= 0.9f)
                {
                    var result = new EstimationResult(n, rate, "Total Variation");
                    result.metrics["original_variation"] = totalVariation;
                    result.metrics["compressed_variation"] = compressedVariation;
                    result.metrics["variation_rate"] = rate;
                    return result;
                }
            }

            // 90%に達しない場合は最大値を返す
            var finalResult = new EstimationResult(maxPoints, variationRates.Last(), "Total Variation");
            finalResult.metrics["original_variation"] = totalVariation;
            finalResult.metrics["variation_rate"] = variationRates.Last();
            return finalResult;
        }

        /// <summary>
        /// 6. 誤差ベースの適応的上限
        /// </summary>
        public static EstimationResult DetermineByErrorBound(TimeValuePair[] data, float tolerance)
        {
            // 二分探索で許容誤差を満たす最小のポイント数を見つける
            int minPoints = 2;
            int maxPoints = Mathf.Min(data.Length / 2, 200); // 上限を適応的に設定
            
            while (minPoints < maxPoints)
            {
                int mid = (minPoints + maxPoints) / 2;
                var compressed = BSplineAlgorithm.ApproximateWithFixedPoints(data, mid);
                float maxError = CalculateMaxError(data, compressed);
                
                if (maxError <= tolerance)
                {
                    maxPoints = mid;
                }
                else
                {
                    minPoints = mid + 1;
                }
            }

            var result = new EstimationResult(minPoints, tolerance, "Error Bound");
            result.metrics["max_points"] = maxPoints;
            result.metrics["tolerance"] = tolerance;

            return result;
        }

        /// <summary>
        /// 7. 統計的アプローチ
        /// </summary>
        public static EstimationResult DetermineByStatistical(TimeValuePair[] data, float tolerance)
        {
            // データの統計的特性を分析
            float mean = data.Average(p => p.value);
            float variance = data.Average(p => (p.value - mean) * (p.value - mean));
            float stdDev = Mathf.Sqrt(variance);
            
            // ノイズレベルを推定（隣接点の差分の標準偏差）
            float noiseLevel = EstimateNoiseLevel(data);
            
            // 信号対雑音比(SNR)を計算
            float snr = stdDev / (noiseLevel + 0.0001f);
            
            // SNRに基づいて上限を決定
            int maxPoints = Mathf.Clamp(
                Mathf.RoundToInt(10 + snr * 5),
                10,
                Mathf.Min(data.Length / 2, 200)
            );

            var result = new EstimationResult(maxPoints, snr, "Statistical");
            result.metrics["variance"] = variance;
            result.metrics["noise_level"] = noiseLevel;
            result.metrics["snr"] = snr;

            return result;
        }

        // ヘルパーメソッド
        private static float CalculateLocalCurvature(TimeValuePair[] data, int index)
        {
            if (index <= 0 || index >= data.Length - 1) return 0;

            Vector2 prev = new Vector2(data[index - 1].time, data[index - 1].value);
            Vector2 curr = new Vector2(data[index].time, data[index].value);
            Vector2 next = new Vector2(data[index + 1].time, data[index + 1].value);

            Vector2 v1 = (curr - prev).normalized;
            Vector2 v2 = (next - curr).normalized;
            
            float dot = Vector2.Dot(v1, v2);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
            
            return angle;
        }

        private static float CalculateEntropy(TimeValuePair[] data)
        {
            if (data.Length <= 1) return 0;

            // ヒストグラムを作成
            int bins = Mathf.Max(1, Mathf.Min(20, data.Length / 5));
            float min = data.Min(p => p.value);
            float max = data.Max(p => p.value);
            float range = max - min;
            
            if (range < 0.0001f) return 0;

            var histogram = new int[bins];
            foreach (var point in data)
            {
                int bin = Mathf.Clamp((int)((point.value - min) / range * (bins - 1)), 0, bins - 1);
                histogram[bin]++;
            }

            // エントロピーを計算
            float entropy = 0;
            float total = data.Length;
            
            foreach (int count in histogram)
            {
                if (count > 0)
                {
                    float p = count / total;
                    entropy -= p * Mathf.Log(p, 2);
                }
            }

            return entropy;
        }

        private static float CalculateTotalVariation(TimeValuePair[] data)
        {
            float totalVariation = 0;
            for (int i = 1; i < data.Length; i++)
            {
                totalVariation += Mathf.Abs(data[i].value - data[i - 1].value);
            }
            return totalVariation;
        }

        private static float EstimateNoiseLevel(TimeValuePair[] data)
        {
            if (data.Length < 3) return 0;

            var differences = new List<float>();
            for (int i = 1; i < data.Length; i++)
            {
                float diff = data[i].value - data[i - 1].value;
                differences.Add(diff);
            }

            float mean = differences.Average();
            float variance = differences.Average(d => (d - mean) * (d - mean));
            
            return Mathf.Sqrt(variance);
        }

        private static float InterpolatePoints(List<(float tol, int points)> data, float targetTol)
        {
            // 対数スケールで補間
            for (int i = 0; i < data.Count - 1; i++)
            {
                if (data[i].tol <= targetTol && targetTol <= data[i + 1].tol)
                {
                    float logT0 = Mathf.Log(data[i].tol);
                    float logT1 = Mathf.Log(data[i + 1].tol);
                    float logTarget = Mathf.Log(targetTol);
                    
                    float t = (logTarget - logT0) / (logT1 - logT0);
                    return Mathf.Lerp(data[i].points, data[i + 1].points, t);
                }
            }
            
            return data.Last().points;
        }

        private static float CalculateMeanSquaredError(TimeValuePair[] original, TimeValuePair[] compressed)
        {
            float totalError = 0;
            
            foreach (var point in original)
            {
                float interpolatedValue = InterpolateValue(compressed, point.time);
                float error = point.value - interpolatedValue;
                totalError += error * error;
            }
            
            return totalError / original.Length;
        }

        private static float CalculateMaxError(TimeValuePair[] original, TimeValuePair[] compressed)
        {
            float maxError = 0;
            
            foreach (var point in original)
            {
                float interpolatedValue = InterpolateValue(compressed, point.time);
                float error = Mathf.Abs(point.value - interpolatedValue);
                maxError = Mathf.Max(maxError, error);
            }
            
            return maxError;
        }

        private static float InterpolateValue(TimeValuePair[] data, float time)
        {
            if (data.Length == 0) return 0;
            if (data.Length == 1) return data[0].value;
            
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (time >= data[i].time && time <= data[i + 1].time)
                {
                    float t = (time - data[i].time) / (data[i + 1].time - data[i].time);
                    return Mathf.Lerp(data[i].value, data[i + 1].value, t);
                }
            }
            
            return time < data[0].time ? data[0].value : data[^1].value;
        }
    }
}
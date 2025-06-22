using System;
using CurveCompression.DataStructures;

namespace CurveCompression.Core
{
    /// <summary>
    /// 入力値検証のユーティリティクラス
    /// </summary>
    public static class ValidationUtils
    {
        /// <summary>
        /// TimeValuePair配列の基本検証
        /// </summary>
        /// <param name="points">検証する配列</param>
        /// <param name="paramName">パラメータ名（エラーメッセージ用）</param>
        /// <param name="minRequired">必要な最小要素数</param>
        /// <exception cref="ArgumentNullException">配列がnullの場合</exception>
        /// <exception cref="ArgumentException">配列が空または要素数が不足の場合</exception>
        public static void ValidatePoints(TimeValuePair[] points, string paramName = "points", int minRequired = 2)
        {
            if (points == null)
                throw new ArgumentNullException(paramName, "Point array cannot be null");
                
            if (points.Length == 0)
                throw new ArgumentException("Point array cannot be empty", paramName);
                
            if (points.Length < minRequired)
                throw new ArgumentException($"At least {minRequired} points are required, but got {points.Length}", paramName);
        }
        
        /// <summary>
        /// 配列インデックスの範囲検証
        /// </summary>
        /// <param name="index">検証するインデックス</param>
        /// <param name="arrayLength">配列の長さ</param>
        /// <param name="paramName">パラメータ名（エラーメッセージ用）</param>
        /// <exception cref="ArgumentOutOfRangeException">インデックスが範囲外の場合</exception>
        public static void ValidateIndex(int index, int arrayLength, string paramName = "index")
        {
            if (index < 0 || index >= arrayLength)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"Index {index} is out of range [0, {arrayLength - 1}]");
        }
        
        /// <summary>
        /// 配列の範囲インデックス検証（start, end）
        /// </summary>
        /// <param name="start">開始インデックス</param>
        /// <param name="end">終了インデックス</param>
        /// <param name="arrayLength">配列の長さ</param>
        /// <exception cref="ArgumentOutOfRangeException">インデックスが範囲外の場合</exception>
        /// <exception cref="ArgumentException">start > endの場合</exception>
        public static void ValidateRange(int start, int end, int arrayLength)
        {
            ValidateIndex(start, arrayLength, "start");
            ValidateIndex(end, arrayLength, "end");
            
            if (start > end)
                throw new ArgumentException($"Start index ({start}) cannot be greater than end index ({end})");
        }
        
        /// <summary>
        /// 許容誤差の検証
        /// </summary>
        /// <param name="tolerance">許容誤差</param>
        /// <param name="paramName">パラメータ名</param>
        /// <exception cref="ArgumentOutOfRangeException">許容誤差が負または零の場合</exception>
        public static void ValidateTolerance(float tolerance, string paramName = "tolerance")
        {
            if (tolerance <= 0f)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"Tolerance must be positive, but got {tolerance}");
        }
        
        /// <summary>
        /// コントロールポイント数の検証
        /// </summary>
        /// <param name="numControlPoints">コントロールポイント数</param>
        /// <param name="dataLength">元データの長さ</param>
        /// <param name="paramName">パラメータ名</param>
        /// <exception cref="ArgumentOutOfRangeException">コントロールポイント数が無効な場合</exception>
        public static void ValidateControlPointCount(int numControlPoints, int dataLength, string paramName = "numControlPoints")
        {
            if (numControlPoints < 2)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"Number of control points must be at least 2, but got {numControlPoints}");
                    
            if (numControlPoints > dataLength)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"Number of control points ({numControlPoints}) cannot exceed data length ({dataLength})");
        }
        
        /// <summary>
        /// 圧縮パラメータの検証
        /// </summary>
        /// <param name="parameters">圧縮パラメータ</param>
        /// <exception cref="ArgumentNullException">パラメータがnullの場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">パラメータ値が無効な場合</exception>
        public static void ValidateCompressionParams(CompressionParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Compression parameters cannot be null");
                
            ValidateTolerance(parameters.tolerance, nameof(parameters.tolerance));
            
            if (parameters.importanceThreshold <= 0f)
                throw new ArgumentOutOfRangeException(nameof(parameters.importanceThreshold), 
                    $"Importance threshold must be positive, but got {parameters.importanceThreshold}");
        }
        
        /// <summary>
        /// TimeValuePair配列の時間順序検証
        /// </summary>
        /// <param name="points">検証する配列</param>
        /// <param name="paramName">パラメータ名</param>
        /// <param name="requireStrictlyIncreasing">厳密に単調増加を要求するかどうか</param>
        /// <exception cref="ArgumentException">時間順序が正しくない場合</exception>
        public static void ValidateTimeOrder(TimeValuePair[] points, string paramName = "points", bool requireStrictlyIncreasing = false)
        {
            ValidatePoints(points, paramName, 1);
            
            for (int i = 1; i < points.Length; i++)
            {
                if (requireStrictlyIncreasing)
                {
                    if (points[i].time <= points[i - 1].time)
                        throw new ArgumentException(
                            $"Time values must be strictly increasing. Found time[{i - 1}]={points[i - 1].time} >= time[{i}]={points[i].time}", 
                            paramName);
                }
                else
                {
                    if (points[i].time < points[i - 1].time)
                        throw new ArgumentException(
                            $"Time values must be non-decreasing. Found time[{i - 1}]={points[i - 1].time} > time[{i}]={points[i].time}", 
                            paramName);
                }
            }
        }
        
        /// <summary>
        /// Vector2配列の検証（B-Splineコントロールポイント用）
        /// </summary>
        /// <param name="controlPoints">コントロールポイント配列</param>
        /// <param name="paramName">パラメータ名</param>
        /// <param name="minRequired">必要な最小要素数</param>
        /// <exception cref="ArgumentNullException">配列がnullの場合</exception>
        /// <exception cref="ArgumentException">配列が空または要素数が不足の場合</exception>
        public static void ValidateControlPoints(UnityEngine.Vector2[] controlPoints, string paramName = "controlPoints", int minRequired = 2)
        {
            if (controlPoints == null)
                throw new ArgumentNullException(paramName, "Control points array cannot be null");
                
            if (controlPoints.Length == 0)
                throw new ArgumentException("Control points array cannot be empty", paramName);
                
            if (controlPoints.Length < minRequired)
                throw new ArgumentException($"At least {minRequired} control points are required, but got {controlPoints.Length}", paramName);
        }
        
        /// <summary>
        /// 範囲内の値かどうかを検証
        /// </summary>
        /// <param name="value">検証する値</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <param name="paramName">パラメータ名</param>
        /// <exception cref="ArgumentOutOfRangeException">値が範囲外の場合</exception>
        public static void ValidateRange(float value, float min, float max, string paramName = "value")
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"Value {value} is out of range [{min}, {max}]");
        }
    }
}
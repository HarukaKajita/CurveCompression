using System;
using UnityEngine;
using CurveCompression.Core;

namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 圧縮モード
    /// </summary>
    public enum CompressionMode
    {
        ToleranceBased,        // 許容誤差ベースの圧縮
        FixedControlPoints,    // 固定コントロールポイント数
        EstimatedControlPoints // 推定アルゴリズムでコントロールポイント数決定
    }
    
    /// <summary>
    /// 制御点推定方法
    /// </summary>
    public enum EstimationMethod
    {
        Elbow,
        Curvature,
        Entropy,
        DouglasePeucker,
        TotalVariation,
        ErrorBound,
        Statistical
    }

    /// <summary>
    /// 圧縮パラメータ
    /// </summary>
    [Serializable]
    public class CompressionParams
    {
        [Range(0.001f, 1.0f)]
        [SerializeField] private float _tolerance = 0.01f;
        
        [Range(0.1f, 10.0f)]
        [SerializeField] private float _importanceThreshold = 1.0f;
        
        public CompressionMethod compressionMethod = CompressionMethod.Bezier; // 圧縮手法
        public CompressionMode compressionMode = CompressionMode.ToleranceBased; // 圧縮モード
        
        public CompressionDataType dataType = CompressionDataType.Animation;
        public ImportanceWeights importanceWeights = ImportanceWeights.Default;
        
        [Range(2, 1000)]
        public int fixedControlPointCount = 10; // 固定コントロールポイント数
        public EstimationMethod estimationMethod = EstimationMethod.TotalVariation; // 推定方法
        
        public bool enableTimeMeasurement = false; // 時間計測を有効化
        
        /// <summary>
        /// 許容誤差
        /// </summary>
        public float tolerance
        {
            get => _tolerance;
            set
            {
                ValidationUtils.ValidateTolerance(value, nameof(tolerance));
                _tolerance = value;
            }
        }
        
        /// <summary>
        /// 重要度閾値
        /// </summary>
        public float importanceThreshold
        {
            get => _importanceThreshold;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(importanceThreshold), "Importance threshold must be positive");
                _importanceThreshold = value;
            }
        }
        
        /// <summary>
        /// パラメータのコピーを作成
        /// </summary>
        public CompressionParams Clone()
        {
            return new CompressionParams
            {
                tolerance = this.tolerance,
                importanceThreshold = this.importanceThreshold,
                compressionMethod = this.compressionMethod,
                compressionMode = this.compressionMode,
                dataType = this.dataType,
                importanceWeights = this.importanceWeights,
                fixedControlPointCount = this.fixedControlPointCount,
                estimationMethod = this.estimationMethod,
                enableTimeMeasurement = this.enableTimeMeasurement
            };
        }
        
        /// <summary>
        /// 他のパラメータと等しいかチェック
        /// </summary>
        public bool Equals(CompressionParams other)
        {
            if (other == null) return false;
            
            return tolerance == other.tolerance &&
                   importanceThreshold == other.importanceThreshold &&
                   compressionMethod == other.compressionMethod &&
                   compressionMode == other.compressionMode &&
                   dataType == other.dataType &&
                   fixedControlPointCount == other.fixedControlPointCount &&
                   estimationMethod == other.estimationMethod &&
                   enableTimeMeasurement == other.enableTimeMeasurement;
        }
    }
}
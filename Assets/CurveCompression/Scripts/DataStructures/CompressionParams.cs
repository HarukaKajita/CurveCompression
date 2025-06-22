using System;
using UnityEngine;
using CurveCompression.Core;

namespace CurveCompression.DataStructures
{
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
        
        public CompressionMethod compressionMethod = CompressionMethod.Bezier_Direct; // 圧縮手法
        
        public CompressionDataType dataType = CompressionDataType.Animation;
        public ImportanceWeights importanceWeights = ImportanceWeights.Default;
        
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
                dataType = this.dataType,
                importanceWeights = this.importanceWeights
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
                   dataType == other.dataType;
        }
    }
}
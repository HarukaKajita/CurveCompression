using System;
using UnityEngine;

namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 圧縮パラメータ
    /// </summary>
    [Serializable]
    public class CompressionParams
    {
        [Range(0.001f, 1.0f)]
        public float tolerance = 0.01f;          // 許容誤差
        
        [Range(0.1f, 10.0f)]
        public float importanceThreshold = 1.0f; // 重要度閾値
        
        public CompressionMethod compressionMethod = CompressionMethod.Bezier_Direct; // 圧縮手法
        
        public CompressionDataType dataType = CompressionDataType.Animation;
        public ImportanceWeights importanceWeights = ImportanceWeights.Default;
        
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
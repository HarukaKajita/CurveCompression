using CurveCompression.DataStructures;

namespace CurveCompression.Core
{
    /// <summary>
    /// 圧縮アルゴリズムの標準インターフェース
    /// </summary>
    public interface ICompressionAlgorithm
    {
        /// <summary>
        /// データを圧縮する
        /// </summary>
        /// <param name="points">圧縮対象のデータ</param>
        /// <param name="parameters">圧縮パラメータ</param>
        /// <returns>圧縮された曲線データ</returns>
        CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters);
        
        /// <summary>
        /// アルゴリズム名
        /// </summary>
        string AlgorithmName { get; }
        
        /// <summary>
        /// サポートする圧縮手法
        /// </summary>
        CompressionMethod SupportedMethod { get; }
    }
}
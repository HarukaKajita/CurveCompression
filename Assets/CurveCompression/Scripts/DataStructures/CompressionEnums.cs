namespace CurveCompression.DataStructures
{
    /// <summary>
    /// 圧縮データタイプ
    /// </summary>
    public enum CompressionDataType
    {
        Animation,       // アニメーションデータ
        SensorData,      // センサーデータ
        FinancialData,   // 金融データ
        Custom           // カスタムデータ
    }
    
    /// <summary>
    /// 圧縮手法の選択
    /// </summary>
    public enum CompressionMethod
    {
        RDP,              // RDP線形補間
        BSpline,          // B-スプライン直接近似
        Bezier            // ベジェ直接近似
    }
}
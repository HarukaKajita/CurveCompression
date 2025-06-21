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
        RDP_Linear,        // RDPに基づく線形補間評価
        RDP_BSpline,       // RDPに基づくBSpline評価  
        RDP_Bezier,        // RDPに基づくBezier評価
        BSpline_Direct,    // BSplineによる直接近似
        Bezier_Direct      // Bezierによる直接近似
    }
}
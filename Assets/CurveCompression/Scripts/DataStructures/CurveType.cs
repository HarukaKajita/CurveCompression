namespace CurveCompression.DataStructures
{
    /// <summary>
    /// カーブの種類
    /// </summary>
    public enum CurveType
    {
        Linear,    // 線形補間
        BSpline,   // B-スプライン曲線
        Bezier     // ベジェ曲線
    }
}
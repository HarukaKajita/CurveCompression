using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Visualization
{
    /// <summary>
    /// カーブデータの可視化コンポーネント
    /// </summary>
    public class CurveVisualizer : MonoBehaviour
    {
        [Header("可視化設定")]
        [SerializeField] private Color originalDataColor = Color.blue;
        [SerializeField] private Color compressedDataColor = Color.red;
        [SerializeField] private Color errorVisualizationColor = Color.yellow;
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private float curveHeight = 2.0f;
        [SerializeField] private float timeScale = 1.0f;
        [SerializeField] private float compressedDataYOffset = 0f;
        
        [Header("Gizmo設定")]
        [SerializeField] private bool showControlPoints = true;
        [SerializeField] private Color controlPointColor = Color.green;
        [SerializeField] private float controlPointSize = 0.1f;
        [SerializeField] private bool showControlLines = true;
        [SerializeField] private Color controlLineColor = Color.gray;
        
        private LineRenderer originalLineRenderer;
        private LineRenderer compressedLineRenderer;
        private LineRenderer errorLineRenderer;
        private CompressionResult currentResult;
        
        /// <summary>
        /// 可視化を初期化
        /// </summary>
        public void Initialize()
        {
            // 元データ用LineRenderer
            GameObject originalObject = new GameObject("OriginalData");
            originalObject.transform.SetParent(transform);
            originalLineRenderer = originalObject.AddComponent<LineRenderer>();
            SetupLineRenderer(originalLineRenderer, originalDataColor);
            
            // 圧縮データ用LineRenderer
            GameObject compressedObject = new GameObject("CompressedData");
            compressedObject.transform.SetParent(transform);
            compressedLineRenderer = compressedObject.AddComponent<LineRenderer>();
            SetupLineRenderer(compressedLineRenderer, compressedDataColor);
            
            // 誤差可視化用LineRenderer
            GameObject errorObject = new GameObject("ErrorVisualization");
            errorObject.transform.SetParent(transform);
            errorLineRenderer = errorObject.AddComponent<LineRenderer>();
            SetupLineRenderer(errorLineRenderer, errorVisualizationColor);
        }
        
        /// <summary>
        /// データを可視化
        /// </summary>
        public void VisualizeData(TimeValuePair[] originalData, CompressionResult result)
        {
            currentResult = result;
            
            if (originalLineRenderer == null || compressedLineRenderer == null)
            {
                Initialize();
            }
            
            // 元データの可視化
            DrawCurve(originalLineRenderer, originalData, 0f);
            
            // 圧縮データの可視化
            if (result != null && result.compressedData != null)
            {
                DrawCurve(compressedLineRenderer, result.compressedData, compressedDataYOffset);
                VisualizeError(originalData, result.compressedData);
            }
        }
        
        private void SetupLineRenderer(LineRenderer lineRenderer, Color color)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 1;
        }
        
        private void DrawCurve(LineRenderer lineRenderer, TimeValuePair[] data, float yOffset)
        {
            if (data == null || data.Length == 0) return;
            
            lineRenderer.positionCount = data.Length;
            Vector3[] positions = new Vector3[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                positions[i] = new Vector3(
                    data[i].time * timeScale,
                    data[i].value * curveHeight + yOffset,
                    0
                );
            }
            
            lineRenderer.SetPositions(positions);
        }
        
        private void VisualizeError(TimeValuePair[] originalData, TimeValuePair[] compressedData)
        {
            if (originalData == null || compressedData == null || originalData.Length == 0) return;
            
            errorLineRenderer.positionCount = originalData.Length;
            Vector3[] errorPositions = new Vector3[originalData.Length];
            
            for (int i = 0; i < originalData.Length; i++)
            {
                float interpolatedValue = InterpolateCompressedValue(compressedData, originalData[i].time);
                float error = Mathf.Abs(originalData[i].value - interpolatedValue);
                
                errorPositions[i] = new Vector3(
                    originalData[i].time * timeScale,
                    error * curveHeight + 0.2f, // 誤差は上部に表示
                    0
                );
            }
            
            errorLineRenderer.SetPositions(errorPositions);
        }
        
        private float InterpolateCompressedValue(TimeValuePair[] compressedData, float time)
        {
            if (compressedData.Length == 0) return 0f;
            if (compressedData.Length == 1) return compressedData[0].value;
            
            for (int i = 0; i < compressedData.Length - 1; i++)
            {
                if (time >= compressedData[i].time && time <= compressedData[i + 1].time)
                {
                    float t = (time - compressedData[i].time) / (compressedData[i + 1].time - compressedData[i].time);
                    return Mathf.Lerp(compressedData[i].value, compressedData[i + 1].value, t);
                }
            }
            
            return time < compressedData[0].time ? compressedData[0].value : compressedData[^1].value;
        }
        
        void OnDrawGizmos()
        {
            if (!showControlPoints || currentResult?.compressedCurve?.segments == null)
                return;
                
            DrawControlPointGizmos();
        }
        
        private void DrawControlPointGizmos()
        {
            Gizmos.color = controlPointColor;
            
            foreach (var segment in currentResult.compressedCurve.segments)
            {
                switch (segment.curveType)
                {
                    case CurveType.BSpline:
                        DrawBSplineControlPoints(segment);
                        break;
                        
                    case CurveType.Bezier:
                        DrawBezierControlPoints(segment);
                        break;
                        
                    case CurveType.Linear:
                        DrawLinearControlPoints(segment);
                        break;
                }
            }
        }
        
        private void DrawBSplineControlPoints(CurveSegment segment)
        {
            if (segment.bsplineControlPoints == null) return;
            
            // コントロールポイントを描画
            for (int i = 0; i < segment.bsplineControlPoints.Length; i++)
            {
                var point = segment.bsplineControlPoints[i];
                Vector3 worldPos = new Vector3(
                    point.x * timeScale,
                    point.y * curveHeight,
                    0
                );
                
                Gizmos.DrawSphere(worldPos, controlPointSize);
                
                // コントロールラインを描画
                if (showControlLines && i < segment.bsplineControlPoints.Length - 1)
                {
                    var nextPoint = segment.bsplineControlPoints[i + 1];
                    Vector3 nextWorldPos = new Vector3(
                        nextPoint.x * timeScale,
                        nextPoint.y * curveHeight,
                        0
                    );
                    
                    Gizmos.color = controlLineColor;
                    Gizmos.DrawLine(worldPos, nextWorldPos);
                    Gizmos.color = controlPointColor;
                }
            }
        }
        
        private void DrawBezierControlPoints(CurveSegment segment)
        {
            // 始点と終点
            Vector3 startPos = new Vector3(
                segment.startTime * timeScale,
                segment.startValue * curveHeight,
                0
            );
            Vector3 endPos = new Vector3(
                segment.endTime * timeScale,
                segment.endValue * curveHeight,
                0
            );
            
            Gizmos.DrawSphere(startPos, controlPointSize);
            Gizmos.DrawSphere(endPos, controlPointSize);
            
            if (showControlLines)
            {
                // タンジェントハンドルを描画
                float dt = segment.endTime - segment.startTime;
                float handleLength = dt * 0.3f; // ハンドルの長さを調整
                
                Vector3 inHandle = startPos + new Vector3(
                    handleLength * timeScale,
                    segment.inTangent * handleLength * curveHeight,
                    0
                );
                Vector3 outHandle = endPos - new Vector3(
                    handleLength * timeScale,
                    segment.outTangent * handleLength * curveHeight,
                    0
                );
                
                Gizmos.color = controlLineColor;
                Gizmos.DrawLine(startPos, inHandle);
                Gizmos.DrawLine(endPos, outHandle);
                
                // ハンドルポイント
                Gizmos.color = controlPointColor;
                Gizmos.DrawWireSphere(inHandle, controlPointSize * 0.7f);
                Gizmos.DrawWireSphere(outHandle, controlPointSize * 0.7f);
            }
        }
        
        private void DrawLinearControlPoints(CurveSegment segment)
        {
            // 始点と終点のみ
            Vector3 startPos = new Vector3(
                segment.startTime * timeScale,
                segment.startValue * curveHeight,
                0
            );
            Vector3 endPos = new Vector3(
                segment.endTime * timeScale,
                segment.endValue * curveHeight,
                0
            );
            
            Gizmos.DrawWireSphere(startPos, controlPointSize * 0.5f);
            Gizmos.DrawWireSphere(endPos, controlPointSize * 0.5f);
        }
    }
}
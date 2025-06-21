using UnityEngine;
using CurveCompression.DataStructures;

namespace CurveCompression.Legacy
{
	/// <summary>
	/// B-スプラインセグメント（レガシー実装）
	/// 注: 新しい実装では CurveCompression.DataStructures.CurveSegment を使用してください
	/// </summary>
	[System.Obsolete("Use CurveCompression.DataStructures.CurveSegment instead")]
	public class BSplineSegment
	{
		private Vector2[] controlPoints;

		public float StartTime => controlPoints[0].x;
		public float EndTime => controlPoints[^1].x;
		public float StartValue => controlPoints[0].y;
		public float EndValue => controlPoints[^1].y;

		public BSplineSegment(TimeValuePair start, TimeValuePair end)
		{
			controlPoints = new Vector2[]
			{
				new Vector2(start.time, start.value),
				new Vector2(end.time, end.value)
			};
		}

		public BSplineSegment(Vector2[] controlPoints)
		{
			this.controlPoints = controlPoints;
		}

		public float Evaluate(float t)
		{
			if (controlPoints.Length == 2)
			{
				return Mathf.Lerp(controlPoints[0].y, controlPoints[1].y, t);
			}

			return EvaluateCubicBSpline(t);
		}

		private float EvaluateCubicBSpline(float t)
		{
			if (controlPoints.Length < 4) return controlPoints[0].y;

			float u = Mathf.Clamp01(t);
			float u2 = u * u;
			float u3 = u2 * u;

			float b0 = (1 - u) * (1 - u) * (1 - u) / 6.0f;
			float b1 = (3 * u3 - 6 * u2 + 4) / 6.0f;
			float b2 = (-3 * u3 + 3 * u2 + 3 * u + 1) / 6.0f;
			float b3 = u3 / 6.0f;

			return b0 * controlPoints[0].y + b1 * controlPoints[1].y +
			       b2 * controlPoints[2].y + b3 * controlPoints[3].y;
		}
	}
}
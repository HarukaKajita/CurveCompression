# Algorithm Implementation Details

## Overview

This document provides detailed information about the current algorithm implementations in the CurveCompression library, including their mathematical foundations, implementation specifics, and performance characteristics.

## Compression Algorithms

### 1. Ramer-Douglas-Peucker (RDP) Algorithm

#### Mathematical Foundation
The RDP algorithm simplifies a curve by recursively finding the point with the maximum distance from a line segment and subdividing based on a tolerance threshold.

**Distance Calculation**:
```
For point P and line segment AB:
distance = |((B.y - A.y) * P.x - (B.x - A.x) * P.y + B.x * A.y - B.y * A.x)| / 
           sqrt((B.y - A.y)² + (B.x - A.x)²)
```

#### Implementation Details

**Core Algorithm**:
```csharp
private static List<int> SimplifyIndices(TimeValuePair[] points, float tolerance, 
                                        int startIndex, int endIndex, 
                                        ImportanceWeights weights)
{
    float maxDistance = 0;
    int maxIndex = 0;
    
    // Find the point with maximum distance
    for (int i = startIndex + 1; i < endIndex; i++)
    {
        float distance = PerpendicularDistance(points, i, startIndex, endIndex);
        
        // Apply importance weighting
        float importance = CalculateImportance(points, i, weights);
        distance *= (1.0f + importance * importanceThreshold);
        
        if (distance > maxDistance)
        {
            maxDistance = distance;
            maxIndex = i;
        }
    }
    
    var result = new List<int>();
    
    if (maxDistance > tolerance)
    {
        // Recursive subdivision
        var leftResults = SimplifyIndices(points, tolerance, startIndex, maxIndex, weights);
        var rightResults = SimplifyIndices(points, tolerance, maxIndex, endIndex, weights);
        
        result.AddRange(leftResults);
        result.Add(maxIndex);
        result.AddRange(rightResults);
    }
    
    return result;
}
```

**Importance Calculation**:
```csharp
private static float CalculateImportance(TimeValuePair[] points, int index, ImportanceWeights weights)
{
    if (index <= 0 || index >= points.Length - 1 || weights == null)
        return 0f;
    
    float importance = 0f;
    
    // Curvature importance
    float curvature = CalculateLocalCurvature(points, index);
    importance += curvature * weights.curvatureWeight;
    
    // Velocity importance
    float velocity = CalculateVelocityChange(points, index);
    importance += velocity * weights.velocityWeight;
    
    // Acceleration importance
    float acceleration = CalculateAcceleration(points, index);
    importance += acceleration * weights.accelerationWeight;
    
    // Temporal importance
    float temporalSpacing = CalculateTemporalSpacing(points, index);
    importance += temporalSpacing * weights.temporalWeight;
    
    return Mathf.Clamp01(importance);
}
```

**Performance Characteristics**:
- Time Complexity: O(n log n) average, O(n²) worst case
- Space Complexity: O(log n) for recursion stack
- Memory Usage: Minimal additional allocation

#### Curve Type Support

**Linear Output**:
```csharp
var segments = new List<CurveSegment>();
for (int i = 0; i < keyIndices.Count - 1; i++)
{
    var segment = CurveSegment.CreateLinear(
        points[keyIndices[i]].time, points[keyIndices[i]].value,
        points[keyIndices[i + 1]].time, points[keyIndices[i + 1]].value);
    segments.Add(segment);
}
```

**Bezier/B-Spline Output**:
The RDP algorithm first identifies key points, then fits curves between them using the specified curve type.

### 2. B-Spline Algorithm

#### Mathematical Foundation
B-Spline curves provide smooth interpolation through control points using basis functions.

**B-Spline Basis Function (Degree 3)**:
```
N₀,₃(t) = (1-t)³/6
N₁,₃(t) = (3t³ - 6t² + 4)/6
N₂,₃(t) = (-3t³ + 3t² + 3t + 1)/6
N₃,₃(t) = t³/6
```

#### Implementation Details

**Control Point Selection**:
```csharp
private static Vector2[] SelectControlPoints(TimeValuePair[] points, int numControlPoints)
{
    var controlPoints = new Vector2[numControlPoints];
    
    // Uniform parameter distribution
    for (int i = 0; i < numControlPoints; i++)
    {
        float t = (float)i / (numControlPoints - 1);
        int index = Mathf.RoundToInt(t * (points.Length - 1));
        controlPoints[i] = new Vector2(points[index].time, points[index].value);
    }
    
    return controlPoints;
}
```

**Control Point Optimization**:
```csharp
private static Vector2[] OptimizeControlPoints(TimeValuePair[] originalPoints, Vector2[] initialControlPoints)
{
    const int maxIterations = 10;
    const float convergenceThreshold = 0.0001f;
    
    var optimizedPoints = new Vector2[initialControlPoints.Length];
    Array.Copy(initialControlPoints, optimizedPoints, initialControlPoints.Length);
    
    for (int iteration = 0; iteration < maxIterations; iteration++)
    {
        var gradients = CalculateGradients(originalPoints, optimizedPoints);
        
        // Apply gradient descent
        float stepSize = 0.1f / (iteration + 1);
        bool converged = true;
        
        for (int i = 1; i < optimizedPoints.Length - 1; i++) // Don't move endpoints
        {
            var newPosition = optimizedPoints[i] - gradients[i] * stepSize;
            
            if (Vector2.Distance(newPosition, optimizedPoints[i]) > convergenceThreshold)
                converged = false;
                
            optimizedPoints[i] = newPosition;
        }
        
        if (converged) break;
    }
    
    return optimizedPoints;
}
```

**B-Spline Evaluation**:
```csharp
private static float EvaluateBSplineInternal(Vector2[] controlPoints, float t)
{
    if (controlPoints.Length < 2) return 0f;
    
    // Clamp parameter to valid range
    t = Mathf.Clamp01(t);
    
    // For cubic B-splines with n control points
    int n = controlPoints.Length;
    float scaledT = t * (n - 3); // Scale to control point range
    int baseIndex = Mathf.FloorToInt(scaledT);
    float localT = scaledT - baseIndex;
    
    // Ensure we have enough control points
    baseIndex = Mathf.Clamp(baseIndex, 0, n - 4);
    
    // Cubic B-spline basis functions
    float b0 = (1 - localT) * (1 - localT) * (1 - localT) / 6f;
    float b1 = (3 * localT * localT * localT - 6 * localT * localT + 4) / 6f;
    float b2 = (-3 * localT * localT * localT + 3 * localT * localT + 3 * localT + 1) / 6f;
    float b3 = localT * localT * localT / 6f;
    
    // Interpolate values
    return b0 * controlPoints[baseIndex].y +
           b1 * controlPoints[baseIndex + 1].y +
           b2 * controlPoints[baseIndex + 2].y +
           b3 * controlPoints[baseIndex + 3].y;
}
```

**Performance Characteristics**:
- Time Complexity: O(n) for fitting, O(1) for evaluation
- Space Complexity: O(k) where k is number of control points
- Memory Usage: Control point array storage

### 3. Bezier Algorithm

#### Mathematical Foundation
Bezier curves use control points and tangents to create smooth curves. For cubic Bezier curves:

**Cubic Bezier Formula**:
```
B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃

Where:
P₀, P₃ = endpoints
P₁, P₂ = control points derived from tangents
```

#### Implementation Details

**Tangent Calculation**:
```csharp
private static float CalculateInTangent(TimeValuePair[] points, int start, int end)
{
    const int sampleCount = 3;
    float totalWeight = 0f;
    float weightedSlope = 0f;
    
    for (int i = 0; i < sampleCount && start + i + 1 < end; i++)
    {
        int idx1 = start + i;
        int idx2 = start + i + 1;
        
        if (idx2 < points.Length)
        {
            float dt = points[idx2].time - points[idx1].time;
            if (dt > 0.0001f)
            {
                float weight = 1.0f / (i + 1);
                float slope = (points[idx2].value - points[idx1].value) / dt;
                
                weightedSlope += slope * weight;
                totalWeight += weight;
            }
        }
    }
    
    return totalWeight > 0 ? weightedSlope / totalWeight : 0f;
}

private static float CalculateOutTangent(TimeValuePair[] points, int start, int end)
{
    const int sampleCount = 3;
    float totalWeight = 0f;
    float weightedSlope = 0f;
    
    for (int i = 0; i < sampleCount && end - i - 1 > start; i++)
    {
        int idx1 = end - i - 1;
        int idx2 = end - i;
        
        if (idx2 < points.Length)
        {
            float dt = points[idx2].time - points[idx1].time;
            if (dt > 0.0001f)
            {
                float weight = 1.0f / (i + 1);
                float slope = (points[idx2].value - points[idx1].value) / dt;
                
                weightedSlope += slope * weight;
                totalWeight += weight;
            }
        }
    }
    
    return totalWeight > 0 ? weightedSlope / totalWeight : 0f;
}
```

**Segment Creation**:
```csharp
public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
{
    var segments = new List<CurveSegment>();
    
    // Adaptive segmentation based on curvature
    var segmentIndices = FindOptimalSegmentPoints(points, tolerance);
    
    for (int i = 0; i < segmentIndices.Count - 1; i++)
    {
        int startIdx = segmentIndices[i];
        int endIdx = segmentIndices[i + 1];
        
        float inTangent = CalculateInTangent(points, startIdx, endIdx);
        float outTangent = CalculateOutTangent(points, startIdx, endIdx);
        
        var segment = CurveSegment.CreateBezier(
            points[startIdx].time, points[startIdx].value,
            points[endIdx].time, points[endIdx].value,
            inTangent, outTangent);
            
        segments.Add(segment);
    }
    
    return new CompressedCurveData(segments.ToArray());
}
```

**Performance Characteristics**:
- Time Complexity: O(n) for segment creation, O(1) for evaluation
- Space Complexity: O(s) where s is number of segments
- Memory Usage: 4 floats per segment (start, end, in/out tangents)

## Control Point Estimation Algorithms

### 1. Elbow Method

#### Mathematical Foundation
Finds the "elbow" point in the error vs control point count curve using second derivative analysis.

**Second Derivative Calculation**:
```csharp
private static EstimationResult EstimateByElbowMethod(TimeValuePair[] data, float tolerance, 
                                                     int minPoints, int maxPoints)
{
    var errors = new List<float>();
    
    // Calculate errors for each control point count
    for (int n = minPoints; n <= maxPoints; n++)
    {
        var compressed = BSplineAlgorithm.ApproximateWithFixedPoints(data, n);
        float error = CalculateMeanSquaredError(data, compressed);
        errors.Add(error);
    }
    
    // Calculate second derivatives
    var secondDerivatives = new List<float>();
    for (int i = 1; i < errors.Count - 1; i++)
    {
        float d2 = errors[i + 1] - 2 * errors[i] + errors[i - 1];
        secondDerivatives.Add(Mathf.Abs(d2));
    }
    
    // Find maximum curvature point
    int elbowIndex = 0;
    float maxCurvature = 0;
    for (int i = 0; i < secondDerivatives.Count; i++)
    {
        if (secondDerivatives[i] > maxCurvature)
        {
            maxCurvature = secondDerivatives[i];
            elbowIndex = i + 1;
        }
    }
    
    return new EstimationResult(minPoints + elbowIndex, errors[elbowIndex], "Elbow Method");
}
```

### 2. Curvature Analysis

#### Mathematical Foundation
Analyzes local curvature distribution to estimate required control points.

**Curvature Calculation**:
```csharp
private static float CalculateLocalCurvature(TimeValuePair[] data, int index)
{
    if (index <= 0 || index >= data.Length - 1) return 0;
    
    Vector2 prev = new Vector2(data[index - 1].time, data[index - 1].value);
    Vector2 curr = new Vector2(data[index].time, data[index].value);
    Vector2 next = new Vector2(data[index + 1].time, data[index + 1].value);
    
    Vector2 v1 = (curr - prev).normalized;
    Vector2 v2 = (next - curr).normalized;
    
    float dot = Vector2.Dot(v1, v2);
    float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
    
    return angle;
}
```

**Distribution Analysis**:
```csharp
private static EstimationResult EstimateByCurvature(TimeValuePair[] data, float tolerance, 
                                                   int minPoints, int maxPoints)
{
    float totalCurvature = 0;
    var curvatures = new List<float>();
    
    // Calculate all curvatures
    for (int i = 1; i < data.Length - 1; i++)
    {
        float curvature = CalculateLocalCurvature(data, i);
        curvatures.Add(curvature);
        totalCurvature += curvature;
    }
    
    // Sort curvatures in descending order
    curvatures.Sort((a, b) => b.CompareTo(a));
    
    // Find points that capture 90% of total curvature
    float cumulativeCurvature = 0;
    int significantPoints = 0;
    float threshold = totalCurvature * 0.9f;
    
    foreach (float c in curvatures)
    {
        cumulativeCurvature += c;
        significantPoints++;
        if (cumulativeCurvature >= threshold)
            break;
    }
    
    // Scale to reasonable range
    int optimalPoints = Mathf.Clamp(
        Mathf.RoundToInt(significantPoints * 0.5f + minPoints),
        minPoints, maxPoints);
    
    return new EstimationResult(optimalPoints, totalCurvature, "Curvature Based");
}
```

### 3. Information Entropy

#### Mathematical Foundation
Uses information theory to measure data complexity and determine required control points.

**Entropy Calculation**:
```csharp
private static float CalculateEntropy(TimeValuePair[] data)
{
    if (data.Length <= 1) return 0;
    
    // Create histogram
    int bins = Mathf.Max(1, Mathf.Min(20, data.Length / 5));
    float min = data.Min(p => p.value);
    float max = data.Max(p => p.value);
    float range = max - min;
    
    if (range < 0.0001f) return 0;
    
    var histogram = new int[bins];
    foreach (var point in data)
    {
        int bin = Mathf.Clamp((int)((point.value - min) / range * (bins - 1)), 0, bins - 1);
        histogram[bin]++;
    }
    
    // Calculate Shannon entropy
    float entropy = 0;
    float total = data.Length;
    
    foreach (int count in histogram)
    {
        if (count > 0)
        {
            float p = count / total;
            entropy -= p * Mathf.Log(p, 2);
        }
    }
    
    return entropy;
}
```

### 4. Statistical Analysis

#### Mathematical Foundation
Uses signal-to-noise ratio and variance analysis to determine optimal control point count.

**SNR Calculation**:
```csharp
private static EstimationResult DetermineByStatistical(TimeValuePair[] data, float tolerance)
{
    // Basic statistics
    float mean = data.Average(p => p.value);
    float variance = data.Average(p => (p.value - mean) * (p.value - mean));
    float stdDev = Mathf.Sqrt(variance);
    
    // Noise level estimation
    float noiseLevel = EstimateNoiseLevel(data);
    
    // Signal-to-noise ratio
    float snr = stdDev / (noiseLevel + 0.0001f);
    
    // Adaptive upper limit based on SNR
    int maxPoints = Mathf.Clamp(
        Mathf.RoundToInt(10 + snr * 5),
        10,
        Mathf.Min(data.Length / 2, 200));
    
    var result = new EstimationResult(maxPoints, snr, "Statistical");
    result.metrics["variance"] = variance;
    result.metrics["noise_level"] = noiseLevel;
    result.metrics["snr"] = snr;
    
    return result;
}
```

## Intelligent Selection Systems

### Algorithm Selection Logic

#### Data Analysis Pipeline
```csharp
public static DataAnalysis AnalyzeDataCharacteristics(TimeValuePair[] data)
{
    var analysis = new DataAnalysis();
    
    // 1. Smoothness calculation
    analysis.smoothness = CalculateSmoothness(data);
    
    // 2. Complexity measurement
    analysis.complexity = CalculateComplexity(data);
    
    // 3. Noise level estimation
    analysis.noiseLevel = EstimateNoiseLevel(data);
    
    // 4. Variability calculation
    float mean = CalculateMean(data);
    float stdDev = Mathf.Sqrt(CalculateVariance(data, mean));
    analysis.variability = stdDev / (Mathf.Abs(mean) + 0.001f);
    
    // 5. Temporal density
    analysis.temporalDensity = CalculateTemporalDensity(data);
    
    // 6. Data type classification
    analysis.recommendedDataType = ClassifyDataType(analysis);
    
    return analysis;
}
```

#### Algorithm Scoring
```csharp
private static Dictionary<CompressionMethod, float> CalculateAlgorithmScores(
    DataAnalysis analysis, CompressionParams parameters)
{
    var scores = new Dictionary<CompressionMethod, float>();
    
    // RDP_Linear: Fast, noise-resistant
    scores[CompressionMethod.RDP_Linear] = 
        0.8f - analysis.complexity * 0.4f +      // Simpler data preferred
        analysis.noiseLevel * 0.3f +             // Good with noise
        (1f - analysis.smoothness) * 0.2f;       // Better for non-smooth data
    
    // Bezier_Direct: High quality, smooth data
    scores[CompressionMethod.Bezier_Direct] = 
        0.8f + analysis.smoothness * 0.5f +      // Excellent for smooth data
        (analysis.recommendedDataType == CompressionDataType.Animation ? 0.4f : 0f) +
        (1f - analysis.noiseLevel) * 0.3f;       // Requires low noise
    
    // Apply performance adjustments
    if (parameters != null)
    {
        AdjustScoresForPerformance(scores, analysis, parameters);
    }
    
    // Normalize scores
    NormalizeScores(scores);
    
    return scores;
}
```

### Adaptive Tolerance System

#### Quality-Based Tolerance
```csharp
private static float CalculateBaseTolerance(DataToleranceAnalysis analysis, QualityLevel quality)
{
    // Base ratio relative to data range
    float baseRatio = quality switch
    {
        QualityLevel.Low => 0.05f,      // 5% of range
        QualityLevel.Medium => 0.01f,   // 1% of range
        QualityLevel.High => 0.002f,    // 0.2% of range
        QualityLevel.Lossless => 0.0001f, // 0.01% of range
        _ => 0.01f
    };
    
    // Minimum absolute tolerance
    float minAbsoluteTolerance = quality switch
    {
        QualityLevel.Low => 0.01f,
        QualityLevel.Medium => 0.001f,
        QualityLevel.High => 0.0001f,
        QualityLevel.Lossless => 0.00001f,
        _ => 0.001f
    };
    
    float relativeTolerance = analysis.range * baseRatio;
    return Mathf.Max(relativeTolerance, minAbsoluteTolerance);
}
```

#### Noise-Based Adjustment
```csharp
private static float AdjustToleranceForNoise(float baseTolerance, float noiseLevel)
{
    if (noiseLevel < 0.001f)
        return baseTolerance * 0.5f;      // Very low noise: tighter tolerance
    else if (noiseLevel < 0.01f)
        return baseTolerance * 0.75f;     // Low noise: somewhat tighter
    else if (noiseLevel > 0.1f)
        return baseTolerance * 2.0f;      // High noise: looser tolerance
    else if (noiseLevel > 0.05f)
        return baseTolerance * 1.5f;      // Moderate noise: moderately looser
    
    return baseTolerance;
}
```

## Performance Optimization Techniques

### Memory Management
- **Struct usage**: Value types for frequently-copied data
- **Array pooling**: Reuse temporary arrays
- **In-place operations**: Minimize allocations
- **Reference counting**: Avoid unnecessary object creation

### Computational Optimization
- **Binary search**: O(log n) lookups in InterpolationUtils
- **Early termination**: Skip unnecessary calculations
- **Vectorized operations**: SIMD-friendly calculations where possible
- **Adaptive sampling**: Variable resolution based on complexity

### Caching Strategies
- **Result caching**: Cache compression results for identical inputs
- **Metric caching**: Cache expensive analysis calculations
- **Algorithm selection caching**: Cache recommendation results

This comprehensive algorithm implementation provides robust, high-performance curve compression with intelligent adaptation to data characteristics.
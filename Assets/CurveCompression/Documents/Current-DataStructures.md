# Current Data Structures

## Overview

This document describes the current data structures in the CurveCompression library after comprehensive refactoring. These structures provide type safety, performance optimization, and clear data flow patterns.

## Core Data Types

### TimeValuePair
**Location**: `CurveCompression.DataStructures.TimeValuePair`
**Type**: `struct` (Value Type)

```csharp
[Serializable]
public struct TimeValuePair : IComparable<TimeValuePair>
{
    public float time;
    public float value;
    
    public TimeValuePair(float time, float value);
    public int CompareTo(TimeValuePair other);
    public override string ToString();
    public override bool Equals(object obj);
    public override int GetHashCode();
}
```

**Design Characteristics**:
- **Value Type**: Minimizes memory allocations and GC pressure
- **Immutable**: Data cannot be modified after creation (data integrity)
- **Comparable**: Supports sorting by time for data processing
- **Serializable**: Unity Inspector and asset serialization support
- **Memory Efficient**: 8 bytes per instance (2 × float)

**Usage Patterns**:
```csharp
// Creation
var point = new TimeValuePair(1.5f, 0.8f);

// Array operations
var dataArray = new TimeValuePair[1000];
Array.Sort(dataArray); // Automatic time-based sorting

// Comparison
bool isEarlier = point1.CompareTo(point2) < 0;
```

### CompressionParams
**Location**: `CurveCompression.DataStructures.CompressionParams`
**Type**: `class` (Reference Type)

```csharp
[Serializable]
public class CompressionParams
{
    // Validated properties
    public float tolerance { get; set; }                    // 0.0001f to 1.0f
    public float importanceThreshold { get; set; }          // 0.1f to 10.0f
    
    // Direct fields
    public CompressionMethod compressionMethod;             // Algorithm selection
    public CompressionDataType dataType;                   // Data classification
    public ImportanceWeights importanceWeights;            // Algorithm weighting
    
    // Methods
    public CompressionParams();
    public CompressionParams Clone();
    public bool Equals(CompressionParams other);
    public override int GetHashCode();
}
```

**Validation System**:
```csharp
public float tolerance
{
    get => _tolerance;
    set
    {
        ValidationUtils.ValidateTolerance(value, nameof(tolerance));
        _tolerance = value;
    }
}

public float importanceThreshold
{
    get => _importanceThreshold;
    set
    {
        ValidationUtils.ValidateRange(value, 0.1f, 10.0f, nameof(importanceThreshold));
        _importanceThreshold = value;
    }
}
```

**Design Features**:
- **Property Validation**: Automatic validation on assignment
- **Serialization**: Unity Inspector editing support
- **Cloneable**: Deep copy capability for parameter variations
- **Equality**: Comparison support for caching and optimization

### CompressionResult
**Location**: `CurveCompression.DataStructures.CompressionResult`
**Type**: `class` (Reference Type)

```csharp
public class CompressionResult
{
    // Core results
    public TimeValuePair[] compressedData;      // Legacy format compatibility
    public CompressedCurveData compressedCurve; // Modern curve representation
    
    // Quality metrics
    public float compressionRatio;              // Compression effectiveness (0-1)
    public float maxError;                      // Maximum deviation
    public float avgError;                      // Average deviation
    public float rmseError;                     // Root mean square error
    
    // Count metrics
    public int originalCount;                   // Original data point count
    public int compressedCount;                 // Compressed segment/point count
    
    // Performance metrics
    public TimeSpan compressionTime;            // Processing time
    public long memoryUsed;                     // Memory consumption
    
    // Constructors
    public CompressionResult(TimeValuePair[] original, TimeValuePair[] compressed);
    public CompressionResult(TimeValuePair[] original, CompressedCurveData compressed);
    
    // Utility methods
    public float InterpolateValue(float time);
    public bool IsWithinTolerance(float tolerance);
}
```

**Metric Calculations**:
```csharp
private void CalculateErrorsWithCurve(TimeValuePair[] original, CompressedCurveData compressed)
{
    float totalError = 0f;
    float totalSquaredError = 0f;
    maxError = 0f;
    
    for (int i = 0; i < original.Length; i++)
    {
        float curveValue = compressed.Evaluate(original[i].time);
        float error = Mathf.Abs(original[i].value - curveValue);
        
        totalError += error;
        totalSquaredError += error * error;
        maxError = Mathf.Max(maxError, error);
    }
    
    avgError = totalError / original.Length;
    rmseError = Mathf.Sqrt(totalSquaredError / original.Length);
}
```

### CompressedCurveData
**Location**: `CurveCompression.DataStructures.CompressedCurveData`
**Type**: `class` (Reference Type)

```csharp
[Serializable]
public class CompressedCurveData
{
    public CurveSegment[] segments;
    
    // Constructor
    public CompressedCurveData(CurveSegment[] segments);
    
    // Evaluation methods
    public float Evaluate(float time);
    public Vector2 EvaluateAsVector2(float time);
    
    // Conversion methods
    public TimeValuePair[] ToTimeValuePairs(int sampleCount);
    public Vector2[] ToVector2Array(int sampleCount);
    public AnimationCurve ToAnimationCurve();
    
    // Analysis methods
    public float GetMinTime();
    public float GetMaxTime();
    public float GetValueRange();
    public int GetSegmentCount();
}
```

**Evaluation Logic**:
```csharp
public float Evaluate(float time)
{
    // Find appropriate segment
    foreach (var segment in segments)
    {
        if (time >= segment.startTime && time <= segment.endTime)
        {
            return segment.Evaluate(time);
        }
    }
    
    // Handle extrapolation
    if (time < segments[0].startTime)
        return segments[0].startValue;
    else
        return segments[segments.Length - 1].endValue;
}
```

### CurveSegment
**Location**: `CurveCompression.DataStructures.CurveSegment`
**Type**: `struct` (Value Type)

```csharp
[Serializable]
public struct CurveSegment
{
    // Common properties
    public CurveType curveType;
    public float startTime, startValue;
    public float endTime, endValue;
    
    // Bezier-specific
    public float inTangent, outTangent;
    
    // B-Spline specific
    public Vector2[] bsplineControlPoints;
    
    // Factory methods
    public static CurveSegment CreateLinear(float startTime, float startValue, 
                                           float endTime, float endValue);
    public static CurveSegment CreateBezier(float startTime, float startValue, 
                                           float endTime, float endValue,
                                           float inTangent, float outTangent);
    public static CurveSegment CreateBSpline(Vector2[] controlPoints);
    
    // Evaluation
    public float Evaluate(float time);
    
    // Utility
    public bool ContainsTime(float time);
    public float GetDuration();
    public Vector2 GetStartPoint();
    public Vector2 GetEndPoint();
}
```

**Curve Type Evaluation**:
```csharp
public float Evaluate(float time)
{
    float t = MathUtils.SafeLerpParameter(time, startTime, endTime);
    
    return curveType switch
    {
        CurveType.Linear => EvaluateLinear(t),
        CurveType.Bezier => EvaluateBezier(t),
        CurveType.BSpline => EvaluateBSpline(t),
        _ => Mathf.Lerp(startValue, endValue, t)
    };
}

private float EvaluateLinear(float t)
{
    return Mathf.Lerp(startValue, endValue, t);
}

private float EvaluateBezier(float t)
{
    float dt = endTime - startTime;
    return InterpolationUtils.HermiteInterpolate(
        startValue, endValue,
        inTangent * dt, outTangent * dt, t);
}

private float EvaluateBSpline(float t)
{
    if (bsplineControlPoints == null || bsplineControlPoints.Length < 2)
        return Mathf.Lerp(startValue, endValue, t);
    
    return EvaluateBSplineInternal(bsplineControlPoints, t);
}
```

## Configuration and Enumeration Types

### CompressionMethod
```csharp
public enum CompressionMethod
{
    RDP_Linear,      // RDP with linear segments
    RDP_BSpline,     // RDP with B-Spline evaluation
    RDP_Bezier,      // RDP with Bezier evaluation
    BSpline_Direct,  // Direct B-Spline approximation
    Bezier_Direct    // Direct Bezier approximation
}
```

### CompressionDataType
```csharp
public enum CompressionDataType
{
    Animation,      // Smooth animation curves
    SensorData,     // Noisy sensor readings
    FinancialData   // Financial time series
}
```

### CurveType
```csharp
public enum CurveType
{
    Linear,    // Linear interpolation
    BSpline,   // B-Spline curves
    Bezier     // Bezier curves
}
```

### ImportanceWeights
**Location**: `CurveCompression.DataStructures.ImportanceWeights`
**Type**: `class` (Reference Type)

```csharp
[Serializable]
public class ImportanceWeights
{
    public float curvatureWeight = 1.0f;      // Curvature importance
    public float velocityWeight = 1.0f;       // Velocity change importance
    public float accelerationWeight = 1.0f;   // Acceleration importance
    public float temporalWeight = 1.0f;       // Temporal spacing importance
    
    // Predefined configurations
    public static ImportanceWeights Default => new ImportanceWeights();
    public static ImportanceWeights ForAnimation => new ImportanceWeights
    {
        curvatureWeight = 2.0f,
        velocityWeight = 1.5f,
        accelerationWeight = 1.0f,
        temporalWeight = 0.8f
    };
    public static ImportanceWeights ForSensorData => new ImportanceWeights
    {
        curvatureWeight = 1.0f,
        velocityWeight = 2.0f,
        accelerationWeight = 1.5f,
        temporalWeight = 1.2f
    };
    public static ImportanceWeights ForFinancialData => new ImportanceWeights
    {
        curvatureWeight = 3.0f,
        velocityWeight = 2.0f,
        accelerationWeight = 2.5f,
        temporalWeight = 1.0f
    };
    
    // Methods
    public ImportanceWeights Clone();
    public bool Equals(ImportanceWeights other);
}
```

## Analysis and Recommendation Types

### AlgorithmSelector.DataAnalysis
```csharp
public struct DataAnalysis
{
    public float smoothness;         // Smoothness measure (0-1)
    public float complexity;         // Data complexity (0-1)
    public float noiseLevel;         // Noise estimation (0-1)
    public float variability;        // Value variability (0+)
    public float temporalDensity;    // Points per time unit
    public CompressionDataType recommendedDataType; // Classified data type
}
```

### AlgorithmSelector.AlgorithmRecommendation
```csharp
public struct AlgorithmRecommendation
{
    public CompressionMethod primaryMethod;    // Best algorithm
    public CompressionMethod fallbackMethod;  // Alternative algorithm
    public float confidence;                   // Confidence (0-1)
    public string reasoning;                   // Selection explanation
    public Dictionary<string, float> scores;  // All algorithm scores
}
```

### AdaptiveTolerance.QualityLevel
```csharp
public enum QualityLevel
{
    Low,        // Fast compression, lower quality
    Medium,     // Balanced speed and quality
    High,       // High quality compression
    Lossless    // Maximum quality preservation
}
```

### AdaptiveTolerance.AdaptiveToleranceResult
```csharp
public struct AdaptiveToleranceResult
{
    public float recommendedTolerance;    // Calculated tolerance
    public float minTolerance;           // Minimum recommended
    public float maxTolerance;           // Maximum recommended
    public float dataRange;              // Input data range
    public float noiseLevel;             // Estimated noise
    public string reasoning;             // Calculation explanation
    public Dictionary<string, float> metrics; // Detailed metrics
}
```

### ControlPointEstimator.EstimationResult
```csharp
public class EstimationResult
{
    public int optimalPoints;                    // Recommended point count
    public float score;                          // Quality score
    public string method;                        // Estimation method
    public Dictionary<string, float> metrics;    // Method-specific metrics
    
    public EstimationResult(int points, float score, string method);
}
```

## Memory Layout and Performance

### Memory Characteristics
```
Data Structure          Size        Type        Usage Pattern
TimeValuePair          8 bytes      Value       High-frequency creation
CurveSegment          ~40 bytes     Value       Moderate creation
CompressionParams     ~64 bytes     Reference   Low-frequency creation
CompressionResult     ~100+ bytes   Reference   Per-compression
CompressedCurveData   Variable      Reference   Result storage
```

### Performance Considerations

#### Value vs Reference Types
- **Value Types (struct)**: Used for small, frequently-copied data
  - `TimeValuePair`: Core data unit
  - `CurveSegment`: Curve representation unit
  - Analysis result structures

- **Reference Types (class)**: Used for larger, shared data
  - `CompressionParams`: Configuration objects
  - `CompressionResult`: Complex result data
  - `CompressedCurveData`: Variable-size curve data

#### Memory Optimization Strategies
```csharp
// Array pooling for temporary arrays
var tempArray = ArrayPool<TimeValuePair>.Shared.Rent(size);
try
{
    // Use array
}
finally
{
    ArrayPool<TimeValuePair>.Shared.Return(tempArray);
}

// Struct reuse for parameter objects
var segment = CurveSegment.CreateLinear(start.time, start.value, end.time, end.value);

// Reference sharing for configuration
var sharedParams = new CompressionParams { tolerance = 0.01f };
// Use sharedParams for multiple compressions
```

## Serialization and Unity Integration

### Unity Serialization Support
```csharp
[Serializable]  // Enable Unity serialization
public struct TimeValuePair { /* ... */ }

[Serializable]
public class CompressionParams { /* ... */ }

// Custom PropertyDrawer support
[CustomPropertyDrawer(typeof(CompressionParams))]
public class CompressionParamsDrawer : PropertyDrawer { /* ... */ }
```

### Inspector Integration
```csharp
public class CompressionDemo : MonoBehaviour
{
    [SerializeField] private CompressionParams parameters;
    [SerializeField] private TimeValuePair[] testData;
    [SerializeField] private AdaptiveTolerance.QualityLevel quality;
    
    // Automatic Inspector editing support
}
```

## Extension Patterns

### Adding New Curve Types
1. **Add to CurveType enum**:
   ```csharp
   public enum CurveType
   {
       Linear, BSpline, Bezier,
       NewCurveType  // Add new type
   }
   ```

2. **Extend CurveSegment evaluation**:
   ```csharp
   public float Evaluate(float time)
   {
       return curveType switch
       {
           // ... existing cases
           CurveType.NewCurveType => EvaluateNewCurveType(time),
           _ => defaultEvaluation
       };
   }
   ```

3. **Add factory method**:
   ```csharp
   public static CurveSegment CreateNewCurveType(/* parameters */)
   {
       return new CurveSegment
       {
           curveType = CurveType.NewCurveType,
           // ... set other fields
       };
   }
   ```

### Adding New Metrics
1. **Extend CompressionResult**:
   ```csharp
   public class CompressionResult
   {
       // ... existing metrics
       public float newMetric;
   }
   ```

2. **Implement calculation**:
   ```csharp
   private void CalculateNewMetric(TimeValuePair[] original, CompressedCurveData compressed)
   {
       // Calculation logic
       newMetric = calculatedValue;
   }
   ```

This data structure design provides excellent performance, type safety, and extensibility while maintaining clear separation of concerns and Unity integration compatibility.
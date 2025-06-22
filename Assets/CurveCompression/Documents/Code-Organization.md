# Code Organization and Structure

## Project Structure

The CurveCompression project is organized into logical modules with clear separation of concerns and responsibilities.

```
Assets/CurveCompression/
├── Documents/                          # Documentation files
│   ├── Current-Architecture.md         # Architecture overview
│   ├── Code-Organization.md           # This file
│   ├── Algorithm-Details.md           # Algorithm implementations
│   ├── Current-DataStructures.md      # Data structures documentation
│   └── Developer-Setup.md             # Setup and extension guide
├── Scripts/
│   ├── Core/                          # Core functionality
│   │   ├── CurveCompressor.cs         # Main API facade
│   │   ├── HybridCompressor.cs        # Algorithm routing
│   │   ├── AlgorithmSelector.cs       # Intelligent algorithm selection
│   │   ├── AdaptiveTolerance.cs       # Adaptive tolerance calculation
│   │   ├── ValidationUtils.cs         # Input validation utilities
│   │   ├── MathUtils.cs              # Mathematical utilities
│   │   └── InterpolationUtils.cs      # Interpolation algorithms
│   ├── DataStructures/               # Data models and types
│   │   ├── TimeValuePair.cs          # Basic data point
│   │   ├── CompressionParams.cs      # Configuration parameters
│   │   ├── CompressionResult.cs      # Results and metrics
│   │   ├── CompressedCurveData.cs    # Compressed curve representation
│   │   ├── CurveSegment.cs           # Individual curve segments
│   │   └── ImportanceWeights.cs      # Algorithm weighting
│   ├── Algorithms/                   # Algorithm implementations
│   │   ├── RDPAlgorithm.cs          # Ramer-Douglas-Peucker
│   │   ├── BSplineAlgorithm.cs      # B-Spline approximation
│   │   ├── BezierAlgorithm.cs       # Bezier curve fitting
│   │   └── ControlPointEstimator.cs  # Optimal point estimation
│   ├── Visualization/               # Visualization components
│   │   ├── CurveVisualizer.cs       # Curve rendering
│   │   └── CurveCompressionDemo.cs  # Demo and testing
│   └── Unity/                       # Unity-specific integration
│       └── UnityCompressionUtils.cs # Unity integration utilities
└── CurveCompression.asmdef          # Assembly definition
```

## Namespace Organization

### Core Namespaces

#### `CurveCompression.Core`
**Purpose**: Primary API and core functionality
**Key Classes**:
- `CurveCompressor`: Main entry point for all compression operations
- `HybridCompressor`: Algorithm selection and routing logic
- `AlgorithmSelector`: Intelligent algorithm recommendation system
- `AdaptiveTolerance`: Data-driven tolerance calculation
- `ValidationUtils`: Comprehensive input validation
- `MathUtils`: Safe mathematical operations
- `InterpolationUtils`: Optimized interpolation algorithms

#### `CurveCompression.DataStructures`
**Purpose**: Data models, configuration, and results
**Key Classes**:
- `TimeValuePair`: Fundamental time-value data point
- `CompressionParams`: Compression configuration and settings
- `CompressionResult`: Compression results with metrics
- `CompressedCurveData`: Compressed curve representation
- `CurveSegment`: Individual curve segment implementation
- `ImportanceWeights`: Algorithm importance weighting

#### `CurveCompression.Algorithms`
**Purpose**: Algorithm implementations and estimation
**Key Classes**:
- `RDPAlgorithm`: Ramer-Douglas-Peucker line simplification
- `BSplineAlgorithm`: B-Spline curve approximation
- `BezierAlgorithm`: Bezier curve fitting
- `ControlPointEstimator`: Optimal control point estimation

#### `CurveCompression.Visualization`
**Purpose**: Visual debugging and demonstration
**Key Classes**:
- `CurveVisualizer`: Real-time curve visualization
- `CurveCompressionDemo`: Interactive demonstration

## Class Responsibilities

### Core Classes

#### CurveCompressor
```csharp
namespace CurveCompression.Core
{
    public static class CurveCompressor
    {
        // Standard compression interface
        public static CompressionResult Compress(TimeValuePair[], CompressionParams)
        public static CompressionResult Compress(TimeValuePair[], float tolerance)
        
        // Intelligent compression methods
        public static CompressionResult CompressWithAutoSelection(TimeValuePair[], float)
        public static CompressionResult CompressWithQualityLevel(TimeValuePair[], QualityLevel)
        public static CompressionResult CompressWithTargetRatio(TimeValuePair[], float)
        
        // Analysis and recommendation
        public static AlgorithmRecommendation GetAlgorithmRecommendation(TimeValuePair[], CompressionParams)
        public static DataAnalysis AnalyzeData(TimeValuePair[])
        public static AdaptiveToleranceResult GetAdaptiveTolerance(TimeValuePair[], QualityLevel)
    }
}
```

**Responsibilities**:
- Unified API facade for all compression operations
- Input validation and error handling
- Integration of intelligent systems
- Performance optimization coordination

#### AlgorithmSelector
```csharp
namespace CurveCompression.Core
{
    public static class AlgorithmSelector
    {
        // Data analysis
        public static DataAnalysis AnalyzeDataCharacteristics(TimeValuePair[])
        
        // Algorithm recommendation
        public static AlgorithmRecommendation SelectBestAlgorithm(TimeValuePair[], CompressionParams)
        
        // Supporting structures
        public struct DataAnalysis { /* smoothness, complexity, noise, etc. */ }
        public struct AlgorithmRecommendation { /* method, confidence, reasoning */ }
    }
}
```

**Responsibilities**:
- Data characteristic analysis (smoothness, complexity, noise)
- Algorithm performance scoring
- Intelligent algorithm recommendation
- Detailed reasoning generation

#### AdaptiveTolerance
```csharp
namespace CurveCompression.Core
{
    public static class AdaptiveTolerance
    {
        // Tolerance calculation
        public static AdaptiveToleranceResult CalculateAdaptiveTolerance(TimeValuePair[], QualityLevel, float?)
        public static float CalculateToleranceForCompressionRatio(TimeValuePair[], float, CompressionMethod)
        
        // Quality-based compression
        public static CompressionResult CompressWithQualityLevel(TimeValuePair[], QualityLevel, CompressionMethod?)
        
        // Supporting enums and structures
        public enum QualityLevel { Low, Medium, High, Lossless }
        public struct AdaptiveToleranceResult { /* tolerance, reasoning, metrics */ }
    }
}
```

**Responsibilities**:
- Data-driven tolerance calculation
- Quality level abstraction
- Compression ratio targeting
- Performance vs quality optimization

### Utility Classes

#### ValidationUtils
```csharp
namespace CurveCompression.Core
{
    public static class ValidationUtils
    {
        // Point validation
        public static void ValidatePoints(TimeValuePair[], string, int minRequired = 2)
        
        // Parameter validation
        public static void ValidateTolerance(float, string)
        public static void ValidateRange(float, float, float, string)
        public static void ValidateCompressionParams(CompressionParams)
        public static void ValidateControlPointCount(int, int, string)
    }
}
```

**Responsibilities**:
- Comprehensive input validation
- Clear error messaging
- Consistent validation patterns
- Parameter boundary checking

#### MathUtils
```csharp
namespace CurveCompression.Core
{
    public static class MathUtils
    {
        // Safe mathematical operations
        public static float SafeDivide(float, float, float defaultValue = 0f)
        public static float SafeSlope(float x1, float y1, float x2, float y2)
        public static float SafeLerpParameter(float value, float start, float end)
        
        // Geometric calculations
        public static float DistanceSquared(Vector2, Vector2)
        public static float PerpendicularDistance(Vector2, Vector2, Vector2)
    }
}
```

**Responsibilities**:
- Zero-division protection
- Floating-point safety
- Geometric calculations
- Numerical stability

#### InterpolationUtils
```csharp
namespace CurveCompression.Core
{
    public static class InterpolationUtils
    {
        // Linear interpolation
        public static float LinearInterpolate(TimeValuePair[], float time)
        
        // Bezier curves
        public static Vector2 QuadraticBezier(Vector2, Vector2, Vector2, float)
        public static Vector2 CubicBezier(Vector2, Vector2, Vector2, Vector2, float)
        
        // Advanced interpolation
        public static float HermiteInterpolate(float, float, float, float, float)
        public static float MonotonicCubicInterpolate(TimeValuePair[], float)
    }
}
```

**Responsibilities**:
- High-performance interpolation
- Multiple interpolation methods
- Binary search optimization
- Monotonic interpolation preservation

## Algorithm Implementation Structure

### Common Algorithm Interface

All compression algorithms follow a standardized interface pattern:

```csharp
public static class [Algorithm]Algorithm
{
    // Standard interface
    public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
    public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
    
    // Algorithm-specific methods
    public static CompressedCurveData CompressWithSpecificFeature(...)
    
    // Legacy compatibility (where needed)
    public static TimeValuePair[] LegacyMethod(...)
}
```

### Algorithm Implementations

#### RDPAlgorithm
```csharp
namespace CurveCompression.Algorithms
{
    public static class RDPAlgorithm
    {
        // Standard interface
        public static CompressedCurveData Compress(TimeValuePair[], CompressionParams)
        public static CompressedCurveData Compress(TimeValuePair[], float)
        
        // Advanced features
        public static CompressedCurveData CompressWithCurveEvaluation(
            TimeValuePair[], float, CurveType, float, ImportanceWeights)
        
        // Internal methods
        private static List<int> SimplifyIndices(TimeValuePair[], float, int, int, ImportanceWeights)
        private static float PerpendicularDistance(TimeValuePair[], int, int, int)
        private static float CalculateImportance(TimeValuePair[], int, ImportanceWeights)
    }
}
```

#### BSplineAlgorithm
```csharp
namespace CurveCompression.Algorithms
{
    public static class BSplineAlgorithm
    {
        // Standard interface
        public static CompressedCurveData Compress(TimeValuePair[], CompressionParams)
        public static CompressedCurveData Compress(TimeValuePair[], float)
        
        // Fixed control points
        public static CompressedCurveData CompressWithFixedControlPoints(TimeValuePair[], int)
        
        // Legacy compatibility
        public static TimeValuePair[] ApproximateWithFixedPoints(TimeValuePair[], int)
        
        // Internal methods
        private static Vector2[] SelectControlPoints(TimeValuePair[], int)
        private static Vector2[] OptimizeControlPoints(TimeValuePair[], Vector2[])
        private static float EvaluateBSpline(Vector2[], float)
    }
}
```

#### ControlPointEstimator
```csharp
namespace CurveCompression.Algorithms
{
    public static class ControlPointEstimator
    {
        // Main estimation interface
        public static Dictionary<string, EstimationResult> EstimateAll(TimeValuePair[], float, int, int)
        
        // Individual estimation methods
        public static EstimationResult EstimateByElbowMethod(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByCurvature(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByInformationEntropy(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByDouglasPeuckerAdaptive(TimeValuePair[], float, int, int)
        public static EstimationResult EstimateByTotalVariation(TimeValuePair[], float, int, int)
        public static EstimationResult DetermineByErrorBound(TimeValuePair[], float)
        public static EstimationResult DetermineByStatistical(TimeValuePair[], float)
        
        // Supporting classes
        public class EstimationResult { /* optimalPoints, score, method, metrics */ }
    }
}
```

## Data Flow Architecture

### Compression Process Flow

```
1. Input Validation
   ├── ValidationUtils.ValidatePoints()
   ├── ValidationUtils.ValidateTolerance()
   └── ValidationUtils.ValidateCompressionParams()

2. Intelligent Analysis (if auto-selection enabled)
   ├── AlgorithmSelector.AnalyzeDataCharacteristics()
   ├── AlgorithmSelector.SelectBestAlgorithm()
   └── AdaptiveTolerance.CalculateAdaptiveTolerance()

3. Algorithm Routing
   ├── HybridCompressor.Compress()
   └── [Specific Algorithm].Compress()

4. Result Assembly
   ├── CompressionResult construction
   ├── Error metrics calculation
   └── Performance metrics collection
```

### Error Handling Flow

```
1. Input Validation Errors
   ├── ArgumentNullException (null inputs)
   ├── ArgumentException (invalid data)
   └── ArgumentOutOfRangeException (invalid ranges)

2. Algorithm Execution Errors
   ├── Graceful degradation to simpler algorithms
   ├── Fallback tolerance adjustment
   └── Emergency linear approximation

3. Result Validation
   ├── Output quality verification
   ├── Metric consistency checking
   └── Performance bounds validation
```

## Extension Points

### Adding New Algorithms

1. **Create Algorithm Class**:
   ```csharp
   namespace CurveCompression.Algorithms
   {
       public static class NewAlgorithm
       {
           public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
           public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
       }
   }
   ```

2. **Update Enumerations**:
   ```csharp
   public enum CompressionMethod
   {
       // ... existing methods
       NewAlgorithm_Variant
   }
   ```

3. **Update HybridCompressor**:
   ```csharp
   CompressionMethod.NewAlgorithm_Variant => NewAlgorithm.Compress(points, parameters)
   ```

4. **Update AlgorithmSelector**:
   ```csharp
   scores[CompressionMethod.NewAlgorithm_Variant] = CalculateNewAlgorithmScore(analysis);
   ```

### Adding New Quality Metrics

1. **Extend CompressionResult**:
   ```csharp
   public class CompressionResult
   {
       // ... existing properties
       public float newMetric;
   }
   ```

2. **Implement Calculation**:
   ```csharp
   private void CalculateNewMetric(TimeValuePair[] original, CompressedCurveData compressed)
   {
       newMetric = /* calculation logic */;
   }
   ```

### Adding New Data Analysis

1. **Extend DataAnalysis Structure**:
   ```csharp
   public struct DataAnalysis
   {
       // ... existing fields
       public float newCharacteristic;
   }
   ```

2. **Implement Analysis Logic**:
   ```csharp
   private static float CalculateNewCharacteristic(TimeValuePair[] data)
   {
       return /* analysis logic */;
   }
   ```

This organization provides clear separation of concerns, maintainable code structure, and excellent extensibility for future enhancements.
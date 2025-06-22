# Current Codebase Architecture

## Overview

The CurveCompression library has been systematically refactored to provide a robust, extensible, and maintainable curve compression system for Unity. This document describes the current architecture after comprehensive refactoring.

## Architecture Principles

### 1. Layered Architecture
```
┌─────────────────────────────────────────┐
│           Unity Integration              │  ← UnityCompressionUtils
├─────────────────────────────────────────┤
│              High-Level API              │  ← CurveCompressor
├─────────────────────────────────────────┤
│           Intelligent Systems            │  ← AlgorithmSelector, AdaptiveTolerance
├─────────────────────────────────────────┤
│          Algorithm Implementations       │  ← RDP, BSpline, Bezier, ControlPointEstimator
├─────────────────────────────────────────┤
│            Core Utilities                │  ← MathUtils, ValidationUtils, InterpolationUtils
├─────────────────────────────────────────┤
│            Data Structures               │  ← TimeValuePair, CompressionParams, etc.
└─────────────────────────────────────────┘
```

### 2. Design Patterns

#### Factory Pattern
- `CurveSegment.CreateLinear()`, `CreateBezier()`, `CreateBSpline()`
- Standardized creation of different curve types

#### Strategy Pattern
- `CompressionMethod` enumeration drives algorithm selection
- `HybridCompressor` routes to appropriate algorithms

#### Template Method Pattern
- Standard compression interface across all algorithms
- Consistent validation and error handling patterns

#### Facade Pattern
- `CurveCompressor` provides simplified access to complex subsystems
- Single entry point for all compression operations

## Module Organization

### Core Namespace (`CurveCompression.Core`)

#### Primary Components
- **CurveCompressor**: Main API facade
- **HybridCompressor**: Algorithm routing and selection
- **AlgorithmSelector**: Intelligent algorithm recommendation
- **AdaptiveTolerance**: Data-driven tolerance calculation
- **ValidationUtils**: Input validation and safety
- **MathUtils**: Safe mathematical operations
- **InterpolationUtils**: Optimized interpolation algorithms

#### Key Responsibilities
- Unified API surface
- Input validation and safety
- Algorithm selection and routing
- Performance optimization

### Algorithms Namespace (`CurveCompression.Algorithms`)

#### Algorithm Implementations
- **RDPAlgorithm**: Ramer-Douglas-Peucker line simplification
- **BSplineAlgorithm**: B-Spline curve fitting
- **BezierAlgorithm**: Bezier curve approximation
- **ControlPointEstimator**: Optimal point count estimation

#### Design Characteristics
- Consistent interface patterns
- Standardized error handling
- Performance-optimized implementations
- Comprehensive parameter validation

### Data Structures Namespace (`CurveCompression.DataStructures`)

#### Core Data Types
- **TimeValuePair**: Basic time-value data point
- **CompressionParams**: Configuration and parameters
- **CompressionResult**: Results and metrics
- **CompressedCurveData**: Compressed curve representation
- **CurveSegment**: Individual curve segment

#### Design Features
- Value types for performance-critical data
- Validated properties with automatic checking
- Serialization support for Unity Inspector
- Immutable data where appropriate

## Safety and Robustness Systems

### 1. Input Validation System

#### ValidationUtils Features
```csharp
// Comprehensive point validation
ValidatePoints(points, minRequired: 2)

// Range validation with clear error messages
ValidateRange(value, min, max, paramName)

// Parameter object validation
ValidateCompressionParams(parameters)

// Tolerance validation with safety bounds
ValidateTolerance(tolerance)
```

#### Validation Strategy
- **Early validation**: Check inputs at API boundaries
- **Clear error messages**: Descriptive parameter-specific errors
- **Consistent patterns**: Uniform validation across all methods
- **Performance consideration**: Minimal overhead for valid inputs

### 2. Mathematical Safety System

#### MathUtils Features
```csharp
// Zero-division safe operations
SafeDivide(numerator, denominator, defaultValue)

// Robust slope calculation
SafeSlope(x1, y1, x2, y2)

// Safe interpolation parameter calculation
SafeLerpParameter(value, start, end)
```

#### Safety Strategies
- **Epsilon comparisons**: Avoid floating-point precision issues
- **Default value handling**: Graceful fallbacks for edge cases
- **Range clamping**: Automatic bounds enforcement
- **NaN/Infinity protection**: Detection and handling of invalid values

### 3. Algorithm Robustness

#### Error Handling Patterns
- **Graceful degradation**: Fallback to simpler algorithms when needed
- **Progressive validation**: Check preconditions at each algorithm stage
- **Resource management**: Proper cleanup and memory management
- **Performance monitoring**: Built-in performance metrics

## Intelligent Systems

### 1. Algorithm Selection System

#### AlgorithmSelector Capabilities
```csharp
// Data characteristic analysis
DataAnalysis analysis = AnalyzeDataCharacteristics(data)
// - Smoothness calculation
// - Complexity measurement  
// - Noise level estimation
// - Temporal density analysis

// Intelligent algorithm recommendation
AlgorithmRecommendation recommendation = SelectBestAlgorithm(data, params)
// - Scored algorithm evaluation
// - Confidence ratings
// - Fallback recommendations
// - Detailed reasoning
```

#### Selection Criteria
- **Data smoothness**: Angular change analysis
- **Complexity level**: Second derivative variance
- **Noise characteristics**: High-frequency component detection
- **Performance requirements**: Speed vs quality trade-offs
- **Unity integration**: Animation-specific optimizations

### 2. Adaptive Tolerance System

#### AdaptiveTolerance Features
```csharp
// Quality-based tolerance calculation
AdaptiveToleranceResult result = CalculateAdaptiveTolerance(data, qualityLevel)

// Compression ratio targeting
float tolerance = CalculateToleranceForCompressionRatio(data, targetRatio)

// Quality level compression
CompressionResult result = CompressWithQualityLevel(data, QualityLevel.High)
```

#### Adaptation Strategies
- **Data range analysis**: Relative tolerance scaling
- **Noise level adjustment**: SNR-based precision tuning
- **Feature preservation**: Sharp edge detection and protection
- **Performance optimization**: Balanced speed-quality trade-offs

### 3. Control Point Estimation System

#### ControlPointEstimator Algorithms
1. **Elbow Method**: Second derivative analysis for optimal point detection
2. **Curvature Analysis**: Geometric curvature distribution analysis
3. **Information Entropy**: Data complexity based on information theory
4. **Douglas-Peucker Adaptive**: Iterative tolerance-based estimation
5. **Total Variation**: Signal variation preservation analysis
6. **Error Bound**: Binary search for tolerance satisfaction
7. **Statistical Analysis**: SNR and variance-based estimation

## Performance Optimization

### 1. Memory Management

#### Strategies
- **Struct vs Class**: Optimal type selection for performance
- **Array pooling**: Reuse of temporary arrays
- **In-place operations**: Minimize memory allocations
- **Reference counting**: Avoid unnecessary object creation

#### Memory Layout
```
TimeValuePair:      8 bytes (struct)
CurveSegment:      40 bytes (struct, variable)
CompressedCurveData: Dynamic (ref type)
```

### 2. Computational Optimization

#### Algorithmic Improvements
- **Binary search**: O(log n) time lookup in InterpolationUtils
- **Vectorized operations**: SIMD-friendly calculations where possible
- **Early termination**: Skip unnecessary calculations
- **Adaptive sampling**: Variable resolution based on complexity

#### Performance Characteristics
```
Algorithm           Time Complexity    Space Complexity
RDP                O(n log n)         O(log n)
B-Spline           O(n)               O(k) [k=control points]
Bezier             O(n)               O(1) per segment
Control Estimation O(n log n)         O(n)
```

## Extensibility Framework

### 1. Algorithm Extension Points

#### Adding New Algorithms
1. Implement standard compression interface
2. Add to `CompressionMethod` enumeration
3. Update `HybridCompressor` routing logic
4. Add to `AlgorithmSelector` scoring system

#### Interface Requirements
```csharp
public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
```

### 2. Data Type Extensions

#### Custom Curve Types
- Add to `CurveType` enumeration
- Implement evaluation logic in `CurveSegment`
- Add factory method in `CurveSegment`
- Update algorithm support as needed

#### Custom Data Analysis
- Extend `DataAnalysis` structure
- Add analysis methods to `AlgorithmSelector`
- Update scoring algorithms
- Integrate with tolerance calculation

### 3. Quality Metrics Extensions

#### Adding New Metrics
- Extend `CompressionResult` class
- Implement calculation in constructor
- Add to performance analysis
- Update documentation and examples

## Testing and Quality Assurance

### 1. Validation Coverage

#### Input Validation Tests
- Null parameter handling
- Empty array handling
- Invalid range detection
- Boundary condition testing

#### Algorithm Correctness Tests
- Known input/output pairs
- Regression testing
- Cross-algorithm comparison
- Performance benchmarking

### 2. Error Handling Verification

#### Exception Testing
- Expected exception types
- Error message clarity
- Graceful degradation paths
- Resource cleanup verification

## Integration Points

### 1. Unity Integration

#### Unity-Specific Features
- **AnimationCurve compatibility**: Seamless conversion
- **Inspector serialization**: EditorGUI support
- **Performance profiling**: Unity Profiler integration
- **Asset pipeline**: Build-time optimization

#### Integration Classes
- `UnityCompressionUtils`: Conversion utilities
- Custom PropertyDrawers for Inspector
- EditorWindow tools for analysis
- Build processors for optimization

### 2. External Integration

#### API Surface
- Clean C# interfaces
- .NET Standard compatibility
- Minimal dependency requirements
- Clear documentation

This architecture provides a solid foundation for curve compression with excellent performance, maintainability, and extensibility characteristics.
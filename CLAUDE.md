# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 (6000.0.42f1) project implementing curve compression algorithms for time-series data optimization. The project provides a comprehensive, standardized API for compressing time-series data using multiple advanced algorithms including Ramer-Douglas-Peucker (RDP), B-Spline fitting, Bezier curves, and hybrid approaches.

## Common Development Commands

### Unity Editor Commands
- **Open Project**: Open Unity Hub and select the CurveCompression project
- **Run Demo/Tests**: 
  - Open Unity Editor
  - Attach the `CurveCompressionDemo` script to a GameObject in a scene
  - Configure compression parameters in the inspector
  - Enter Play Mode to run compression tests and visualizations
  - View results in the Console window and Scene view
- **Build Project**: File > Build Settings > Build (or Ctrl+Shift+B)

### Unity Command Line (if needed)
```bash
# Run Unity tests from command line (adjust Unity path as needed)
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode

# Build from command line
Unity -batchmode -quit -projectPath . -buildTarget StandaloneLinux64 -buildLinux64Player ./Build/CurveCompression
```

## Architecture Overview

### Core Algorithm Structure
The project implements multiple compression algorithms through a standardized API:

1. **RDPAlgorithm** - Ramer-Douglas-Peucker line simplification
   - Recursively removes points based on perpendicular distance threshold
   - Supports weighted importance and multiple curve types (Linear, BSpline, Bezier)
   - Good for preserving sharp features and critical points

2. **BSplineAlgorithm** - B-Spline curve fitting
   - Creates smooth B-Spline segments from data points
   - Adaptive segmentation based on error tolerance
   - Excellent for smooth, continuous data

3. **BezierAlgorithm** - Bezier curve fitting
   - Creates Bezier segments with computed tangents
   - Compatible with Unity AnimationCurve system
   - Good balance of smoothness and accuracy

4. **HybridCompressor** - Unified compression orchestrator
   - Routes to appropriate algorithm based on CompressionMethod
   - Supports all compression types: RDP variants, direct B-Spline, direct Bezier
   - Provides data-type specific optimization weights

### Standardized API Design
All compression algorithms follow the `ICompressionAlgorithm` interface pattern:
```csharp
// Standard interface
CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
CompressedCurveData Compress(TimeValuePair[] points, float tolerance)

// High-level usage
CompressionResult CurveCompressor.Compress(TimeValuePair[] points, CompressionParams parameters)
```

### Data Structures
- **TimeValuePair[]** - Input time-series data
- **CompressionParams** - Configuration with method, tolerance, importance weights, data type
- **CompressedCurveData** - Output curve segments (CurveSegment[])
- **CompressionResult** - Includes metrics (compression ratio, errors, timing)
- **CurveSegment** - Individual curve pieces (Linear, BSpline, or Bezier)

### Demo/Testing Architecture
- **CurveCompressionDemo** - Main demo component with comprehensive testing
- **CurveVisualizer** - Real-time visualization of compression results
- **ControlPointEstimator** - Automatic optimal control point estimation
- Test data generation: complex waveforms, sine waves, noise patterns
- Performance metrics: compression ratio, max/avg error, computation time
- AnimationClip export functionality for Unity integration

### Key Data Flow
1. Generate or load time-series data as `TimeValuePair[]`
2. Configure compression via `CompressionParams` (method, tolerance, weights)
3. Call `CurveCompressor.Compress()` for high-level compression with metrics
4. Or call individual algorithms for direct access
5. Result includes both `CompressedCurveData` and performance metrics
6. Visualize with `CurveVisualizer` or export as Unity AnimationClips

## Development Notes

### Unity-Specific Considerations
- Universal Render Pipeline is configured with separate PC and Mobile render assets
- Assembly definition restricts to `CurveCompression` namespace
- Unity integration provided via `UnityCompressionUtils` class
- LineRenderer-based visualization for real-time curve display
- AnimationClip export/import support for seamless Unity workflow

### Core Utility Classes
- **ValidationUtils** - Comprehensive input validation and error checking
- **MathUtils** - Safe mathematical operations with division by zero protection
- **InterpolationUtils** - Optimized interpolation methods (linear, Bezier, B-Spline, Catmull-Rom)
- **UnityCompressionUtils** - Unity-specific conversions (AnimationCurve ↔ compressed data)
- **TangentCalculator** - Smooth tangent computation for Bezier curves

### Compression Methods (CompressionMethod enum)
- **RDP_Linear** - RDP with linear segments
- **RDP_BSpline** - RDP with B-Spline curve evaluation
- **RDP_Bezier** - RDP with Bezier curve evaluation
- **BSpline_Direct** - Direct B-Spline fitting
- **Bezier_Direct** - Direct Bezier fitting

### Algorithm Parameters
Current standardized API uses `CompressionParams`:
```csharp
var parameters = new CompressionParams
{
    tolerance = 0.01f,                    // Compression tolerance
    compressionMethod = CompressionMethod.Bezier_Direct,
    dataType = CompressionDataType.Animation,
    importanceThreshold = 1.0f,           // Importance weighting
    importanceWeights = ImportanceWeights.Default
};
```

### Control Point Estimation
Advanced control point estimation with 7 algorithms:
- **Elbow Method** - Finds optimal point count via error curve analysis
- **Curvature Analysis** - Based on local curvature variation
- **Information Entropy** - Uses data complexity metrics
- **Douglas-Peucker Adaptive** - RDP-based progressive refinement
- **Total Variation** - Minimizes variation while preserving features
- **Error Bound** - Upper bound determination
- **Statistical Analysis** - Data distribution-based estimation

### Current Development Status
- **Production Ready** - Comprehensive API with full error handling
- **Extensively Tested** - Demo framework with multiple test scenarios
- **Optimized Performance** - Safe mathematical operations and validation
- **Clean Architecture** - Standardized interfaces and separation of concerns
- **Unity Integrated** - Full AnimationCurve support and visualization tools

## Usage Examples

### Basic Compression
```csharp
using CurveCompression.Core;
using CurveCompression.DataStructures;

// Simple compression with tolerance
var result = CurveCompressor.Compress(timeValueData, 0.01f);
Debug.Log($"Compressed from {result.originalCount} to {result.compressedCount} points");
Debug.Log($"Max error: {result.maxError:F6}");
```

### Advanced Compression with Parameters
```csharp
var parameters = new CompressionParams
{
    tolerance = 0.005f,
    compressionMethod = CompressionMethod.RDP_Bezier,
    dataType = CompressionDataType.Animation,
    importanceThreshold = 1.5f
};

var result = CurveCompressor.Compress(animationData, parameters);
```

### Unity AnimationCurve Integration
```csharp
using CurveCompression.Core;

// Convert AnimationCurve to compressed data
var compressedData = UnityCompressionUtils.FromAnimationCurve(animCurve, 0.01f);

// Compress existing time-series data and convert back to AnimationCurve
var result = CurveCompressor.Compress(timeValueData, 0.01f);
var newAnimCurve = UnityCompressionUtils.ToAnimationCurve(result.compressedCurve);
```

### Direct Algorithm Access
```csharp
using CurveCompression.Algorithms;

// Use specific algorithms directly
var rdpResult = RDPAlgorithm.Compress(data, compressionParams);
var bezierResult = BezierAlgorithm.Compress(data, tolerance);
var bsplineResult = BSplineAlgorithm.Compress(data, tolerance);
```

### Automatic Control Point Estimation
```csharp
using CurveCompression.Algorithms;

// Estimate optimal control points
var estimates = ControlPointEstimator.EstimateAll(data, tolerance, 2, 50);
int optimalPoints = estimates["Elbow"].optimalPoints;

// Use estimated points for compression
var fixedResult = BezierAlgorithm.CompressWithFixedControlPoints(data, optimalPoints);
```

## Common Development Patterns

### Error Handling
All compression methods use `ValidationUtils` for input validation:
- Automatically validates non-null, non-empty data
- Checks tolerance values are positive
- Validates CompressionParams configuration
- Throws descriptive ArgumentExceptions for invalid inputs

### Performance Considerations
- Use `MathUtils` for safe mathematical operations
- `InterpolationUtils` provides optimized interpolation methods
- Large datasets benefit from RDP preprocessing before curve fitting
- Consider data type (`Animation`, `SensorData`, `FinancialData`) for optimal weights

### Testing and Debugging
- Use `CurveCompressionDemo` for interactive testing
- Enable `CurveVisualizer` for real-time visual feedback
- Monitor compression metrics (ratio, max/avg error) for quality assessment
- Export results as AnimationClips for detailed Unity inspection
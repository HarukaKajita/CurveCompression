# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 (6000.0.42f1) project implementing curve compression algorithms for time-series data optimization. The project focuses on three main compression approaches: Ramer-Douglas-Peucker (RDP), B-Spline fitting, and a hybrid approach.

## Common Development Commands

### Unity Editor Commands
- **Open Project**: Open Unity Hub and select the CurveCompression project
- **Run Tests**: 
  - Open Unity Editor
  - Attach the `CurveCompressorTest` script to a GameObject in a scene
  - Enter Play Mode to run the compression tests
  - View results in the Console window
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
The project implements three compression algorithms that all work with `TimeValuePair[]` data:

1. **RDPAlgorithm** - Classic Ramer-Douglas-Peucker line simplification
   - Recursively removes points based on perpendicular distance threshold
   - Good for preserving sharp features

2. **BSplineAlgorithm** - B-Spline curve fitting
   - Uses `BSplineSegment` for individual curve segments
   - Fits smooth curves through data points
   - Better for smooth data but may miss sharp transitions

3. **HybridCompressor** - Combines both approaches
   - First applies RDP for initial simplification
   - Then uses B-Spline fitting on the simplified data
   - Aims to balance accuracy and smoothness

### Testing Architecture
- Tests are implemented as Unity MonoBehaviours in `CurveCompressorTest.cs`
- Test data generation includes sine waves, square waves, sawtooth waves, and noise
- Performance metrics tracked: compression ratio, maximum error, average error, computation time

### Key Data Flow
1. Generate or load time-series data as `TimeValuePair[]`
2. Pass data to compression algorithm with tolerance parameter
3. Algorithm returns compressed `TimeValuePair[]`
4. Evaluate quality metrics (error, compression ratio)

## Development Notes

### Unity-Specific Considerations
- The project uses Unity's new Input System (check InputSystem_Actions.inputactions for bindings)
- Universal Render Pipeline is configured with separate PC and Mobile render assets
- Assembly definition restricts to `CurveCompression` namespace

### Algorithm Parameters
- `RDPAlgorithm.Simplify(data, tolerance)` - tolerance is perpendicular distance threshold
- `BSplineAlgorithm.Compress(data, maxError)` - maxError is maximum allowed deviation
- `HybridCompressor.Compress(data, tolerance)` - uses same tolerance for both stages

### Current Development Status
- Algorithms are in initial implementation stage (per recent commit)
- Test framework is set up but may need expansion for edge cases
- Modified files indicate active development on all compression algorithms
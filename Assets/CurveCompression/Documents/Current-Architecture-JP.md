# 現在のコードベースアーキテクチャ

## 概要

CurveCompressionライブラリは、Unity向けの堅牢で拡張可能、保守可能な曲線圧縮システムを提供するため、体系的にリファクタリングされました。このドキュメントでは、包括的なリファクタリング後の現在のアーキテクチャについて説明します。

## アーキテクチャ原則

### 1. 階層化アーキテクチャ
```
┌─────────────────────────────────────────┐
│           Unity統合                      │  ← UnityCompressionUtils
├─────────────────────────────────────────┤
│              高レベルAPI                  │  ← CurveCompressor
├─────────────────────────────────────────┤
│           インテリジェントシステム         │  ← AlgorithmSelector, AdaptiveTolerance
├─────────────────────────────────────────┤
│          アルゴリズム実装                 │  ← RDP, BSpline, Bezier, ControlPointEstimator
├─────────────────────────────────────────┤
│            コアユーティリティ             │  ← MathUtils, ValidationUtils, InterpolationUtils
├─────────────────────────────────────────┤
│            データ構造                     │  ← TimeValuePair, CompressionParams, etc.
└─────────────────────────────────────────┘
```

### 2. デザインパターン

#### ファクトリーパターン
- `CurveSegment.CreateLinear()`, `CreateBezier()`, `CreateBSpline()`
- 異なる曲線タイプの標準化された作成

#### ストラテジーパターン
- `CompressionMethod`列挙型がアルゴリズム選択を駆動
- `HybridCompressor`が適切なアルゴリズムにルーティング

#### テンプレートメソッドパターン
- すべてのアルゴリズム間での標準圧縮インターフェース
- 一貫した検証とエラーハンドリングパターン

#### ファサードパターン
- `CurveCompressor`が複雑なサブシステムへの簡単なアクセスを提供
- すべての圧縮操作の単一エントリーポイント

## モジュール構成

### コア名前空間 (`CurveCompression.Core`)

#### 主要コンポーネント
- **CurveCompressor**: メインAPIファサード
- **HybridCompressor**: アルゴリズムルーティングと選択
- **AlgorithmSelector**: インテリジェントアルゴリズム推奨
- **AdaptiveTolerance**: データ駆動許容誤差計算
- **ValidationUtils**: 入力検証と安全性
- **MathUtils**: 安全な数学演算
- **InterpolationUtils**: 最適化された補間アルゴリズム

#### 主要責任
- 統一APIサーフェス
- 入力検証と安全性
- アルゴリズム選択とルーティング
- パフォーマンス最適化

### アルゴリズム名前空間 (`CurveCompression.Algorithms`)

#### アルゴリズム実装
- **RDPAlgorithm**: Ramer-Douglas-Peucker線単純化
- **BSplineAlgorithm**: B-スプライン曲線フィッティング
- **BezierAlgorithm**: ベジェ曲線近似
- **ControlPointEstimator**: 最適ポイント数推定

#### 設計特性
- 一貫したインターフェースパターン
- 標準化されたエラーハンドリング
- パフォーマンス最適化実装
- 包括的パラメータ検証

### データ構造名前空間 (`CurveCompression.DataStructures`)

#### コアデータタイプ
- **TimeValuePair**: 基本時間-値データポイント
- **CompressionParams**: 設定とパラメータ
- **CompressionResult**: 結果とメトリクス
- **CompressedCurveData**: 圧縮曲線表現
- **CurveSegment**: 個別曲線セグメント

#### 設計特徴
- パフォーマンス重要データのための値型
- 自動チェック付き検証プロパティ
- Unity Inspectorシリアライゼーションサポート
- 適切な場所での不変データ

## 安全性と堅牢性システム

### 1. 入力検証システム

#### ValidationUtils機能
```csharp
// 包括的ポイント検証
ValidatePoints(points, minRequired: 2)

// 明確なエラーメッセージ付き範囲検証
ValidateRange(value, min, max, paramName)

// パラメータオブジェクト検証
ValidateCompressionParams(parameters)

// 安全境界付き許容誤差検証
ValidateTolerance(tolerance)
```

#### 検証戦略
- **早期検証**: API境界での入力チェック
- **明確なエラーメッセージ**: パラメータ固有の記述的エラー
- **一貫したパターン**: すべてのメソッド間での統一検証
- **パフォーマンス考慮**: 有効入力での最小オーバーヘッド

### 2. 数学的安全性システム

#### MathUtils機能
```csharp
// ゼロ除算安全操作
SafeDivide(numerator, denominator, defaultValue)

// 堅牢な傾き計算
SafeSlope(x1, y1, x2, y2)

// 安全な補間パラメータ計算
SafeLerpParameter(value, start, end)
```

#### 安全性戦略
- **イプシロン比較**: 浮動小数点精度問題の回避
- **デフォルト値処理**: エッジケースでの優雅なフォールバック
- **範囲クランプ**: 自動境界強制
- **NaN/Infinity保護**: 無効値の検出と処理

### 3. アルゴリズム堅牢性

#### エラーハンドリングパターン
- **優雅な劣化**: 必要時により簡単なアルゴリズムへのフォールバック
- **プログレッシブ検証**: 各アルゴリズム段階での前提条件チェック
- **リソース管理**: 適切なクリーンアップとメモリ管理
- **パフォーマンス監視**: 組み込みパフォーマンスメトリクス

## インテリジェントシステム

### 1. アルゴリズム選択システム

#### AlgorithmSelector機能
```csharp
// データ特性分析
DataAnalysis analysis = AnalyzeDataCharacteristics(data)
// - 滑らかさ計算
// - 複雑度測定
// - ノイズレベル推定
// - 時間密度分析

// インテリジェントアルゴリズム推奨
AlgorithmRecommendation recommendation = SelectBestAlgorithm(data, params)
// - スコア化アルゴリズム評価
// - 信頼度評価
// - フォールバック推奨
// - 詳細推論
```

#### 選択基準
- **データ滑らかさ**: 角度変化分析
- **複雑度レベル**: 二階微分分散
- **ノイズ特性**: 高周波成分検出
- **パフォーマンス要件**: 速度対品質トレードオフ
- **Unity統合**: アニメーション固有最適化

### 2. 適応的許容誤差システム

#### AdaptiveTolerance機能
```csharp
// 品質ベース許容誤差計算
AdaptiveToleranceResult result = CalculateAdaptiveTolerance(data, qualityLevel)

// 圧縮率ターゲティング
float tolerance = CalculateToleranceForCompressionRatio(data, targetRatio)

// 品質レベル圧縮
CompressionResult result = CompressWithQualityLevel(data, QualityLevel.High)
```

#### 適応戦略
- **データ範囲分析**: 相対許容誤差スケーリング
- **ノイズレベル調整**: SNRベース精度調整
- **特徴保持**: 鋭いエッジ検出と保護
- **パフォーマンス最適化**: バランス速度-品質トレードオフ

### 3. 制御点推定システム

#### ControlPointEstimatorアルゴリズム
1. **エルボー法**: 最適点検出のための二階微分分析
2. **曲率解析**: 幾何学的曲率分布分析
3. **情報エントロピー**: 情報理論ベースのデータ複雑度
4. **Douglas-Peucker適応**: 反復許容誤差ベース推定
5. **全変動**: 信号変動保持分析
6. **誤差境界**: 許容誤差満足のための二分探索
7. **統計解析**: SNRと分散ベース推定

## パフォーマンス最適化

### 1. メモリ管理

#### 戦略
- **構造体対クラス**: パフォーマンスのための最適型選択
- **配列プーリング**: 一時配列の再利用
- **インプレース操作**: メモリ割り当ての最小化
- **参照カウント**: 不要なオブジェクト作成の回避

#### メモリレイアウト
```
TimeValuePair:      8 bytes (struct)
CurveSegment:      40 bytes (struct, variable)
CompressedCurveData: Dynamic (ref type)
```

### 2. 計算最適化

#### アルゴリズム改善
- **二分探索**: InterpolationUtilsでのO(log n)時間ルックアップ
- **ベクトル化操作**: 可能な場所でのSIMDフレンドリー計算
- **早期終了**: 不要計算のスキップ
- **適応サンプリング**: 複雑度ベース可変解像度

#### パフォーマンス特性
```
アルゴリズム           時間計算量         空間計算量
RDP                   O(n log n)        O(log n)
B-Spline             O(n)               O(k) [k=制御点数]
Bezier               O(n)               O(1) セグメント毎
制御点推定           O(n log n)         O(n)
```

## 拡張性フレームワーク

### 1. アルゴリズム拡張ポイント

#### 新しいアルゴリズムの追加
1. 標準圧縮インターフェースの実装
2. `CompressionMethod`列挙型への追加
3. `HybridCompressor`ルーティングロジックの更新
4. `AlgorithmSelector`スコアリングシステムへの追加

#### インターフェース要件
```csharp
public static CompressedCurveData Compress(TimeValuePair[] points, CompressionParams parameters)
public static CompressedCurveData Compress(TimeValuePair[] points, float tolerance)
```

### 2. データタイプ拡張

#### カスタム曲線タイプ
- `CurveType`列挙型への追加
- `CurveSegment`での評価ロジック実装
- `CurveSegment`でのファクトリーメソッド追加
- 必要に応じたアルゴリズムサポート更新

#### カスタムデータ分析
- `DataAnalysis`構造体の拡張
- `AlgorithmSelector`への分析メソッド追加
- スコアリングアルゴリズムの更新
- 許容誤差計算との統合

### 3. 品質メトリクス拡張

#### 新しいメトリクスの追加
- `CompressionResult`クラスの拡張
- コンストラクタでの計算実装
- パフォーマンス分析への追加
- ドキュメントと例の更新

## テストと品質保証

### 1. 検証カバレッジ

#### 入力検証テスト
- nullパラメータ処理
- 空配列処理
- 無効範囲検出
- 境界条件テスト

#### アルゴリズム正確性テスト
- 既知の入力/出力ペア
- 回帰テスト
- アルゴリズム間比較
- パフォーマンスベンチマーク

### 2. エラーハンドリング検証

#### 例外テスト
- 期待される例外タイプ
- エラーメッセージ明確性
- 優雅な劣化パス
- リソースクリーンアップ検証

## 統合ポイント

### 1. Unity統合

#### Unity固有機能
- **AnimationCurve互換性**: シームレス変換
- **Inspectorシリアライゼーション**: EditorGUIサポート
- **パフォーマンスプロファイリング**: Unity Profiler統合
- **アセットパイプライン**: ビルド時最適化

#### 統合クラス
- `UnityCompressionUtils`: 変換ユーティリティ
- Inspector用カスタムPropertyDrawer
- 分析用EditorWindowツール
- 最適化用ビルドプロセッサ

### 2. 外部統合

#### APIサーフェス
- クリーンなC#インターフェース
- .NET Standard互換性
- 最小限の依存要件
- 明確なドキュメント

このアーキテクチャは、優れたパフォーマンス、保守性、拡張性特性を持つ曲線圧縮の堅実な基盤を提供します。
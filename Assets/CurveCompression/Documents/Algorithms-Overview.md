# アルゴリズム概要

## 概要

CurveCompressionライブラリは、4つの主要な圧縮アルゴリズムと7つの制御点推定アルゴリズムを実装しています。各アルゴリズムの特徴、適用場面、性能特性について説明します。

## 圧縮アルゴリズム分類

### 1. 点削減型アルゴリズム
**原理**: 重要でない点を除去してデータ量を削減

#### Ramer-Douglas-Peucker (RDP) アルゴリズム
- **手法**: 再帰的な点削減による線単純化
- **特徴**: 鋭い特徴の保持、適応的削減
- **計算量**: O(n log n) ～ O(n²)

### 2. 曲線近似型アルゴリズム
**原理**: 元データを滑らかな曲線で近似

#### B-スプライン近似アルゴリズム
- **手法**: B-スプライン基底関数による曲線フィッティング
- **特徴**: 滑らかな連続性、局所制御
- **計算量**: O(n)

#### ベジェ曲線近似アルゴリズム
- **手法**: ベジェ曲線による区間近似
- **特徴**: Unity AnimationCurve互換、直感的制御
- **計算量**: O(n)

### 3. ハイブリッド型アルゴリズム
**原理**: 複数手法の組み合わせによる最適化

#### ハイブリッド圧縮器
- **手法**: アルゴリズム選択とルーティング
- **特徴**: データタイプ別最適化、統一インターフェース
- **利点**: 用途に応じた最適手法選択

## 詳細アルゴリズム解説

### RDPアルゴリズム

#### 基本原理
1. **端点固定**: 始点と終点を固定
2. **最大偏差点検索**: 線分からの最大距離点を発見
3. **閾値判定**: 距離が許容値以下なら線分近似
4. **再帰分割**: 許容値超過時は点で分割して再帰

#### 実装の特徴
```csharp
// 重要度加重された距離計算
float distance = PerpendicularDistance(points[i], points[start], points[end]);
float importance = CalculateImportance(points, i, weights);
distance *= (1.0f + importance * importanceThreshold);
```

#### 重要度計算
- **曲率重み**: 局所的な曲率変化
- **速度重み**: 時間的変化率
- **加速度重み**: 二階微分値
- **時間重み**: 時間間隔の影響

#### 曲線タイプ対応
- **Linear**: 線形セグメント生成
- **BSpline**: B-スプライン評価後にセグメント化
- **Bezier**: ベジェ評価後にセグメント化

### B-スプライン近似アルゴリズム

#### 基本原理
1. **適応的セグメンテーション**: データ複雑度に応じた分割
2. **コントロールポイント配置**: 最適な制御点配置計算
3. **スプライン構築**: B-スプライン基底での曲線構築
4. **誤差評価**: 近似誤差の計算と閾値判定

#### 制御点最適化
```csharp
// 均等配置による初期化
var controlIndices = new int[numControlPoints];
for (int i = 0; i < numControlPoints; i++)
{
    float t = (float)i / (numControlPoints - 1);
    controlIndices[i] = Mathf.RoundToInt(t * (points.Length - 1));
}

// 最小二乗法による最適化
OptimizeControlPoints(originalPoints, controlPoints);
```

#### 誤差制御
- **適応的分割**: 誤差超過時の自動分割
- **品質保証**: 最大誤差の保証
- **効率最適化**: 最小制御点数での近似

### ベジェ曲線近似アルゴリズム

#### 基本原理
1. **セグメント分割**: データを適切な区間に分割
2. **タンジェント計算**: 各区間の接線ベクトル算出
3. **ベジェ構築**: 3次ベジェ曲線での近似
4. **品質評価**: 近似品質の評価と調整

#### タンジェント計算手法
```csharp
// 滑らかなタンジェント計算
private static float CalculateInTangent(TimeValuePair[] points, int start, int end)
{
    // 複数点を使った重み付き傾き推定
    float totalWeight = 0f;
    float weightedSlope = 0f;
    
    for (int i = 0; i < sampleCount; i++)
    {
        float weight = 1.0f / (i + 1); // 距離重み
        float slope = (points[idx2].value - points[idx1].value) / 
                     (points[idx2].time - points[idx1].time);
        weightedSlope += slope * weight;
        totalWeight += weight;
    }
    
    return totalWeight > 0 ? weightedSlope / totalWeight : 0f;
}
```

#### Unity統合機能
- **AnimationCurve互換**: キーフレームとタンジェント対応
- **エディター統合**: Inspector表示とハンドル編集
- **実行時効率**: 最適化された評価関数

### ハイブリッド圧縮器

#### アルゴリズム選択ロジック
```csharp
return compressionMethod switch
{
    CompressionMethod.RDP_Linear => RDPAlgorithm.CompressWithCurveEvaluation(
        points, tolerance, CurveType.Linear, importanceThreshold, weights),
        
    CompressionMethod.RDP_BSpline => RDPAlgorithm.CompressWithCurveEvaluation(
        points, tolerance, CurveType.BSpline, importanceThreshold, weights),
        
    CompressionMethod.BSpline_Direct => BSplineAlgorithm.Compress(
        points, tolerance),
        
    CompressionMethod.Bezier_Direct => BezierAlgorithm.Compress(
        points, tolerance),
        
    _ => BezierAlgorithm.Compress(points, tolerance) // デフォルト
};
```

#### データタイプ別最適化
```csharp
public static ImportanceWeights GetOptimalWeights(CompressionDataType dataType, ImportanceWeights userWeights)
{
    return dataType switch
    {
        CompressionDataType.Animation => ImportanceWeights.ForAnimation,      // 滑らかさ重視
        CompressionDataType.SensorData => ImportanceWeights.ForSensorData,   // 精度重視
        CompressionDataType.FinancialData => ImportanceWeights.ForFinancialData, // 極値保持
        _ => userWeights ?? ImportanceWeights.Default
    };
}
```

## 制御点推定アルゴリズム

### 1. エルボー法 (Elbow Method)
**原理**: 誤差曲線の「肘」点検出
```csharp
// 曲率ベースの肘点検出
float curvature = CalculateCurvature(errors, i);
if (curvature > maxCurvature)
{
    maxCurvature = curvature;
    elbowPoint = i;
}
```

### 2. 曲率解析 (Curvature Analysis)
**原理**: データの局所曲率に基づく点数決定
- 高曲率領域: より多くの制御点
- 低曲率領域: 少ない制御点

### 3. 情報エントロピー (Information Entropy)
**原理**: データの情報量に基づく複雑度測定
```csharp
float entropy = 0f;
for (int i = 0; i < bins.Length; i++)
{
    if (bins[i] > 0)
    {
        float p = bins[i] / totalSamples;
        entropy -= p * Mathf.Log(p);
    }
}
```

### 4. Douglas-Peucker適応手法
**原理**: RDPアルゴリズムの段階的適用
- 粗い近似から開始
- 段階的に精度向上
- 許容誤差達成で停止

### 5. 全変動最小化 (Total Variation)
**原理**: 信号の全変動を最小化
```csharp
float totalVariation = 0f;
for (int i = 1; i < points.Length; i++)
{
    totalVariation += Mathf.Abs(points[i].value - points[i-1].value);
}
```

### 6. 誤差境界決定 (Error Bound)
**原理**: 統計的誤差境界による上限決定
- 信頼区間計算
- 外れ値検出
- 保守的推定

### 7. 統計解析 (Statistical Analysis)
**原理**: データ分布特性による推定
- 分散分析
- 正規性検定
- 適応的サンプリング

## 性能比較

### 計算量比較
| アルゴリズム | 時間計算量 | 空間計算量 | 備考 |
|-------------|-----------|-----------|------|
| RDP | O(n log n) | O(log n) | 平均ケース |
| B-スプライン | O(n) | O(k) | k: 制御点数 |
| ベジェ | O(n) | O(1) | セグメント毎 |
| 制御点推定 | O(n log n) | O(n) | アルゴリズム依存 |

### 品質vs効率トレードオフ
```
品質  ↑
     │   ベジェ
     │     ○
     │  B-スプライン
     │     ○
     │       RDP(曲線)
     │         ○
     │           RDP(線形)
     │             ○
     └────────────────→ 効率
```

### 適用場面ガイド

#### アニメーションデータ
- **推奨**: ベジェ（Unity互換性）
- **代替**: B-スプライン（滑らかさ重視）

#### センサーデータ
- **推奨**: RDP（ノイズ除去）
- **代替**: B-スプライン（トレンド抽出）

#### 金融データ
- **推奨**: RDP（極値保持）
- **代替**: ベジェ（トレンド可視化）

#### リアルタイム処理
- **推奨**: RDP線形（高速）
- **代替**: 固定制御点手法

## 品質評価指標

### 定量的指標
- **圧縮率**: `compressed_size / original_size`
- **最大誤差**: `max(|original[i] - approximated[i]|)`
- **平均誤差**: `mean(|original[i] - approximated[i]|)`
- **RMSE**: `sqrt(mean((original[i] - approximated[i])²))`

### 定性的指標
- **視覚的品質**: 人間の知覚による評価
- **特徴保持**: 重要な特徴の維持度
- **滑らかさ**: 曲線の連続性

この包括的なアルゴリズム概要により、用途に応じた最適な圧縮手法を選択できます。
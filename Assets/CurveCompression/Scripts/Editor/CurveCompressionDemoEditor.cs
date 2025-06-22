using UnityEngine;
using UnityEditor;
using CurveCompression.Test;

namespace CurveCompression.Editor
{
    /// <summary>
    /// CurveCompressionDemoのカスタムエディター
    /// </summary>
    [CustomEditor(typeof(CurveCompressionDemo))]
    public class CurveCompressionDemoEditor : UnityEditor.Editor
    {
        // スタイル
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
            
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(15, 15, 5, 5),
                    fontSize = 12
                };
            }
        }
        
        public override void OnInspectorGUI()
        {
            InitializeStyles();
            
            CurveCompressionDemo demo = (CurveCompressionDemo)target;
            
            // デフォルトインスペクター描画
            DrawDefaultInspector();
            
            EditorGUILayout.Space(20);
            
            // 実行ボタンセクション
            EditorGUILayout.LabelField("実行コントロール", headerStyle);
            
            // 状態表示
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("現在の状態", EditorStyles.boldLabel);
            
            if (HasTestData(demo))
            {
                EditorGUILayout.LabelField($"テストデータ: {GetTestDataCount(demo)}ポイント");
                
                if (HasCompressionResult(demo))
                {
                    var result = GetCompressionResult(demo);
                    EditorGUILayout.LabelField($"圧縮後: {result.compressedCount}ポイント");
                    EditorGUILayout.LabelField($"圧縮率: {result.compressionRatio:F3}");
                    EditorGUILayout.LabelField($"最大誤差: {result.maxError:F6}");
                    
                    if (result.compressionTime > 0)
                    {
                        EditorGUILayout.LabelField($"圧縮時間: {result.compressionTime:F2} ms");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("圧縮結果: なし");
                }
            }
            else
            {
                EditorGUILayout.LabelField("テストデータ: なし");
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // アクションボタン
            EditorGUILayout.BeginVertical();
            
            // テストデータ生成ボタン
            if (GUILayout.Button("テストデータを生成", buttonStyle, GUILayout.Height(30)))
            {
                demo.RegenerateTestData();
                EditorUtility.SetDirty(demo);
            }
            
            EditorGUILayout.Space(5);
            
            // 圧縮実行ボタン
            EditorGUI.BeginDisabledGroup(!HasTestData(demo));
            if (GUILayout.Button("圧縮を実行", buttonStyle, GUILayout.Height(30)))
            {
                demo.RerunCompression();
                EditorUtility.SetDirty(demo);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // 全手法比較ボタン
            EditorGUI.BeginDisabledGroup(!HasTestData(demo));
            if (GUILayout.Button("全手法を比較", buttonStyle, GUILayout.Height(25)))
            {
                CompareAllMethods(demo);
                EditorUtility.SetDirty(demo);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // コントロールポイント推定ボタン
            EditorGUI.BeginDisabledGroup(!HasTestData(demo));
            if (GUILayout.Button("コントロールポイント推定を実行（時間計測付き）", buttonStyle, GUILayout.Height(25)))
            {
                demo.RunControlPointEstimationWithTiming();
                EditorUtility.SetDirty(demo);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // AnimationClip保存ボタン
            EditorGUI.BeginDisabledGroup(!HasTestData(demo) || !HasCompressionResult(demo));
            if (GUILayout.Button("AnimationClipとして保存", buttonStyle, GUILayout.Height(25)))
            {
                demo.SaveCurrentDataAsAnimationClips();
                EditorUtility.SetDirty(demo);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // 推定結果レポートの表示
            if (HasEstimationResults(demo))
            {
                EditorGUILayout.Space(20);
                DrawEstimationReport(demo);
            }
            
            // 再描画
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }
        
        // プライベートフィールドへのアクセス用リフレクション
        private bool HasTestData(CurveCompressionDemo demo)
        {
            var field = typeof(CurveCompressionDemo).GetField("currentTestData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var data = field?.GetValue(demo) as CurveCompression.DataStructures.TimeValuePair[];
            return data != null && data.Length > 0;
        }
        
        private int GetTestDataCount(CurveCompressionDemo demo)
        {
            var field = typeof(CurveCompressionDemo).GetField("currentTestData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var data = field?.GetValue(demo) as CurveCompression.DataStructures.TimeValuePair[];
            return data?.Length ?? 0;
        }
        
        private bool HasCompressionResult(CurveCompressionDemo demo)
        {
            var field = typeof(CurveCompressionDemo).GetField("currentResult", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(demo) != null;
        }
        
        private CurveCompression.DataStructures.CompressionResult GetCompressionResult(CurveCompressionDemo demo)
        {
            var field = typeof(CurveCompressionDemo).GetField("currentResult", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(demo) as CurveCompression.DataStructures.CompressionResult;
        }
        
        private void CompareAllMethods(CurveCompressionDemo demo)
        {
            // compareAllMethodsフィールドを一時的にtrueに設定
            var field = typeof(CurveCompressionDemo).GetField("compareAllMethods", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var originalValue = (bool)field.GetValue(demo);
            
            field.SetValue(demo, true);
            demo.RerunCompression();
            field.SetValue(demo, originalValue);
        }
        
        private bool HasEstimationResults(CurveCompressionDemo demo)
        {
            var field = typeof(CurveCompressionDemo).GetField("estimationResults", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var results = field?.GetValue(demo) as System.Collections.Generic.List<CurveCompressionDemo.EstimationDisplay>;
            return results != null && results.Count > 0;
        }
        
        private void DrawEstimationReport(CurveCompressionDemo demo)
        {
            EditorGUILayout.LabelField("コントロールポイント推定結果", headerStyle);
            
            var field = typeof(CurveCompressionDemo).GetField("estimationResults", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var results = field?.GetValue(demo) as System.Collections.Generic.List<CurveCompressionDemo.EstimationDisplay>;
            
            if (results == null || results.Count == 0)
                return;
            
            // ヘッダー行のスタイル
            var headerRowStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            
            var cellStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            // テーブル描画
            EditorGUILayout.BeginVertical("Box");
            
            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("アルゴリズム", headerRowStyle, GUILayout.Width(120));
            GUILayout.Label("ポイント数", headerRowStyle, GUILayout.Width(80));
            GUILayout.Label("スコア", headerRowStyle, GUILayout.Width(60));
            
            // 時間計測が有効な場合のみ時間列を表示
            bool hasTimingData = results.Exists(r => r.estimationTime > 0);
            if (hasTimingData)
            {
                GUILayout.Label("実行時間", headerRowStyle, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();
            
            // セパレーター
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            float totalTime = 0f;
            float minTime = float.MaxValue;
            float maxTime = 0f;
            string fastestMethod = "";
            string slowestMethod = "";
            
            // データ行
            foreach (var result in results)
            {
                EditorGUILayout.BeginHorizontal();
                
                // アルゴリズム名
                GUILayout.Label(result.methodName, cellStyle, GUILayout.Width(120));
                
                // ポイント数
                GUILayout.Label(result.optimalPoints.ToString(), cellStyle, GUILayout.Width(80));
                
                // スコア
                GUILayout.Label(result.score.ToString("F3"), cellStyle, GUILayout.Width(60));
                
                // 実行時間
                if (hasTimingData)
                {
                    string timeText = result.estimationTime > 0 ? $"{result.estimationTime:F2} ms" : "-";
                    
                    // 最速/最遅の判定
                    if (result.estimationTime > 0)
                    {
                        totalTime += result.estimationTime;
                        if (result.estimationTime < minTime)
                        {
                            minTime = result.estimationTime;
                            fastestMethod = result.methodName;
                        }
                        if (result.estimationTime > maxTime)
                        {
                            maxTime = result.estimationTime;
                            slowestMethod = result.methodName;
                        }
                    }
                    
                    // 最速は緑、最遅は赤でハイライト
                    if (result.methodName == fastestMethod && result.estimationTime > 0)
                    {
                        GUI.color = Color.green;
                        timeText += " ⚡";
                    }
                    else if (result.methodName == slowestMethod && result.estimationTime > 0)
                    {
                        GUI.color = new Color(1f, 0.5f, 0.5f);
                        timeText += " 🐌";
                    }
                    
                    GUILayout.Label(timeText, cellStyle, GUILayout.Width(80));
                    GUI.color = Color.white;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // 合計時間
            if (hasTimingData && totalTime > 0)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("合計実行時間:", headerRowStyle, GUILayout.Width(260));
                GUILayout.Label($"{totalTime:F2} ms", headerRowStyle, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
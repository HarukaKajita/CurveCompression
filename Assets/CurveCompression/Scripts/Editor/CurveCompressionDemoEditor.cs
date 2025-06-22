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
            if (GUILayout.Button("コントロールポイント推定を実行", buttonStyle, GUILayout.Height(25)))
            {
                demo.RunControlPointEstimationManual();
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
    }
}
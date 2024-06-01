using UnityEditor;
using UnityEngine;

namespace STak4.AssetCICD.Editor
{
    /// <summary>
    /// 汎用進捗表示用ウィンドウ
    /// </summary>
    public class ProgressWindow : EditorWindow
    {
        private float progress;
        private string message;

        public static ProgressWindow ShowWindow(string title)
        {
            return GetWindow<ProgressWindow>(title);
        }
        

        public void SetProgress(float value, string text)
        {
            progress = value;
            message = text;
            Repaint();
        }

        private void OnEnable() {
            minSize = new Vector2(500, 50);
            maxSize = new Vector2(800, 50);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField(message);
            EditorGUI.ProgressBar(new Rect(3, 30, position.width - 6, 20), progress, progress * 100 + "%");
        }
    }
}

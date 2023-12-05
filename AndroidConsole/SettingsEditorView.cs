#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AndroidConsoleSettings))]
[CanEditMultipleObjects]
public class SettingsEditorView : Editor {
    private GUIStyle _notificationTextStyle;

    private static AndroidConsole _androidConsoleWindow;
    public static bool IsRestartRequired;

    private void OnEnable() 
    {
        _notificationTextStyle = new GUIStyle
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        _notificationTextStyle.normal.textColor = new Color32(238, 68, 68, 255);
    }
    public static void TryShowRestartLabel()
    {
        _androidConsoleWindow = AndroidConsole.FindFirstInstance();
        if(_androidConsoleWindow is not null)
        {
            IsRestartRequired = true;
        }
    }
    public override void OnInspectorGUI() 
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck()) 
        {
            TryShowRestartLabel();
        }
        
        if(IsRestartRequired)
        {
            GUILayout.Space(20);
            GUILayout.Label("You will need to restart the Android Console window\nfor the settings to take effect!", _notificationTextStyle);
            if (GUILayout.Button("Restart"))
            {
                _androidConsoleWindow?.RestartWindow();
                IsRestartRequired = false;
            }
        }
    }
}
#endif
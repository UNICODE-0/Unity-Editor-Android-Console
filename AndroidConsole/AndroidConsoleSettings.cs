using UnityEngine;

[CreateAssetMenu(fileName = "AndroidConsoleSettings", menuName = "SO/AndroidConsoleSettings", order = 0)]
public class AndroidConsoleSettings : ScriptableObject 
{
    [Header("Color Scheme")]
    public Color infoColor = new Color32(196, 196, 196, 255);
    public Color warningColor = new Color32(231, 175, 86, 255);
    public Color errorColor = new Color32(238, 68, 68, 255);
    public Color adbOutputColor = new Color32(153, 153, 255, 255);
    public Color disconnectionLabelColor = new Color32(238, 68, 68, 255);
    public Color connectionLabelColor = new Color32(130, 214, 130, 255);
    [Header("Message Appearance")]
    [Range(10,20)]
    public int fontSize = 12;
    [Header("Console Settings")]
    public int maxMessagesCount = 250;
    [Tooltip("The number of messages that will be deleted when the maximum number of messages has been reached")]
    public int messagesBatchToDelete = 100;
    [Tooltip("By default, the Android console disconnects from the device after compiling the scripts")]
    public bool reconnectOnRecompile = true;

    public void Reset()
    {
        SettingsEditorView.TryShowRestartLabel();
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "AndroidConsoleSettings", menuName = "SO/AndroidConsoleSettings", order = 0)]
public class AndroidConsoleSettings : ScriptableObject 
{
    public Color DisconnectionLabelColor = Color.red;
    public Color ConnectionLabelColor = Color.green;
    [Space]
    public Color infoColor = Color.gray;
    public Color wariningColor = Color.yellow;
    public Color errorColor = Color.red;
    [Space]
    public int maxMessagesCount = 250;
    public int messagesBatchToDelete = 100;
    [Space]
    public bool reconnectOnRecompile = true;
}

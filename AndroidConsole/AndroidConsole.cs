#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEditor.Compilation;
using System.Threading.Tasks;
using System.IO;

public class AndroidConsole : EditorWindow 
{
    private const string ADB_PROCESS_KEY = "ADB_process";
    private const int END_PROCESS_WAIT_TIME = 2000; // ms

    private Vector2 _scrollPosition;
    private Process _process;
    private bool _isConnected;
    private bool _isSnaped;
    private bool _useEmulator;
    private bool _guiStylesCreated;
    private GUIStyle _connectedGUIStyle;
    private GUIStyle _disconnectedGUIStyle;

    private GUIStyle _infoGUIStyle;
    private GUIStyle _warningGUIStyle;
    private GUIStyle _errorGUIStyle;

    private AndroidConsoleSettings _settings;

    private List<AndroidMessage> _androidMessages = new List<AndroidMessage>();

    [MenuItem("Window/AndroidConsole")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AndroidConsole));
    }
    private void CreateGUIStyles()
    {
        _connectedGUIStyle = new GUIStyle
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        _connectedGUIStyle.normal.textColor = _settings.ConnectionLabelColor;

        _disconnectedGUIStyle = new GUIStyle
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        _disconnectedGUIStyle.normal.textColor = _settings.DisconnectionLabelColor;
        
        _infoGUIStyle = new GUIStyle("textarea");
        _infoGUIStyle.normal.textColor = _settings.infoColor;

        _warningGUIStyle = new GUIStyle("textarea");
        _warningGUIStyle.normal.textColor = _settings.warningColor;

        _errorGUIStyle = new GUIStyle("textarea");
        _errorGUIStyle.normal.textColor = _settings.errorColor;

        _guiStylesCreated = true;
    }
    private void OnEnable()
    {
        _settings = Resources.Load("AndroidConsoleSettings") as AndroidConsoleSettings;

        EditorApplication.quitting += OnEditorQuit;
        AssemblyReloadEvents.afterAssemblyReload += OnCompilationFinished;
    }

    private void OnDisable() 
    {
        AssemblyReloadEvents.afterAssemblyReload -= OnCompilationFinished;
    }

    private void OnCompilationFinished()
    {
        if(EditorPrefs.HasKey(ADB_PROCESS_KEY))
        {
            _process = Process.GetProcessById(EditorPrefs.GetInt(ADB_PROCESS_KEY));
            Disconnect();
            EditorPrefs.DeleteKey(ADB_PROCESS_KEY);
            if(_settings.reconnectOnRecompile) Connect();
        }
    }
    private void OnGUI() 
    {
        if(!_guiStylesCreated) CreateGUIStyles();

        GUILayout.BeginHorizontal();

        if(_isConnected)
        {
            GUILayout.Label("Connected", _connectedGUIStyle);
        } else
        {
            GUILayout.Label("Disconnected", _disconnectedGUIStyle);
        } 

        EditorGUI.BeginDisabledGroup(_isConnected);
        {
            if (GUILayout.Button("Kill server", GUILayout.Width(85), GUILayout.Height(20)))
            {
                if(EditorUtility.DisplayDialog("Restart server", "Are you sure you want to kill the adb server?", "Yes", "No"))
                {
                    RestartServer();
                }
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.EndHorizontal();

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        for (int i = 0; i < _androidMessages.Count; i++)
        {
            GUILayout.TextArea(_androidMessages[i].text, _androidMessages[i].style);
        }
        GUILayout.EndScrollView();

        if (Event.current.type == EventType.Layout)
        {
           if(_isSnaped) _scrollPosition.y = float.MaxValue;
        }

        GUILayout.BeginHorizontal();

        _isSnaped = GUILayout.Toggle(_isSnaped, "Snap to end");
        _useEmulator = GUILayout.Toggle(_useEmulator, "Use emulator");

        if (GUILayout.Button("Clear"))
        {
            _androidMessages.Clear();
        }

        EditorGUI.BeginDisabledGroup(_process is not null);
        {
            if (GUILayout.Button("Connect"))
            {
                Connect();
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(_process is null);
        {
            if (GUILayout.Button("Disconnect"))
            {
                Disconnect();
            }
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
    }
    private void RestartServer()
    {
        try
        {
            Process process = new Process();
                
            string adbPath = GetAdbPath();
            if(adbPath == string.Empty) return;
            
            process.StartInfo.FileName = adbPath;
            process.StartInfo.Arguments = "kill-server";
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            _androidMessages.Add(new AndroidMessage("Adb server successfully killed", _infoGUIStyle));
        } catch(Exception ex)
        {
            _androidMessages.Add(new AndroidMessage(ex.Message, _infoGUIStyle));
        }
    }
    private string GetAdbPath()
    {
        string adbPath = $@"{EditorApplication.applicationContentsPath}/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe";
        if(!File.Exists(adbPath))
        {
            _androidMessages.Add(new AndroidMessage("The android SDK is required for the android console to work", _errorGUIStyle));
            return string.Empty;
        }

        return adbPath;
    }
    private void Connect()
    {
        try
        {
            string adbPath = GetAdbPath();
            if(adbPath == string.Empty) return;

            _process = new Process();
            
            _process.StartInfo.FileName = adbPath;
            _process.StartInfo.Arguments = (_useEmulator ? "-e" : "-d") + " logcat -s Unity";

            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;

            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnOutputDataReceived;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _isConnected = true;
            _ = WaitForProcessExit();

            EditorPrefs.SetInt(ADB_PROCESS_KEY, _process.Id);

        } catch (Exception ex)
        {
            _androidMessages.Add(new AndroidMessage(ex.Message, _errorGUIStyle));
        }
    }
    private async Task WaitForProcessExit()
    {
        if(_process == null) return;  

        while(!_process.HasExited)
        {
            await Task.Delay(END_PROCESS_WAIT_TIME);
            if(_process == null) return;
        }
        
        _process = null;
        _isConnected = false;
        if(EditorPrefs.HasKey(ADB_PROCESS_KEY)) EditorPrefs.DeleteKey(ADB_PROCESS_KEY);
        Repaint();
    }
    private void Disconnect()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnOutputDataReceived;

            _process.Kill();
            _process.Dispose();
            _process = null;
            _isConnected = false;
        }
    
        if(EditorPrefs.HasKey(ADB_PROCESS_KEY)) EditorPrefs.DeleteKey(ADB_PROCESS_KEY);
    }
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            EditorApplication.delayCall += () =>
            {
                if(_androidMessages.Count >= _settings.maxMessagesCount)
                {
                    int deleteCount = _settings.messagesBatchToDelete;
                    if(deleteCount > _androidMessages.Count) deleteCount = _androidMessages.Count;

                    _androidMessages.RemoveRange(0, deleteCount);
                } 

                GUIStyle messageStyle = _infoGUIStyle; 

                if(e.Data.Contains("I Unity")) 
                {
                    messageStyle = _infoGUIStyle;
                }
                else if(e.Data.Contains("W Unity")) 
                {
                    messageStyle = _warningGUIStyle;
                }
                else if(e.Data.Contains("E Unity")) 
                {
                    messageStyle = _errorGUIStyle;
                }
                
                string message = e.Data;
                if(message == "error: protocol fault (couldn't read status): connection reset")
                {
                    message += ". TRY TO KILL SERVER (maybe several times)";
                }

                _androidMessages.Add(new AndroidMessage(message, messageStyle));
                Repaint();
            };
        }
    }
    private void OnEditorQuit()
    {
        EditorPrefs.DeleteKey(ADB_PROCESS_KEY);
    }
}

class AndroidMessage
{
    public string text;
    public GUIStyle style;
    public AndroidMessage(string text, GUIStyle style)
    {
        this.text = text;   
        this.style = style;
    }
}

#endif
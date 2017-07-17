using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System;

public class ProtoEditor : EditorWindow
{
    private const string DATABASE_PATH = @"Assets/Resources/Configs/ProtoSettingDatabase.asset";
    private readonly string[] encodingNames = { "utf-8", "gbk", "unicode" };
    private ProtoSetting setting;
    private Vector2 _scrollPos;
    private string[] protoFiles = { };
    private bool[] protoFileFolds = { };
    [SerializeField]
    private bool luaFold = true, csharpFold = true;
    private Vector2 scrollPos;
    private int encodingNameIndex = 0;
    private string csharpCmd, luaCmd;

    [MenuItem("Tools/ProtoBuf Generator")]
    public static void Init()
    {
        ProtoEditor window = GetWindow<ProtoEditor>();
        window.minSize = new Vector2(320, 100);
        window.Show();
    }

    void OnEnable()
    {
        if (setting == null) LoadSetting();
        if (!string.IsNullOrEmpty(setting.ProtoFilesPath)) {
            protoFiles = Directory.GetFiles(setting.ProtoFilesPath, "*.proto");
            protoFileFolds = new bool[protoFiles.Length];
        }
    }

    void LoadSetting()
    {
        setting = (ProtoSetting)AssetDatabase.LoadAssetAtPath(DATABASE_PATH, typeof(ProtoSetting));
        if (!setting) CreateSetting();
    }

    void CreateSetting()
    {
        setting = CreateInstance<ProtoSetting>();
        var configDir = Directory.GetCurrentDirectory() + "/Assets/Resources/Configs/";
        if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
        AssetDatabase.CreateAsset(setting, DATABASE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void OnGUI()
    {
        csharpCmd = "\n --proto_path=" + setting.ProtoFilesPath + "\n";
        csharpCmd += (setting.version == ProtoVersion.Proto2 ? " -output_directory=" : " --csharp_out=" + setting.CsharpOutput);
        foreach (var file in protoFiles) {
            var containsSynx = File.ReadAllText(file).Contains("proto3");
            if (setting.version == ProtoVersion.Proto2 && containsSynx || setting.version == ProtoVersion.Proto3 && !containsSynx)  continue;
            csharpCmd += " " + file + "\n";
        }

        luaCmd = "\n -I=" + setting.ProtoFilesPath.Replace("\\", "/") + "\n";
        luaCmd += " --lua_out=" + setting.LuaOutput.Replace("\\", "/") + "\n";
        luaCmd += " --plugin=protoc-gen-lua=protoc-gen-lua.bat";
        foreach (var file in protoFiles) {
            if (File.ReadAllText(file).Contains("proto3")) continue;
            luaCmd += " " + file.Replace("\\", "/") + "\n";
        }

        if (GUILayout.Button("Generate code", GUILayout.ExpandWidth(true), GUILayout.Height(30))) {
            if (setting.CSharp) {
                var csharpStartInfo = new System.Diagnostics.ProcessStartInfo();
                csharpStartInfo.WorkingDirectory = @"c:\";
                csharpStartInfo.FileName = setting.CSharpGenerator;
                csharpStartInfo.CreateNoWindow = false;
                csharpStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                csharpStartInfo.ErrorDialog = true;
                csharpStartInfo.Arguments = csharpCmd;
                var csharpProcess = System.Diagnostics.Process.Start(csharpStartInfo);
                csharpProcess.WaitForInputIdle();
            }
            if (setting.Lua && setting.version == ProtoVersion.Proto2) {
                var luaStartInfo = new System.Diagnostics.ProcessStartInfo();
                luaStartInfo.CreateNoWindow = false;
                luaStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                luaStartInfo.ErrorDialog = true;
                luaStartInfo.FileName = setting.LuaGenerator;
                var strs = setting.LuaGenerator.Split('\\');
                var genLength = strs[strs.Length - 1].Length;
                luaStartInfo.WorkingDirectory = setting.LuaGenerator.Remove(setting.LuaGenerator.Length - genLength, genLength);
                luaStartInfo.Arguments = luaCmd;
                System.Diagnostics.Process luaProcess = System.Diagnostics.Process.Start(luaStartInfo);
                luaProcess.WaitForInputIdle();
            }
            AssetDatabase.Refresh();
        }

        csharpFold = EditorGUILayout.Foldout(csharpFold, "C# generate option");
        if (csharpFold) {
            var csharpRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (GUILayout.Button("C# generate tool", GUILayout.Width(120))) {
                string path = EditorUtility.OpenFilePanel("Select Tool ProtoGen.exe in ProtoBuf", "", "");
                setting.CSharpGenerator = path.Replace("/", "\\");
                Debug.Log(setting.CSharpGenerator);
                EditorUtility.SetDirty(setting);
            }
            setting.CSharpGenerator = EditorGUILayout.TextField(new GUIContent(""), setting.CSharpGenerator);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (GUILayout.Button("C# output", GUILayout.Width(120))) {
                string path = EditorUtility.OpenFolderPanel("Select C# Output Dir", "", "");

                setting.CsharpOutput = path.Replace("/", "\\");
                EditorUtility.SetDirty(setting);
            }
            setting.CsharpOutput = EditorGUILayout.TextField(new GUIContent(""), setting.CsharpOutput);
            EditorGUILayout.EndHorizontal();
            setting.CSharp = EditorGUILayout.Toggle("Generate", setting.CSharp);
            if (setting.CSharp) {
                EditorGUILayout.PrefixLabel("CMD Preview");
                EditorGUILayout.TextArea(setting.CSharpGenerator + csharpCmd);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.DrawRect(csharpRect, Color.cyan / 3f);
        }

        luaFold = EditorGUILayout.Foldout(luaFold, "Lua generate option");
        if (luaFold) {
            var luaRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Lua generate tool", GUILayout.Width(120))) {
                string path = EditorUtility.OpenFilePanel("Select Lua tool", "", "");
                setting.LuaGenerator = path.Replace("/", "\\");
                EditorUtility.SetDirty(setting);
            }
            setting.LuaGenerator = EditorGUILayout.TextField(new GUIContent(""), setting.LuaGenerator);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Lua output", GUILayout.Width(120))) {
                string path = EditorUtility.OpenFolderPanel("Select lua output dir", "", "");
                setting.LuaOutput = path.Replace("/", "\\");
                EditorUtility.SetDirty(setting);
            }
            setting.LuaOutput = EditorGUILayout.TextField(new GUIContent(""), setting.LuaOutput);
            EditorGUILayout.EndHorizontal();
            setting.Lua = EditorGUILayout.Toggle("Generate", setting.Lua);
            if (setting.Lua) {
                EditorGUILayout.PrefixLabel("CMD Preview");
                EditorGUILayout.TextArea(setting.LuaGenerator + luaCmd);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.DrawRect(luaRect, Color.blue / 3f);
        }

        GUILayout.Space(25f);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Proto Files Dir", GUILayout.Width(120))) {
            string path = EditorUtility.OpenFolderPanel("Select Proto Files Dir", "", "");
            if (!string.IsNullOrEmpty(path)) {
                protoFiles = Directory.GetFiles(path);
                setting.ProtoFilesPath = path.Replace("/", "\\");
            }
            OnEnable();
            EditorUtility.SetDirty(setting);
        }

        setting.ProtoFilesPath = EditorGUILayout.TextField("", setting.ProtoFilesPath);
        EditorGUILayout.EndHorizontal();
        setting.version = (ProtoVersion)EditorGUILayout.EnumPopup("Version", setting.version);
        encodingNameIndex = EditorGUILayout.Popup("Encoding", encodingNameIndex, encodingNames);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.LabelField("Protobuf Files");
        for (int i = 0; i < protoFiles.Length; i++) {
            var fileName = protoFiles[i];
            if (protoFileFolds.Length > i) {
                if (protoFileFolds[i] = EditorGUILayout.Foldout(protoFileFolds[i], (i + 1).ToString() + " : " + fileName)) {
                    if (!File.Exists(fileName)) continue;
                    var text = File.ReadAllText(fileName, Encoding.GetEncoding(encodingNames[encodingNameIndex]));
                    EditorGUILayout.TextArea(text);
                }
            }
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(15f);
    }
}
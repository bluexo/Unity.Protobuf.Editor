using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;

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
        if (GUILayout.Button("Generate code", GUILayout.ExpandWidth(true), GUILayout.Height(30))) {
            if (setting.CSharp) {
                var csharpCmd = "--proto_path=" + setting.ProtoFilesPath;
                foreach (var file in protoFiles) csharpCmd += " " + file;
                csharpCmd += (setting.version == ProtoVersion.Proto2 ? " -output_directory=" : " --csharp_out=" + setting.CsharpOutput);

                var csharpStartInfo = new System.Diagnostics.ProcessStartInfo();
                csharpStartInfo.WorkingDirectory = @"c:\";
                csharpStartInfo.FileName = setting.ProtoGenerator;
                csharpStartInfo.CreateNoWindow = false;
                csharpStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                csharpStartInfo.ErrorDialog = true;
                csharpStartInfo.Arguments = csharpCmd;
                //var csharpProcess = System.Diagnostics.Process.Start(csharpStartInfo);
                var csharpProcess = System.Diagnostics.Process.Start("cmd", setting.ProtoGenerator + " " + csharpCmd);
                csharpProcess.WaitForInputIdle();
            }
            if (setting.Lua && setting.version == ProtoVersion.Proto2) {
                var luaCmd = " -I=" + setting.ProtoFilesPath.Replace("\\", "/");
                luaCmd += " --lua_out=" + setting.LuaOutput.Replace("\\", "/");
                luaCmd += " --plugin=protoc-gen-lua=protoc-gen-lua.bat";
                foreach (var file in protoFiles) luaCmd += " " + file.Replace("\\", "/");
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
                Debug.Log(path);
                setting.ProtoGenerator = path.Replace("/", "\\");
                Debug.Log(setting.ProtoGenerator);
                EditorUtility.SetDirty(setting);
            }
            setting.ProtoGenerator = EditorGUILayout.TextField(new GUIContent(""), setting.ProtoGenerator);
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
            EditorGUILayout.EndVertical();
            EditorGUI.DrawRect(luaRect, Color.blue / 3f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

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
        EditorGUILayout.LabelField("Protobuf File Names");
        for (int i = 0; i < protoFiles.Length; i++) {
            var fileName = protoFiles[i];
            if (protoFileFolds.Length > i) {
                if (protoFileFolds[i] = EditorGUILayout.Foldout(protoFileFolds[i], (i + 1).ToString() + " : " + fileName)) {
                    var text = File.ReadAllText(fileName, Encoding.GetEncoding(encodingNames[encodingNameIndex]));
                    EditorGUILayout.TextArea(text);
                }
            }
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }
}
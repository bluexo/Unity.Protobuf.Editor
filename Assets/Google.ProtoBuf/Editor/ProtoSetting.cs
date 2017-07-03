

using UnityEngine;

public enum ProtoVersion { Proto2, Proto3 }

[CreateAssetMenu(fileName = "ProtoSettingDatabase", menuName = "Configs/CreateProtoSettingDatabase")]
public class ProtoSetting : ScriptableObject
{
    public string ProtoGenerator;
    public string CsharpOutput;
    public string LuaGenerator;
    public string LuaOutput;
    public string ProtoFilesPath;
    public bool Lua;
    public bool CSharp;
    public string encodingName;
    public ProtoVersion version;
}

using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System.Runtime.CompilerServices;
using System.Threading;
/// <summary>
/// Editorフォルダ以下においてください
/// </summary>
public class AsmdefUtility : EditorWindow
{
    readonly string desc = $@"
選択したフォルダ直下にasmdefファイルを作ります。
Editorフォルダ以下にも作ります
Editorフォルダ以下の場合、アセットのルートフォルダに作成したasmdefファイルへの参照を追加します
Assets直下のフォルダを指定しないとおかしくなるかも
参照が足りないときもあるので、その場合は自分で追加する必要があります";
  
    [MenuItem("Window/CreateAsmdef")]
    static void Open()
    {
        GetWindow<AsmdefUtility>();
    }
    private void OnGUI()
    {
        GUILayout.Label(desc);
        if (GUILayout.Button("create"))
        {
            string filepath = EditorUtility.OpenFolderPanel("title", "Assets", "_Project");
            
            if (string.IsNullOrEmpty(filepath))//何も選択されていなければ
            {
                return;
            }
            DirectoryInfo rootInfo = new DirectoryInfo(filepath);
            AsmdefCreator.CreateAll(rootInfo);

            AssetDatabase.Refresh();
        }

        GUILayout.Label("選択したフォルダ以下のasmdefファイルを全部消します。");
        if (GUILayout.Button("Delete"))
        {
            string filepath = EditorUtility.OpenFolderPanel("", "Assets", "");
            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }
            DirectoryInfo info = new DirectoryInfo(filepath);
            DeleteAsmdef.Delete(info);
            AssetDatabase.Refresh();
            Debug.Log($"{DeleteAsmdef.count}個のファイルを消去しました");
        }
    }
}
   

internal class AsmdefCreator
{
    static int count;
    public static void CreateAll(DirectoryInfo rootInfo)
    {
        count = 0;
        var fileInfo = MakeFile(rootInfo);
        WriteAsmdefStr(rootInfo, new StreamWriter(fileInfo.fs),fileInfo.asmName );

        string rootAsmdefGuid = AssetDatabase.AssetPathToGUID(fileInfo.fs.Name.Substring(fileInfo.fs.Name.IndexOf("Assets")) );
        
        if (string.IsNullOrEmpty(rootAsmdefGuid))
        {
            Debug.LogError("asmdefguidを取得できていない");
        }
        
        foreach (DirectoryInfo info in rootInfo.GetDirectories())
        {
            Create(info, rootAsmdefGuid);
        }
        Debug.Log($"{count}個 asmdefファイルを作成しました");
    }
    static void Create(DirectoryInfo dirInfo,string rootAsmdefGuid)
    {
        if (dirInfo.Name == "Editor")//エディタフォルダの場合
        {
            //スクリプトファイルが無くてもDllファイルが無ければ作成する
            if (dirInfo.GetFiles("*.cs").Length <= 0)//スクリプトファイルがない
            {
                if (dirInfo.GetFiles("*.dll").Length > 0)//スクリプトファイルが無くてdllファイルがあれば作成しない
                {
                    Debug.Log("csファイルが無くDLLファイルがあったので作成しない-" + dirInfo.FullName);
                    return;
                }
            }
            var fileInfo = MakeFile(dirInfo, rootAsmdefGuid);
            WriteAsmdefStr(dirInfo, new StreamWriter(fileInfo.fs), fileInfo.asmName, rootAsmdefGuid);
            return;//Editorフォルダに当たったら下の階層のフォルダは処理しない
        }

        foreach (DirectoryInfo inforow in dirInfo.GetDirectories())
        {
            Create(inforow,rootAsmdefGuid);
        }
    }
    static (FileStream fs,string asmName) MakeFile(DirectoryInfo info)
    {
        string asmName = AsmdefName(info);
        string fullName = info.FullName + Path.DirectorySeparatorChar + asmName;

        return (File.Create(fullName),asmName);
    }
    static (FileStream fs, string asmName) MakeFile(DirectoryInfo info,string rootGuid)
    {
        string asmName = AsmdefName(info, rootGuid);
        string fullName = info.FullName + Path.DirectorySeparatorChar + asmName;
        return (File.Create(fullName), asmName);
    }
    /// <summary>
    /// asmdefファイルにJson文字列を書き込む
    /// </summary>
    /// <param name="asmName"></param>
    /// <returns></returns>
    static void WriteAsmdefStr(DirectoryInfo info, StreamWriter sw,string asmName )
    {
        var str = new AsmdefJson();
        str.name = asmName;
        sw.Write(str.ToString());
        sw.Flush();
        sw.Close();
        count++;
    }

    /// <summary>
    /// asmdefファイルにJson文字列を書き込む
    /// </summary>
    /// <param name="asmName"></param>
    /// <param name="rootAsmdefGuid"></param>
    /// <returns></returns>
    static void WriteAsmdefStr(DirectoryInfo info, StreamWriter sw ,string asmName, string rootAsmdefGuid)
    {
        var json = new AsmdefJson();
        json.name = asmName;
        json.references.Add($"GUID:{rootAsmdefGuid}");
        json.includePlatforms.Add("Editor");
        sw.Write(json.ToString());
        sw.Flush();
        sw.Close();
        count++;
    }
    /// <summary>
    /// asmdefファイルの名前が被らないように乱数入れたりしてみてる
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static string AsmdefName(DirectoryInfo info)
    {
        return  info.Name  + ".asmdef";
    }
    static string AsmdefName(DirectoryInfo info,string rootGuid)
    {
        int rand = Random.Range(0, 10000);
        return info.Parent.Name + info.Name + rand + ".asmdef";
    }
}
/// <summary>
/// 選択したフォルダ以下のasmdefファイルを全て消去する
/// </summary>
internal  class DeleteAsmdef 
{
    public static int count = 0;
    public static void Delete(DirectoryInfo dirInfo)
    {
        foreach (FileInfo fileinfo in dirInfo.GetFiles("*.asmdef"))
        {
            int index = fileinfo.FullName.IndexOf("Assets");
            string path = fileinfo.FullName.Substring(index);
            AssetDatabase.DeleteAsset(path);
            count++;
        }
        foreach (DirectoryInfo childInfo in dirInfo.GetDirectories())
        {
            Delete(childInfo);
        }
    }
}
/// <summary>
/// JsonUtilityを使うために作ったクラス
/// </summary>
[System.Serializable]
internal class AsmdefJson
{
    public string name;
    public bool allowUnsafeCode = false, overrideReferences = false, autoReferenced = true;
    public List<string> references = new List<string>(),
        optionallUnityReferences = new List<string>(),
        includePlatforms = new List<string>(),
        excludePlatforms = new List<string>(),
        precompiledReferences = new List<string>(),
        defineConstraints = new List<string>(),
        versionDefines = new List<string>();

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }
}

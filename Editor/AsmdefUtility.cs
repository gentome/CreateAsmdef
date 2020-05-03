using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Editorフォルダ以下においてください
/// </summary>
public class AsmdefUtility : EditorWindow
{
    int count;
    string[] asmdefGuidArray;
    string rootAsmdefGuid;
    DirectoryInfo rootInfo;
    [MenuItem("Window/CreateAsmdef")]
    static void Open()
    {
        GetWindow<AsmdefUtility>();
    }
    private void OnGUI()
    {
        count = 0;
        GUILayout.Label($@"
選択したフォルダ直下にasmdefファイルを作ります。
Editorフォルダ以下にも作ります
Editorフォルダ以下の場合、アセットのルートフォルダに作成したasmdefファイルへの参照を追加します
Assets直下のフォルダを指定しないとおかしくなるかも
参照が足りないときもあるので、その場合は自分で追加する必要があります");
        if (GUILayout.Button("create"))
        {
            string filepath = EditorUtility.OpenFolderPanel("title", "Assets", "_Project");
            rootAsmdefGuid = "";
            if (string.IsNullOrEmpty(filepath))//何も選択されていなければ
            {
                return;
            }
            rootInfo = new DirectoryInfo(filepath);
            //最初に選択したフォルダ直下にasmdefファイルを作成する
            MakeAsmdef(rootInfo.FullName, $"{rootInfo.Name}.asmdef", AsmType.root);
            //再帰的にフォルダの階層を降りて行ってEditorフォルダがあればasmdefファイルを作成
            CreateAsmdef(rootInfo);

            AssetDatabase.Refresh();
            Debug.Log(count + "個処理した");
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
            AllDelete(info);
            AssetDatabase.Refresh();

            Debug.Log(count + "個処理した");
        }
    }
    /// <summary>
    /// 再帰で下の階層のフォルダを検索していく
    /// Editorフォルダだったらasmdefファイルを作成する
    /// Editorフォルダで、スクリプトファイルが無くて、
    /// dllファイルがあればasmdefファイルを作成しない
    /// </summary>
    /// <param name="info"></param>
    void CreateAsmdef(DirectoryInfo info)
    {
        if (info.Name == "Editor")//エディタフォルダの場合
        {
            //スクリプトファイルが無くてもDllファイルが無ければ作成する
            if(info.GetFiles("*.cs").Length <= 0)//スクリプトファイルがない
            {
                if(info.GetFiles("*.dll").Length > 0)//スクリプトファイルが無くてdllファイルがあれば作成しない
                {
                    Debug.Log("csファイルが無くDLLファイルがあったので作成しない-" + info.FullName);
                    return;
                }
            }
            int random = UnityEngine.Random.Range(0, 10000);
            string asmName = $"{info.Parent.Name}{info.Name}{random}.asmdef";//asmdefのファイル名は被らないように乱数を適当にいれてる
            string directoryPath = info.FullName;
            MakeAsmdef(directoryPath, asmName,AsmType.editor);
            return;//Editorフォルダに当たったら下の階層のフォルダは処理しない
        }

        foreach(DirectoryInfo inforow in info.GetDirectories())
        {
            CreateAsmdef(inforow);
        }
    }
    
    void MakeAsmdef(string directoryPath,string asmName,AsmType asmtype)
    {
        if (!File.Exists(directoryPath+"\\"+asmName))
        {
            //Closeで閉じないと何かエラーが出ると思う
            File.Create(directoryPath + "\\" + asmName).Close();
        }
        string contents = "";
        switch (asmtype)
        {
            case AsmType.editor:
                contents =
$@" 
{{
    ""name"":""{asmName}"",
    ""optionalUnityReferences"": [],
    ""references"":[
        ""GUID:{rootAsmdefGuid}""
    ],
    ""includePlatforms"": [
        ""Editor""
    ],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": []
}}
";

                break;
            case AsmType.root:
                //ルートフォルダの時だけこの分岐に入る
                contents =
$@" 
{{
    ""name"":""{asmName}"",
    ""references"": [],
    ""optionalUnityReferences"": [],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": []
}}
";
                AssetDatabase.Refresh();//ガイドが取得できないかも？
                rootAsmdefGuid = AssetDatabase.AssetPathToGUID(rootInfo.FullName.Substring(rootInfo.FullName.IndexOf("Assets"))+"\\"+asmName);
                break;
        }

        File.WriteAllText(directoryPath+"\\"+asmName, contents);
       
        //Debug.Log(directoryPath + "\\" + asmName + "に作成した");
        count++;//ただ処理したasmdefファイルの数を数えてるだけ
    }

    /// <summary>
    /// フォルダ以下のasmdefファイルを全部消す
    /// </summary>
    /// <param name="info"></param>
    void AllDelete(DirectoryInfo info)
    {
        foreach(FileInfo fileinfo in info.GetFiles("*.asmdef"))
        {
            int index = fileinfo.FullName.IndexOf("Assets");
            string path = fileinfo.FullName.Substring(index);
            Debug.Log("ファイル消去する:"+path);
            AssetDatabase.DeleteAsset(path);
            count++;
        }
        foreach(DirectoryInfo childInfo in info.GetDirectories())
        {
            AllDelete(childInfo);
        }
    }
    enum AsmType { root,editor}
}


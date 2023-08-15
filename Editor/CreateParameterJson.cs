using System;
using UnityEditor;
using System.IO;
using UnityEngine;

namespace ParamGenerator.Editor
{
    public static class CreateTemplateJson
    {
        [MenuItem("Assets/Create/Parameter Json")]
        public static void CreateJson()
        {
            // ファイル保存ダイアログを表示
            string path = EditorUtility.SaveFilePanel(
                "Save JSON file",
                AssetDatabase.GetAssetPath(Selection.activeObject),
                "",
                "json"
            );

            // ユーザーがキャンセルした場合、パスが空になる
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // 新しいJsonObjectを作成
            JsonObject jsonObject = new JsonObject
            {
                parameters = Array.Empty<ParameterPair>(),
                csharpPath = "",
                className = "",
                nameSpace = "",
                assetPath = ""
            };
            
            string templateJson = JsonUtility.ToJson(jsonObject);
            
            File.WriteAllText(path, templateJson);
            
            AssetDatabase.Refresh();
        }
    }
}

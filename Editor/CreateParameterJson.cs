using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ParamGenerator.Editor
{
    public static class CreateTemplateJson
    {
        [MenuItem("Assets/Create/Parameter Json")]
        public static void CreateJson()
        {
            // ファイル保存ダイアログを表示
            var path = EditorUtility.SaveFilePanel(
                "Save JSON file",
                AssetDatabase.GetAssetPath(Selection.activeObject),
                "",
                "json"
            );

            // ユーザーがキャンセルした場合、パスが空になる
            if (string.IsNullOrEmpty(path)) return;

            // 新しいJsonObjectを作成
            var jsonObject = new JsonObject
            {
                parameters = Array.Empty<ParameterPair>(),
                csharpPath = "",
                className = "",
                nameSpace = "",
                assetPath = ""
            };

            var templateJson = JsonUtility.ToJson(jsonObject);

            File.WriteAllText(path, templateJson);

            AssetDatabase.Refresh();
        }
    }
}
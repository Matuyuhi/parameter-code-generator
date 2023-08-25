using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ParamGenerator.Editor
{
    public class ParameterEditor : EditorWindow
    {
        private string jsonPath;
        private JsonObject parameterData;

        private void OnEnable()
        {
            if (jsonPath == "" || !File.Exists(jsonPath)) return;
            var json = File.ReadAllText(jsonPath);
            if (json == "") return;
            parameterData = JsonUtility.FromJson<JsonObject>(json);
        }

        private void OnGUI()
        {
            if (jsonPath == "" || parameterData == null || !File.Exists(jsonPath)) return;

            EditorGUILayout.LabelField(Path.GetFileName(jsonPath), EditorStyles.boldLabel);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Edit Parameters", EditorStyles.boldLabel);

            GUILayout.Space(10);
            EditorGUILayout.BeginVertical("box"); // Enclose in a box for visual grouping

            string[] options = { "float", "int", "string", "Vector3", "Vector2" }; // サポートする型のオプション

            foreach (var parameter in parameterData.parameters)
            {
                EditorGUILayout.BeginHorizontal();

                parameter.key = EditorGUILayout.TextField(parameter.key, GUILayout.Width(80));
                var selectedIndex = EditorGUILayout.Popup(Array.IndexOf(options, parameter.type), options);
                if (selectedIndex < 0) selectedIndex = 0;

                parameter.type = options[selectedIndex];

                switch (parameter.type)
                {
                    case "float":
                        parameter.value = EditorGUILayout.FloatField(!string.IsNullOrEmpty(parameter.value) ? float.Parse(parameter.value) : 0f)
                            .ToString();
                        break;
                    case "int":
                        parameter.value = EditorGUILayout.IntField(!string.IsNullOrEmpty(parameter.value) ? int.Parse(parameter.value) : 0)
                            .ToString();
                        break;
                    case "string":
                        parameter.value = EditorGUILayout.TextField(parameter.value);
                        break;
                    case "Vector3":
                        var valueVector3 = MyUtil.TryParse3(parameter.value);
                        parameter.value = EditorGUILayout.Vector3Field("", valueVector3 ?? Vector3.zero).ToString();
                        break;
                    case "Vector2":
                        var valueVector2 = MyUtil.TryParse2(parameter.value);
                        parameter.value = EditorGUILayout.Vector2Field("", valueVector2 ?? Vector2.zero).ToString();
                        break;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    parameterData.parameters = parameterData.parameters.Where(p => p != parameter).ToArray();

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Parameter"))
            {
                var newList = parameterData.parameters.ToList();
                newList.Add(new ParameterPair());
                parameterData.parameters = newList.ToArray();
            }

            EditorGUILayout.EndVertical(); // End box

            GUILayout.Space(10);

            parameterData.csharpPath = EditorGUILayout.TextField("Output C# File", parameterData.csharpPath);
            if (GUILayout.Button("Select C# Output File"))
            {
                var path = EditorUtility.SaveFilePanel("Select C# Output File", "", "", "cs");
                if (!string.IsNullOrEmpty(path)) parameterData.csharpPath = MakeRelativePath(Application.dataPath, path);
            }

            GUILayout.Space(10);

            parameterData.assetPath = EditorGUILayout.TextField("Output Asset File", parameterData.assetPath);
            if (GUILayout.Button("Select Asset Output File"))
            {
                var path = EditorUtility.SaveFilePanel("Select Asset Output File", "", "", "asset");
                if (!string.IsNullOrEmpty(path)) parameterData.assetPath = MakeRelativePath(Application.dataPath, path);
            }

            GUILayout.Space(10);

            parameterData.className = EditorGUILayout.TextField("Class Name", Path.GetFileNameWithoutExtension(parameterData.csharpPath));

            parameterData.nameSpace = EditorGUILayout.TextField("Namespace", parameterData.nameSpace);

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                var json = JsonUtility.ToJson(parameterData);
                if (jsonPath != null)
                {
                    File.WriteAllText(jsonPath, json);
                    CodeGenerator.GenerateCode(jsonPath, parameterData.csharpPath, parameterData.assetPath);
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(100))) OnEnable();

            EditorGUILayout.EndHorizontal();
        }

        // Utility function to convert an absolute path to a relative path.
        private string MakeRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) return toPath; // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }

        [MenuItem("Assets/Edit Parameters")]
        public static void ShowWindow()
        {
            var window = GetWindow<ParameterEditor>("Parameter Editor");
            window.jsonPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            window.OnEnable();
        }


        // MenuItemのバリデーションメソッド
        [MenuItem("Assets/Edit Parameters", true)]
        private static bool ValidateMenuOption()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith(".json");
        }

        private static class MyUtil
        {
            public static Vector3? TryParse3(string a)
            {
                var input = a;
                input = input.Trim('(', ')'); // 括弧を削除
                var parts = input.Split(','); // カンマで分割

                if (parts.Length == 3) // x, y, zの3部分があることを確認
                {
                    if (float.TryParse(parts[0].Trim(), out var x) &&
                        float.TryParse(parts[1].Trim(), out var y) &&
                        float.TryParse(parts[2].Trim(), out var z))
                    {
                        var result = new Vector3(x, y, z);
                        return result;
                    }

                    return null;
                }

                return null;
            }

            public static Vector2? TryParse2(string a)
            {
                var input = a;
                input = input.Trim('(', ')'); // 括弧を削除
                var parts = input.Split(','); // カンマで分割

                if (parts.Length == 2) // x, y, zの3部分があることを確認
                {
                    if (float.TryParse(parts[0].Trim(), out var x) &&
                        float.TryParse(parts[1].Trim(), out var y))
                    {
                        var result = new Vector2(x, y);
                        return result;
                    }

                    return null;
                }

                return null;
            }
        }
    }
}
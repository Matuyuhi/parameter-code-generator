using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ParamGenerator.Editor
{
    public class ParameterEditor : EditorWindow
    {
        private JsonObject parameterData;
        private string jsonPath;

        [MenuItem("Assets/Edit Parameters")]
        public static void ShowWindow()
        {
            ParameterEditor window = GetWindow<ParameterEditor>("Parameter Editor");
            window.jsonPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            window.OnEnable();
        }

        private void OnEnable()
        {
            string json = File.ReadAllText(jsonPath);
            if (json == "") return;
            parameterData = JsonUtility.FromJson<JsonObject>(json);
        }

        private void OnGUI()
        {
            if (jsonPath == "" || parameterData == null) return;

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
                int selectedIndex = EditorGUILayout.Popup(Array.IndexOf(options, parameter.type), options);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                parameter.type = options[selectedIndex];

                switch (parameter.type)
                {
                    case "float":
                        parameter.value = EditorGUILayout.FloatField(float.Parse(parameter.value)).ToString();
                        break;
                    case "int":
                        parameter.value = EditorGUILayout.IntField(int.Parse(parameter.value)).ToString();
                        break;
                    case "string":
                        parameter.value = EditorGUILayout.TextField(parameter.value);
                        break;
                    case "Vector3":
                        Vector3? valueVector3 = MyUtil.TryParse3(parameter.value);
                        parameter.value = EditorGUILayout.Vector3Field("", valueVector3 ?? Vector3.zero).ToString();
                        break;
                    case "Vector2":
                        Vector2? valueVector2 = MyUtil.TryParse2(parameter.value);
                        parameter.value = EditorGUILayout.Vector2Field("", valueVector2 ?? Vector2.zero).ToString();
                        break;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    parameterData.parameters = parameterData.parameters.Where(p => p != parameter).ToArray();
                }

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
                string path = EditorUtility.SaveFilePanel("Select C# Output File", "", "", "cs");
                if (!string.IsNullOrEmpty(path))
                {
                    parameterData.csharpPath = path;
                }
            }

            GUILayout.Space(10);

            parameterData.assetPath = EditorGUILayout.TextField("Output Asset File", parameterData.assetPath);
            if (GUILayout.Button("Select Asset Output File"))
            {
                string path = EditorUtility.SaveFilePanel("Select Asset Output File", "", "", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    parameterData.assetPath = path;
                }
            }

            GUILayout.Space(10);

            parameterData.className = EditorGUILayout.TextField("Class Name", Path.GetFileNameWithoutExtension(parameterData.csharpPath));

            parameterData.nameSpace = EditorGUILayout.TextField("Namespace", parameterData.nameSpace);

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                string json = JsonUtility.ToJson(parameterData);
                File.WriteAllText(jsonPath, json);
                CodeGenerator.GenerateCode(jsonPath, parameterData.csharpPath, parameterData.assetPath);
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                OnEnable();
            }

            EditorGUILayout.EndHorizontal();
        }


        // MenuItemのバリデーションメソッド
        [MenuItem("Assets/Edit Parameters", true)]
        private static bool ValidateMenuOption()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith(".json");
        }

        public static class MyUtil
        {
            public static Vector3? TryParse3(string a)
            {
                string input = a;
                input = input.Trim('(', ')'); // 括弧を削除
                string[] parts = input.Split(','); // カンマで分割

                if (parts.Length == 3) // x, y, zの3部分があることを確認
                {
                    float x, y, z;
                    if (float.TryParse(parts[0].Trim(), out x) &&
                        float.TryParse(parts[1].Trim(), out y) &&
                        float.TryParse(parts[2].Trim(), out z))
                    {
                        Vector3 result = new Vector3(x, y, z);
                        return result;
                    }

                    return null;
                }

                return null;
            }

            public static Vector2? TryParse2(string a)
            {
                string input = a;
                input = input.Trim('(', ')'); // 括弧を削除
                string[] parts = input.Split(','); // カンマで分割

                if (parts.Length == 2) // x, y, zの3部分があることを確認
                {
                    float x, y;
                    if (float.TryParse(parts[0].Trim(), out x) &&
                        float.TryParse(parts[1].Trim(), out y))
                    {
                        Vector2 result = new Vector2(x, y);
                        return result;
                    }

                    return null;
                }

                return null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
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
            parameterData = JsonUtility.FromJson<JsonObject>(json);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(Path.GetFileName(jsonPath), EditorStyles.boldLabel);
            GUILayout.Space(30);
            List<ParameterPair> parameterList = new List<ParameterPair>(parameterData.parameters);
            string output = parameterData.outputFileName;
            string className = parameterData.className;
            string nameSpace = parameterData.nameSpace;
            
            EditorGUILayout.LabelField("Edit Parameters", EditorStyles.boldLabel);
        
            string[] options = { "float", "int", "string", "Vector3", "Vector2" }; // サポートする型のオプション

            bool canApply = true;

            foreach (var parameter in parameterList.ToArray())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(30);
                parameter.key = EditorGUILayout.TextField(parameter.key);
                EditorGUILayout.Space();
                int selectedIndex = EditorGUILayout.Popup(Array.IndexOf(options, parameter.type), options);
                if (selectedIndex < 0) {
                    selectedIndex = 0;
                }
                parameter.type = options[selectedIndex];
                EditorGUILayout.Space();
                // 選択された型に応じた入力フィールドを表示
                bool tried;
                switch (parameter.type)
                {
                    case "float":
                        tried = float.TryParse(parameter.value, out var valueFloat);
                        parameter.value = EditorGUILayout.FloatField(tried ? valueFloat : 0f).ToString();
                        break;
                    case "int":
                        tried = int.TryParse(parameter.value, out var valueInt);
                        parameter.value = EditorGUILayout.IntField(tried ? valueInt : 0).ToString();
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
                EditorGUILayout.Space();
            
                if (GUILayout.Button("Remove"))
                {
                    parameterList.Remove(parameter); // パラメーターの削除
                }
                
                if (string.IsNullOrEmpty(parameter.key) || string.IsNullOrEmpty(parameter.value) || string.IsNullOrEmpty(parameter.type))
                {
                    canApply = false;
                }
            
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Parameter"))
            {
                parameterList.Add(new ParameterPair());
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Output File"))
            {
                string path = EditorUtility.SaveFilePanel("Select output file", "", "", "cs");
                if (!string.IsNullOrEmpty(path))
                {
                    output = path;
                }
                else
                {
                    canApply = false;
                }
            }
            EditorGUILayout.TextField(output); // 選択したファイルパスを表示
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("className");
            className = Path.GetFileNameWithoutExtension(output);
            EditorGUILayout.LabelField(className);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("namespace");
            nameSpace = EditorGUILayout.TextField(nameSpace);
            if (string.IsNullOrEmpty(nameSpace))
            {
                canApply = false;
            }
            EditorGUILayout.EndHorizontal();
        
            parameterData.parameters = parameterList.ToArray();
            parameterData.outputFileName = output;
            parameterData.className = className;
            parameterData.nameSpace = nameSpace;
            

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply"))
            {
                if (canApply)
                {
                    string json = JsonUtility.ToJson(parameterData);
                    File.WriteAllText(jsonPath, json);
                    CodeGenerator.GenerateCode(jsonPath, output); 
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogWarning("Please fill all the parameters before applying.");
                }
                
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Cancel"))
            {
                // Reload the JSON data
                OnEnable();
            }
            EditorGUILayout.Space(40);
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
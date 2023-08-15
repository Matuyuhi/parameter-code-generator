using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace ParamGenerator.Editor
{
    public static class CodeGenerator
    {
        private static JsonObject jsonObject;
        private static string outputPath;
        private static string outputAsset;

        public static void GenerateCode(string jsonPath, string outputPath, string assetPath)
        {
            try
            {
                CodeGenerator.outputPath = outputPath;
                outputAsset = assetPath;
                
                string json = File.ReadAllText(jsonPath);

                jsonObject = JsonUtility.FromJson<JsonObject>(json);

                string code = $"using UnityEngine;\n";
                string doName = "";
                if (jsonObject.nameSpace != "")
                {
                    code += $"namespace {jsonObject.nameSpace}\n{{\n    ";
                    doName = "    ";
                }
                if (string.IsNullOrEmpty(jsonObject.className))
                {
                    throw new Exception("Not Found ClassName");
                }
                code += $"public class {jsonObject.className} : ScriptableObject\n{doName}{{\n";
                foreach (var parameter in jsonObject.parameters)
                {
                    string key = parameter.key;
                    string type = parameter.type;
                    string valueString = parameter.value;
                    if (string.IsNullOrEmpty(parameter.key) || string.IsNullOrEmpty(parameter.value) || string.IsNullOrEmpty(parameter.type))
                    {
                        continue;
                    }
                    string value = ConvertValue(type, valueString);
                    code += $"    {doName}public {type} {key} = {value};\n\n";
                }

                if (jsonObject.nameSpace != "")
                {
                    code += "    }\n";
                }

                code += "}";
                
                File.WriteAllText(outputPath, code);
                AssetDatabase.Refresh();

                EditorApplication.update += WaitForCompile;



            }
            catch (Exception e)
            {
                Debug.LogError(e);
                AssetDatabase.Refresh();
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }

        private static void WaitForCompile()
        {
            try
            {
                if (EditorApplication.isCompiling)
                    return;

                EditorApplication.update -= WaitForCompile;
                
                string qualifiedTypeName = (jsonObject.nameSpace != "" ? jsonObject.nameSpace + "." : "") + jsonObject.className;
                Type generatedType = null;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in asm.GetTypes())
                    {
                        if (type.FullName == qualifiedTypeName)
                        {
                            generatedType = type;
                            break;
                        }
                    }

                    if (generatedType != null)
                        break;
                }

                if (generatedType != null)
                {
                    ScriptableObject asset = ScriptableObject.CreateInstance(generatedType);
                    string assetPath = outputAsset;
                    AssetDatabase.CreateAsset(asset, assetPath);
                    Debug.Log("Generated type: " + generatedType);
                    Debug.Log("Asset path: " + assetPath);

                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError("Generated type not found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        


        private static string ConvertValue(string type, string valueString)
        {
            switch (type)
            {
                case "float": return valueString + "f";
                case "int": return valueString;
                case "string": return $"\"{valueString}\"";
                case "Vector3":
                {
                    string modifiedInput = Regex.Replace(valueString, @"(\d+\.\d+)", "$1f");
                    return $"new Vector3{modifiedInput}";
                }
                case "Vector2":
                {
                    string modifiedInput = Regex.Replace(valueString, @"(\d+\.\d+)", "$1f");
                    return $"new Vector2{modifiedInput}";
                }
                default: return valueString;
            }
        }
    }

    [Serializable]
    public class ParameterPair
    {
        public string key;
        public string type;
        public string value;
        
    }

    [Serializable]
    public class JsonObject
    {
        public ParameterPair[] parameters;
        public string csharpPath;
        public string className;
        public string nameSpace;
        public string assetPath;
    }
}
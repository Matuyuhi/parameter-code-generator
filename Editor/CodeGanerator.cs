using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ParamGenerator.Editor
{
    internal static class CodeGenerator
    {
        public static void GenerateCode(string jsonPath, string outputPath, string assetPath)
        {
            try
            {
                GeneratingFlagHolder.instance.OutputPath = assetPath;

                var json = File.ReadAllText(jsonPath);

                GeneratingFlagHolder.instance.Json = json;

                var jsonObject = JsonUtility.FromJson<JsonObject>(json);

                var code = "using UnityEngine;\n";
                var doName = "";
                if (jsonObject.nameSpace != "")
                {
                    code += $"namespace {jsonObject.nameSpace}\n{{\n    ";
                    doName = "    ";
                }

                if (string.IsNullOrEmpty(jsonObject.className)) throw new Exception("Not Found ClassName");
                code += $"public class {jsonObject.className} : ScriptableObject\n{doName}{{\n";
                foreach (var parameter in jsonObject.parameters)
                {
                    var key = parameter.key;
                    var type = parameter.type;
                    var valueString = parameter.value;
                    if (string.IsNullOrEmpty(parameter.key) || string.IsNullOrEmpty(parameter.value) ||
                        string.IsNullOrEmpty(parameter.type)) continue;
                    var value = ConvertValue(type, valueString);
                    code += $"    {doName}public {type} {key} = {value};\n\n";
                }

                if (jsonObject.nameSpace != "") code += "    }\n";

                code += "}";

                File.WriteAllText(outputPath, code);

                GeneratingFlagHolder.instance.IsGenerating = true;

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                //AssetDatabase.Refresh();
            }
        }

        [InitializeOnLoadMethod]
        private static void WaitForCompile()
        {
            try
            {
                if (!GeneratingFlagHolder.instance.IsGenerating)
                    return;

                GeneratingFlagHolder.instance.IsGenerating = false;

                var jsonObject = JsonUtility.FromJson<JsonObject>(GeneratingFlagHolder.instance.Json);

                var qualifiedTypeName = (jsonObject.nameSpace != "" ? jsonObject.nameSpace + "." : "") + jsonObject.className;
                Type generatedType = null;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                        if (type.FullName == qualifiedTypeName)
                        {
                            generatedType = type;
                            break;
                        }

                    if (generatedType != null)
                        break;
                }

                if (generatedType != null)
                {
                    var asset = ScriptableObject.CreateInstance(generatedType);
                    var assetPath = GeneratingFlagHolder.instance.OutputPath;
                    AssetDatabase.CreateAsset(asset, assetPath);

                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError("Generated type not found");
                    GeneratingFlagHolder.instance.IsGenerating = false;
                }

                EditorApplication.update -= WaitForCompile;
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
                    var modifiedInput = Regex.Replace(valueString, @"(\d+\.\d+)", "$1f");
                    return $"new Vector3{modifiedInput}";
                }
                case "Vector2":
                {
                    var modifiedInput = Regex.Replace(valueString, @"(\d+\.\d+)", "$1f");
                    return $"new Vector2{modifiedInput}";
                }
                default: return valueString;
            }
        }

        internal class GeneratingFlagHolder : ScriptableSingleton<GeneratingFlagHolder>
        {
            private bool isGenerating;
            private string json;

            private string outputPath;

            public bool IsGenerating
            {
                get => isGenerating;
                set
                {
                    isGenerating = value;
                    Save(false);
                }
            }

            public string Json
            {
                get => json;
                set
                {
                    json = value;
                    Save(false);
                }
            }

            public string OutputPath
            {
                get => outputPath;
                set
                {
                    outputPath = value;
                    Save(false);
                }
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
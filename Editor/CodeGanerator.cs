using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ParamGenerator.Editor
{
    public static class CodeGenerator
    {

        public static void GenerateCode(string jsonPath, string outputPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);

                var jsonObject = JsonUtility.FromJson<JsonObject>(json);

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
                code += $"public class {jsonObject.className}\n{doName}{{\n";
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
                    code += $"    {doName}public static {type} {key} {{ get; }} = {value};\n\n";
                }

                if (jsonObject.nameSpace != "")
                {
                    code += "    }\n";
                }

                code += "}";
                
                File.WriteAllText(outputPath, code);
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
        public string outputFileName;
        public string className;
        public string nameSpace;
    }
}
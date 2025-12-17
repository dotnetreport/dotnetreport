using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReportBuilder.Web.Models
{    
    public class DynamicCodeRunner
    {
        private static readonly Dictionary<string, Func<object>> _compiledCache = new Dictionary<string, Func<object>>();

        public static object RunCode(string code)
        {
            if (_compiledCache.TryGetValue(code, out var cached))
            {
                return cached();
            }

            string methodName = "Execute";
            string typeName = "SnippetAssembly.SnippetClass";
            string sourceCode = GenerateSnippetCode(methodName, code);
            string binPath = AppContext.BaseDirectory;
            string outputPath = Path.Combine(binPath, "DynamicAssembly.dll");

            Assembly.LoadFrom(outputPath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(outputPath)
            };

            var compilation = CSharpCompilation.Create(
                "DynamicSnippetAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => $"Error: {d.GetMessage()}")
                    .ToList();

                return string.Join("\n", errors);
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var method = assembly.GetType(typeName).GetMethod(methodName);
            var func = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), method);

            _compiledCache[code] = func;

            return func();
        }

        public static object CallFunction(Type compiledType, string functionName, params object[] parameters)
        {

            MethodInfo method = compiledType.GetMethod(functionName);

            if (method == null)
            {
                throw new Exception($"Method '{functionName}' not found in compiled type.");
            }

            return method.Invoke(null, parameters);  // Null because it's a static class
        }

        public static void BuildAssembly(List<CustomFunctionModel> functions)
        {
            string outputPath = Path.Combine(AppContext.BaseDirectory, "DynamicAssembly.dll");

            string sourceCode = GenerateExecutableCode(functions);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "DynamicAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using (var fs = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                var emitResult = compilation.Emit(fs);

                if (!emitResult.Success)
                {
                    var errors = emitResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.GetMessage());

                    throw new Exception(string.Join(Environment.NewLine, errors));
                }

                fs.Flush(true);
            } 
        }


        private static string GetDefaultValue(string dataType, string defaultValue)
        {
            switch (dataType)
            {
                case "int": return string.IsNullOrEmpty(defaultValue) ? "0" : defaultValue;
                case "string": return string.IsNullOrEmpty(defaultValue) ? "\"\"" : $"\"{defaultValue}\"";
                case "bool": return string.IsNullOrEmpty(defaultValue) ? "false" : defaultValue.ToLower();
                case "float": return string.IsNullOrEmpty(defaultValue) ? "0f" : defaultValue + "f";
                default: return string.IsNullOrEmpty(defaultValue) ? "null" : defaultValue;
            }
        }

        public static string GenerateFunctionCode(CustomFunctionModel model)
        {
            string parameterList = string.Join(", ", model.Parameters.Select(p => $"{(string.IsNullOrEmpty(p.DataType) ? "object" : p.DataType)} {p.ParameterName} = {GetDefaultValue(p.DataType, p.DefaultValue)}"));

            return "        public static " + (string.IsNullOrEmpty(model.ResultDataType) ? "object" : model.ResultDataType) + " " + model.Name + "(" + parameterList + ")\n" +
                   "        {\n" +
                   "            " + model.Code + "\n" +
                   "        }\n";
        }

        private static string GenerateSnippetCode(string methodName, string code)
        {
            string modifiedCode = PrefixFunctionCalls(code);

            var dynamicCode =
                "using System;\n" +
                "using DynamicAssembly;\n" +
                "namespace SnippetAssembly\n" +
                "{\n" +
                "    public static class SnippetClass\n" +
                "    {\n" +
                "        public static object " + methodName + "()\n" +
                "        {\n" +
                "            return " + modifiedCode + ";\n" +
                "        }\n" +
                "    }\n" +
                "}";

            return dynamicCode;
        }

        private static string PrefixFunctionCalls(string inputCode)
        {
            // Regex to detect function calls without a namespace/class prefix
            string pattern = @"(?<!\w\.)\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(";

            return Regex.Replace(inputCode, pattern, match =>
            {
                string functionName = match.Groups[1].Value;

                if (IsSystemKeyword(functionName))
                    return match.Value;

                return "DynamicClass." + match.Value;
            });
        }

        private static bool IsSystemKeyword(string word)
        {
            string[] keywords = { "if", "else", "for", "while", "return", "switch", "case", "new", "typeof", "sizeof", "true", "false" };
            return Array.Exists(keywords, keyword => keyword == word);
        }


        private static string GenerateExecutableCode(List<CustomFunctionModel> functions)
        {
            var dynamicCode =
                "using System;\n" +
                "namespace DynamicAssembly\n" +
                "{\n" +
                "    public static class DynamicClass\n" +
                "    {\n";

            foreach (var f in functions)
            {
                dynamicCode += GenerateFunctionCode(f);
            }

            dynamicCode +=
                "    }\n" +
                "}";
            return dynamicCode;

        }

    }
}

using System.Data;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Concurrent;

namespace ReportBuilder.Web.Models
{
    public static class DynamicCodeRunner
    {
        private static ConcurrentDictionary<string, Assembly> _assemblyCache = new ConcurrentDictionary<string, Assembly>();

        public async static Task<object> RunCode(string code)
        {
            string methodName = "Execute";
            string assemblyName = "DynamicAssembly";

            if (!_assemblyCache.TryGetValue(assemblyName, out Assembly assembly))
            {
                var functions = await DotNetReportHelper.GetApiFunctions();
                string sourceCode = GenerateExecutableCode(methodName, code, functions);
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                MetadataReference[] references = new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                };
                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        var failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                        throw new InvalidOperationException("Compilation failed: " + string.Join(", ", failures.Select(diag => diag.GetMessage())));
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    _assemblyCache[assemblyName] = assembly;
                }
            }

            Type type = assembly.GetType("DynamicNamespace.DynamicClass");
            MethodInfo method = type.GetMethod(methodName);
            return method.Invoke(null, null); // No parameters are needed here because they are included in the 'code'
        }

        public static string GenerateFunctionCode(CustomFunctionModel model)
        {
            string parameterList = string.Join(", ", model.Parameters.Select(p => $"{(string.IsNullOrEmpty(p.DataType) ? "object" : p.DataType)} {p.ParameterName}"));

            return "        public static " + (string.IsNullOrEmpty(model.ResultDataType) ? "object" : model.ResultDataType)  + " " + model.Name + "(" + parameterList + ")\n" +
                   "        {\n" +
                   "            " + model.Code + "\n" +
                   "        }\n";
        }

        private static string GenerateExecutableCode(string methodName, string code, List<CustomFunctionModel> functions)
        {
            var dynamicCode =
                "using System;\n" +
                "namespace DynamicNamespace\n" +
                "{\n" +
                "    public static class DynamicClass\n" +
                "    {\n" +
                "        public static object " + methodName + "()\n" +
                "        {\n" +
                "            return " + code + ";\n" + // Direct execution of the code string
                "        }\n";                
            
            foreach(var f in functions)
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

using DynamicExpresso;
using Microsoft.CSharp;
using Quartz.Impl.Triggers;
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace ReportBuilder.Web.Models
{
    public static class DynamicCodeRunner
    {

        private static readonly Regex _fnRegex = new Regex(
            @"/\*\|(.*?)\|\*/[^,]+AS\s+\[([^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly HashSet<Type> _numericTypes = new HashSet<Type> {
        typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(float)
        };


        // cache snippetText → compiled delegate
        static readonly ConcurrentDictionary<string, Func<object>> _snippetCache
          = new ConcurrentDictionary<string, Func<object>>();

        // one single regex instance (you had this already)
        static readonly Regex _prefixer = new Regex(
          @"(?<!\w\.)\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(",
          RegexOptions.Compiled);


        static readonly Regex _functionCall = new Regex(
            @"^(?<fn>\w+)\((?<args>.*)\);?$",
            RegexOptions.Compiled);

        /// <summary>
        /// Attempts to use CallFunction() to return result of expression
        /// Expect code in the format: FunctionName(arg1, arg2, ...)
        /// Fallbacks to compiling expression when arguments are not primitive
        /// </summary>
        public static object RunCode(string code)
        {
            var match = _functionCall.Match(code);
            if (!match.Success)
                throw new InvalidOperationException("Code must be a function call, e.g., MyFunc(1, \"foo\").");

            string functionName = match.Groups["fn"].Value;
            string argsString = match.Groups["args"].Value.Trim();

            var args = new List<object>();
            if (!string.IsNullOrWhiteSpace(argsString))
            {
                var argMatches = SplitArguments(argsString);
                var interpreter = new Interpreter();
                char[] symbols = { '=', '>', '<', '!', '+', '-', '*', '/' };

                foreach (string argMatch in argMatches)
                {
                    string arg = argMatch.Trim();

                    //check for nested function call
                    if (_functionCall.IsMatch(arg))
                    {
                        args.Add(RunCode(arg));
                        continue;
                    }

                    //try parsing as primitive type
                    if (TryParsePrimitive(arg, out object primitiveValue))
                    {
                        args.Add(primitiveValue);
                        continue;
                    }

                    //try evaluating with DynamicExpresso if expression
                    if (arg.IndexOfAny(symbols) >= 0)
                    {
                        try
                        {
                            args.Add(interpreter.Eval(arg));
                        }
                        catch
                        {
                            // Fallback, compiling full expression
                            return RunCodeCompile(code);
                        }
                    }
                    else
                    {
                        args.Add(arg.Trim('\"'));
                    }
                }
            }

            try
            {
                return CallFunction(functionName, args.ToArray());
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException ?? tie;
            }
        }

        private static List<string> SplitArguments(string argsString)
        {
            var args = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;
            int parenDepth = 0;

            for (int i = 0; i < argsString.Length; i++)
            {
                char c = argsString[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == '(' && !inQuotes)
                {
                    parenDepth++;
                    current.Append(c);
                }
                else if (c == ')' && !inQuotes)
                {
                    parenDepth--;
                    current.Append(c);
                }
                else if (c == ',' && !inQuotes && parenDepth == 0)
                {
                    args.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                args.Add(current.ToString().Trim());

            return args;
        }

        private static bool TryParsePrimitive(string arg, out object value)
        {
            if (decimal.TryParse(arg, out decimal dblResult)) { value = dblResult; return true; }
            if (DateTime.TryParse(arg, out DateTime dateTimeResult)) { value = dateTimeResult; return true; }
            if (arg.Equals("null", StringComparison.OrdinalIgnoreCase)) { value = null; return true; }
            value = null;
            return false;
        }

        /// <summary>
        /// Evaluate a single expression (snippet) and return its result.
        /// Compiles the snippet once, then re-uses a Func<object>.
        /// </summary>
        public static object RunCodeCompile(string code)
        {
            var assemblies = Config.CustomFunctionAssemblies;

            // get-or-add: compile this snippet only once
            var runner = _snippetCache.GetOrAdd(code, snippet =>
            {
                // 1) inject your prefixer
                string modified = PrefixFunctionCalls(snippet);

                // 2) wrap in a tiny class & method
                var src = new System.Text.StringBuilder();

                src.AppendLine("using System;");
                src.AppendLine("using System.Globalization;");
                src.AppendLine("using DynamicAssembly;");
                foreach (var assembly in assemblies)
                {
                    src.AppendLine($"using {assembly.Value};");
                }
                src.AppendLine("namespace SnippetAssembly {");
                src.AppendLine(" public static class SnippetClass {");
                src.AppendLine("     public static object Execute() {");
                src.AppendLine($"        return {modified};");
                src.AppendLine("     }");
                src.AppendLine(" }");
                src.AppendLine("}");

                // 3) compile in-memory
                var provider = new CSharpCodeProvider();
                var parms = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true
                };

                AddBinFolderReferences(parms, assemblies);

                var results = provider.CompileAssemblyFromSource(parms, src.ToString());
                if (results.Errors.HasErrors)
                {
                    var errs = string.Join("; ",
                      results.Errors
                             .Cast<CompilerError>()
                             .Select(e => e.ErrorText));
                    throw new InvalidOperationException("Snippet compile failed: " + errs);
                }

                // 4) grab the MethodInfo and create a Func<object>
                var asm = results.CompiledAssembly;
                var m = asm
                          .GetType("SnippetAssembly.SnippetClass")
                          .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

                return (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), m);
            });

            // invoke the cached delegate
            try
            {
                return runner();
            }
            catch (TargetInvocationException tie)
            {
                // unwrap exceptions if you like
                throw tie.InnerException ?? tie;
            }
        }

        // The one in-memory assembly holding all your CustomFunctionModel methods
        static Assembly _functionAssembly;
        static Type _functionType;

        // cache functionName → delegate(object[] → object)
        static readonly ConcurrentDictionary<string, Func<object[], object>> _fnCache
          = new ConcurrentDictionary<string, Func<object[], object>>();

        private static readonly object _buildLock = new object();

        /// <summary>
        /// Compile (in-memory) a set of strongly-typed methods once.
        /// After calling this, CallFunction(name, args) will work.
        /// </summary>
        public static void BuildAssembly(List<CustomFunctionModel> functions)
        {
            lock (_buildLock)
            {
                if (functions == null || functions.Count == 0)
                    throw new ArgumentException("No functions provided", nameof(functions));

                // generate one big class with all your methods
                var sb = new System.Text.StringBuilder();

                sb.AppendLine("using System;");
                sb.AppendLine("using System.Globalization;");
                foreach (var assembly in Config.CustomFunctionAssemblies)
                {
                    sb.AppendLine($"using {assembly.Value};");
                }

                sb.AppendLine("namespace DynamicAssembly {");
                sb.AppendLine("  public static class DynamicClass {");
                foreach (var f in functions)
                {
                    // parameter list with defaults
                    var plist = string.Join(", ",
                        f.Parameters.Select(p =>
                            $"{(string.IsNullOrEmpty(p.DataType) ? "object" : p.DataType)} {p.ParameterName} = {GetDefaultValue(p.DataType, p.DefaultValue)}"
                        ));

                    var retType = string.IsNullOrEmpty(f.ResultDataType) ? "object" : f.ResultDataType;
                    sb.AppendLine($"    public static {retType} {f.Name}({plist}) {{");
                    sb.AppendLine("      " + f.Code);
                    sb.AppendLine("    }");
                }
                sb.AppendLine("  }");
                sb.AppendLine("}");

                // compile in-memory
                var provider = new CSharpCodeProvider();
                var parms = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true
                };

                AddBinFolderReferences(parms, Config.CustomFunctionAssemblies, referenceDynamicAsm: false);

                var results = provider.CompileAssemblyFromSource(parms, sb.ToString());
                if (results.Errors.HasErrors)
                {
                    var errs = string.Join("; ",
                      results.Errors
                             .Cast<CompilerError>()
                             .Select(e => e.ErrorText));
                    throw new InvalidOperationException("BuildAssembly failed: " + errs);
                }

                _functionAssembly = results.CompiledAssembly;
                _functionType = _functionAssembly.GetType("DynamicAssembly.DynamicClass");

                // compile dll as a fallback
                string appDataPath = HostingEnvironment.MapPath("~/App_Data");
                string dynamicAssmblyPath = Path.Combine(appDataPath, "DynamicAssembly.dll");
                provider = new CSharpCodeProvider();
                parms = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = false,
                    OutputAssembly = dynamicAssmblyPath
                };

                AddBinFolderReferences(parms, Config.CustomFunctionAssemblies, referenceDynamicAsm: false);

                var dllResults = provider.CompileAssemblyFromSource(parms, sb.ToString());
                if (dllResults.Errors.HasErrors)
                {
                    var errs = string.Join("; ",
                      dllResults.Errors
                             .Cast<CompilerError>()
                             .Select(e => e.ErrorText));
                    throw new InvalidOperationException("BuildAssembly failed: " + errs);
                }
            }
        }

        /// <summary>
        /// Invoke one of the methods you compiled via BuildAssembly.
        /// Caches a Func<object[],object> wrapper on first use.
        /// </summary>
        public static object CallFunction(string functionName, params object[] parameters)
        {
            if (_functionType == null)
                throw new InvalidOperationException("Call BuildAssembly(...) before CallFunction.");

            // get-or-add the delegate wrapper
            var invoker = _fnCache.GetOrAdd(functionName, fn =>
            {
                var mi = _functionType.GetMethod(fn, BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                    throw new MissingMethodException($"'{fn}' not found");

                var paramInfos = mi.GetParameters();

                // wrap MethodInfo.Invoke in a fast Func<object[],object>
                return new Func<object[], object>(args =>
                {
                    //save parameter info to enable optional params
                    var fullArgs = new object[paramInfos.Length];
                    for (int i = 0; i < paramInfos.Length; i++)
                    {
                        if (args != null && i < args.Length && args[i] != null)
                            fullArgs[i] = args[i];
                        else if (paramInfos[i].HasDefaultValue)
                            fullArgs[i] = paramInfos[i].DefaultValue;
                        else
                            fullArgs[i] = Type.Missing;
                    }
                    return mi.Invoke(null, fullArgs);
                });
            });



            try
            {
                return invoker(parameters);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException ?? tie;
            }
        }

        //--- helpers ------------------------------------------------

        private static void AddBinFolderReferences(CompilerParameters parameters, Dictionary<string, string> assemblies, bool referenceDynamicAsm = true)
        {
            string binPath = HostingEnvironment.MapPath("~/bin");

            parameters.ReferencedAssemblies.Add("System.dll");
            if (referenceDynamicAsm == true)
            {
                string appDataPath = HostingEnvironment.MapPath("~/App_Data");
                string dynamicAssmblyPath = Path.Combine(appDataPath, "DynamicAssembly.dll");
                parameters.ReferencedAssemblies.Add(dynamicAssmblyPath);
            }

            if (Directory.Exists(binPath))
            {
                assemblies.Keys.ToList().ForEach(assembly =>
                {
                    string assemblyPath = Path.Combine(binPath, assembly);
                    if (!File.Exists(assemblyPath))
                    {
                        throw new Exception($"Assembly '{assembly}' not found in bin folder");
                    }

                    parameters.ReferencedAssemblies.Add(assemblyPath);
                });
            }
        }

        static string PrefixFunctionCalls(string input)
        {
            return _prefixer.Replace(input, match =>
            {
                var name = match.Groups[1].Value;
                if (IsKeyword(name))
                    return match.Value;
                return "DynamicClass." + match.Value;
            });
        }

        static bool IsKeyword(string w)
            => new[] { "if", "else", "for", "while", "return", "switch", "case", "new", "typeof", "sizeof", "true", "false" }
                .Contains(w);

        static string GetDefaultValue(string dt, string def)
        {
            switch (dt)
            {
                case "int": return string.IsNullOrEmpty(def) ? "0" : def;
                case "long": return string.IsNullOrEmpty(def) ? "0L" : def + "L";
                case "float": return string.IsNullOrEmpty(def) ? "0f" : def + "f";
                case "double": return string.IsNullOrEmpty(def) ? "0d" : def;
                case "decimal": return string.IsNullOrEmpty(def) ? "0m" : def + "m";
                case "bool": return string.IsNullOrEmpty(def) ? "false" : def.ToLower();
                case "string": return string.IsNullOrEmpty(def) ? "\"\"" : $"\"{def}\"";
                default: return string.IsNullOrEmpty(def) ? "null" : def;
            }
        }


        #region custom function keyword blacklisting
        private static readonly List<string> blacklistedWords = new List<string>
        {
            "abstract", "Activator.", "Assembly", "Console.", "DeclaredMethods", "Delegate", "DefinedTypes", "DllImport", "Environment.", "Exception(",
            "fixed(", "GC.", "GetEnumerator", ".GetInterfaces", ".GetMethod(", ".GetProperties(", "GetType", "InterfaceMethods", "Invoke", "lock(",
            "Memory<", "Microsoft.", "namespace", ".Net", "Object ", "OperatingSystem", "private volatile ", "protected volatile ", "public volatile ",
            "sizeof(", "static", "System;", "System.", "Thread", "throw", "typeof(", "unchecked", "using", " virtual "
        };

        private static readonly List<string> whitelistedWords = new List<string>
        {
            "System.DateTime", ".NetPaid", "System.Collections.Generic.List", "System.Collections.Generic.Dictionary", "System.Math",
            "System.Collections.ArrayList", "System.Linq.Enumerable", "System.StringComparison.OrdinalIgnoreCase", "System.Decimal.Parse",
            "System.Globalization.CultureInfo.GetCultureInfo", "System.Globalization.DateTimeStyles.None", "System.Guid.Empty"
        };

        /// <summary>
        /// Returns a list of black listed C# key words that are in the users script
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static List<string> ContainsBlacklistedScriptWords(string code)
        {
            var found = new List<string>();

            if (string.IsNullOrEmpty(code) == false)
            {
                // remove content within double quotes but keep the quotes
                string codeToCheck = Regex.Replace(code, "(\"[^\"]*\")", "\"\"");

                // replace multiple consecutive whitespace characters with a single space
                codeToCheck = Regex.Replace(codeToCheck, @"\s+", " ");

                // remove the space where a word followed by space and a full stop, e.g. "System ." becomes "System."
                codeToCheck = Regex.Replace(codeToCheck, @"(\w)\s+\.", "$1.");

                // remove the space where a full stop followed by space and a word, e.g. ". GetMethod" becomes ".GetMethod"
                codeToCheck = Regex.Replace(codeToCheck, @"\. +(\w)", ".$1");

                // remove the space where a word followed by space and an opening parenthesis, e.g. "typeof (" becomes "typeof("
                codeToCheck = Regex.Replace(codeToCheck, @"(\w)\s+\(", "$1(");

                // remove the space where a word followed by space and an arrow, e.g. "Memory <" becomes "Memory<"
                codeToCheck = Regex.Replace(codeToCheck, @"(\w)\s+<", "$1<");

                // remove the space where a word followed by space and a semicolon, e.g. "System ;" becomes "System;"
                codeToCheck = Regex.Replace(codeToCheck, @"(\w)\s+;", "$1;");

                // remove whitelisted words
                whitelistedWords.ForEach(whitelistedWord => codeToCheck = Regex.Replace(codeToCheck, whitelistedWord, ""));

                foreach (string nextWord in blacklistedWords)
                {
                    if (codeToCheck.Contains(nextWord) == true)
                    {
                        found.Add(nextWord);
                    }
                }
            }

            return found;
        }
        #endregion
    }

    public static class Config
    {
        private static Dictionary<string, string> _customFunctionAssemblies;
        public static Dictionary<string, string> CustomFunctionAssemblies
        {
            get
            {
                if (_customFunctionAssemblies == null)
                {
                    var section = ConfigurationManager.GetSection("customFunctionAssemblies") as NameValueCollection;
                    if (section != null && section.Count > 0)
                    {
                        _customFunctionAssemblies = section.AllKeys.ToDictionary(key => key, key => section[key]);
                    }
                    else
                    {
                        _customFunctionAssemblies = new Dictionary<string, string>();
                    }
                }
                return _customFunctionAssemblies;
            }
        }
    }
}
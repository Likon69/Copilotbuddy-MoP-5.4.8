using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ExpressionSet
    {
        private static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(void))
                return "void";

            if (!type.IsGenericType)
                return type.FullName;

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            StringBuilder name = new StringBuilder(type.Namespace + "." + genericTypeDefinition.Name.Remove(genericTypeDefinition.Name.IndexOf('`')));
            name.Append('<');

            Type[] genericArguments = type.GetGenericArguments();
            for (int i = 0; i < genericArguments.Length; i++)
            {
                Type genericArgument = genericArguments[i];
                name.Append(GetFriendlyTypeName(genericArgument));
                if (i != genericArguments.Length - 1)
                    name.Append(',');
            }

            name.Append('>');
            return name.ToString();
        }

        private static string GetRandomParameterName(Random random)
        {
            StringBuilder name = new StringBuilder(12);
            for (int i = 0; i < 12; i++)
                name.Append("ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvxyz"[random.Next("ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvxyz".Length)]);

            return name.ToString();
        }

        private static void EmitExpressionMethod(ExpressionEntry entry, StringBuilder source, ref int line, Random random)
        {
            string expression = entry.Expression.Replace(Environment.NewLine, "\n").Replace("\n", Environment.NewLine);
            entry.LineStart = line;
            string methodName = "DynamicExpression" + entry.LineStart;
            entry.CompiledFuncName = methodName;

            MethodInfo invokeMethod = entry.DelayCompiledExpression.DelegateType.GetMethod("Invoke");
            source.AppendFormat("public {0} {1}(", GetFriendlyTypeName(invokeMethod.ReturnType), methodName);

            ParameterInfo[] parameters = invokeMethod.GetParameters();
            string[] parameterNames = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                source.Append(GetFriendlyTypeName(parameters[i].ParameterType));
                parameterNames[i] = GetRandomParameterName(random);
                source.AppendFormat(" {0}", parameterNames[i]);
                if (i != parameters.Length - 1)
                    source.Append(",");
            }

            source.AppendLine(")");
            line++;
            source.AppendLine("{");
            line++;

            if (invokeMethod.ReturnType != typeof(void))
                source.Append("return ");

            source.AppendFormat("new {0}({1})(", GetFriendlyTypeName(entry.DelayCompiledExpression.DelegateType), expression);
            line += expression.Count(c => c == '\n');

            for (int i = 0; i < parameters.Length; i++)
            {
                source.Append(parameterNames[i]);
                if (i != parameters.Length - 1)
                    source.Append(",");
            }

            source.AppendLine(");");
            line++;
            source.AppendLine("}");
            line++;
        }

        private static StringBuilder BuildSource(out string typeName, IEnumerable<ExpressionEntry> entries)
        {
            StringBuilder source = new StringBuilder();
            int setId = Interlocked.Increment(ref _expressionSetCounter);
            typeName = "DynamicExpressionSet" + setId + ".DynamicExpressions";

            source.AppendLine("using System;\r\nusing System.Threading;\r\nusing System.Diagnostics;\r\nusing System.Drawing;\r\nusing System.Linq;\r\nusing System.Linq.Expressions;\r\nusing Styx;\r\nusing Styx.Common;\r\n\r\nusing Styx.Helpers;\r\nusing Styx.CommonBot;\r\nusing Styx.CommonBot.Frames;\r\nusing Styx.CommonBot.Routines;\r\nusing Styx.CommonBot.Inventory;\r\nusing Styx.CommonBot.POI;\r\n\r\nusing Styx.Pathing;\r\n\r\nusing Styx.WoWInternals;\r\nusing Styx.WoWInternals.WoWObjects;\r\nusing Styx.WoWInternals.WoWCache;\r\nusing Styx.WoWInternals.Misc;\r\nusing Styx.WoWInternals.World;                    \r\n\r\nusing Styx.Plugins;\r\n\r\nnamespace DynamicExpressionSet" + setId + "\r\n{\r\npublic class DynamicExpressions : Styx.CommonBot.Profiles.Quest.Order.ProfileHelperFunctionsBase\r\n{");

            int line = 1;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '\n')
                    line++;
            }

            Random random = new Random();
            foreach (ExpressionEntry entry in entries)
                EmitExpressionMethod(entry, source, ref line, random);

            source.Append("}\r\n}");
            return source;
        }

        public bool IsCompiled { get; private set; }

        public T Add<T>(string expression) where T : class
        {
            return (T)(object)Add(expression, typeof(T));
        }

        public object Add(string expression, Type delegateType)
        {
            if (!IsSupportedDelegateType(delegateType))
                throw new NotSupportedException("Only type parameters of System.Action and System.Func and their derivates are supported.");

            ExpressionEntry entry = new ExpressionEntry
            {
                DelayCompiledExpression = new DelayCompiledExpression(delegateType),
                Expression = expression
            };

            lock (_syncLock)
            {
                if (IsCompiled)
                    throw new InvalidOperationException("Cannot add an expression to an already compiled expression set");

                _entries.Add(entry);
            }

            return entry.DelayCompiledExpression.CallableExpression;
        }

        public bool Compile(out ExpressionError[] errors)
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                        references.Add(Styx.Common.Compiler.CodeCompiler.GetMetadataReference(assembly.Location));
                }
                catch (NotSupportedException)
                {
                }
            }

            List<ExpressionEntry> entries;
            lock (_syncLock)
            {
                entries = _entries.ToList();
                _entries.Clear();
                IsCompiled = true;
            }

            if (entries.Count <= 0)
            {
                errors = new ExpressionError[0];
                return true;
            }

            string typeName;
            string source = BuildSource(out typeName, entries).ToString();

            CSharpCompilation compilation = CSharpCompilation.Create(
                "DynamicExpressions_" + Guid.NewGuid().ToString("N"),
                new[] { CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8)) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (MemoryStream assemblyStream = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(assemblyStream);
                if (!emitResult.Success)
                {
                    Diagnostic[] diagnostics = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                    errors = new ExpressionError[diagnostics.Length];
                    for (int i = 0; i < diagnostics.Length; i++)
                    {
                        int line = diagnostics[i].Location.GetLineSpan().StartLinePosition.Line + 1;
                        int entryIndex = entries.Count - 1;
                        while (entryIndex >= 0 && line <= entries[entryIndex].LineStart)
                            entryIndex--;

                        errors[i] = new ExpressionError(entries[entryIndex].Expression, diagnostics[i].GetMessage(CultureInfo.InvariantCulture));
                    }

                    return false;
                }

                Type compiledType = Assembly.Load(assemblyStream.ToArray()).GetType(typeName);
                object instance = Activator.CreateInstance(compiledType);
                foreach (ExpressionEntry entry in entries)
                    entry.DelayCompiledExpression.CompiledExpression = Delegate.CreateDelegate(entry.DelayCompiledExpression.DelegateType, instance, entry.CompiledFuncName);

                errors = new ExpressionError[0];
                return true;
            }
        }

        private static bool IsSupportedDelegateType(Type type)
        {
            if (type == typeof(Action))
                return true;

            if (!type.IsGenericType)
                return false;

            type = type.GetGenericTypeDefinition();
            return type == typeof(Action<>) || type == typeof(Action<,>) || type == typeof(Action<,,>) || type == typeof(Action<,,,>) || type == typeof(Action<,,,,>) || type == typeof(Action<,,,,,>) || type == typeof(Action<,,,,,,>) || type == typeof(Action<,,,,,,,>) || type == typeof(Action<,,,,,,,,>) || type == typeof(Action<,,,,,,,,,>) || type == typeof(Action<,,,,,,,,,,>) || type == typeof(Action<,,,,,,,,,,,>) || type == typeof(Action<,,,,,,,,,,,,>) || type == typeof(Action<,,,,,,,,,,,,,>) || type == typeof(Action<,,,,,,,,,,,,,,>) || type == typeof(Action<,,,,,,,,,,,,,,,>) ||
                   type == typeof(Func<>) || type == typeof(Func<,>) || type == typeof(Func<,,>) || type == typeof(Func<,,,>) || type == typeof(Func<,,,,>) || type == typeof(Func<,,,,,>) || type == typeof(Func<,,,,,,>) || type == typeof(Func<,,,,,,,>) || type == typeof(Func<,,,,,,,,>) || type == typeof(Func<,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,,,,,,>) || type == typeof(Func<,,,,,,,,,,,,,,,,>);
        }

        private readonly object _syncLock = new object();
        private readonly List<ExpressionEntry> _entries = new List<ExpressionEntry>();
        private static int _expressionSetCounter;

        private class ExpressionEntry
        {
            public DelayCompiledExpression DelayCompiledExpression { get; set; }

            public string Expression { get; set; }

            public string CompiledFuncName { get; set; }

            public int LineStart { get; set; }
        }
    }
}

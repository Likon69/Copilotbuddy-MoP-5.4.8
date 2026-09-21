using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public static class ConditionHelper
    {
        internal static Func<bool> ParseConditionString(XAttribute node)
        {
            try
            {
                ExpressionBinder binder = ((IXmlLineInfo)node).HasLineInfo()
                    ? new ExpressionBinder(node.Value, ((IXmlLineInfo)node).LineNumber)
                    : new ExpressionBinder(node.Value, null);

                return binder.Evaluate;
            }
            catch (CantCompileException)
            {
                return null;
            }
        }

        public static Func<bool> ParseConditionString(string str)
        {
            try
            {
                return new ExpressionBinder(str, null).Evaluate;
            }
            catch (CantCompileException)
            {
                return null;
            }
        }

        private class ExpressionBinder
        {
            public ExpressionBinder(string expression, int? line = null)
            {
                _expression = expression;
                _line = line;

                if (GlobalSettings.Instance.ProfileDebuggingMode)
                {
                    _method = Compile(_expression, _line);
                    _methodAssigned = true;

                    if (_method == null)
                        throw new CantCompileException();
                }
            }

            public bool Evaluate()
            {
                if (!_methodAssigned)
                {
                    _method = Compile(_expression, _line);
                    _methodAssigned = true;
                }

                return _method != null && _method();
            }

            private static Func<bool> Compile(string expression, int? line)
            {
                Func<bool> method;
                if (_cachedMethods.TryGetValue(expression, out method))
                    return method;

                lock (_parserLock)
                {
                    if (line != null)
                        Logging.WriteDiagnostic("Compiling expression '{0}' @ line {1}", expression, line.Value);
                    else
                        Logging.WriteDiagnostic("Compiling expression '{0}'", expression);

                    method = QuickConditionMatcher.TryMatch(expression);
                    if (method == null)
                    {
                        ExpressionSet expressionSet = new ExpressionSet();
                        method = expressionSet.Add<Func<bool>>("() => " + expression);

                        ExpressionError[] errors;
                        if (!expressionSet.Compile(out errors))
                        {
                            method = null;

                            if (errors.Length > 0)
                            {
                                Logging.WriteDiagnostic("{0} errors encountered while compiling condition '{1}'", errors.Length, expression);
                                foreach (ExpressionError error in errors)
                                    Logging.WriteDiagnostic(error.Error);
                            }
                        }
                    }

                    _cachedMethods.Add(expression, method);
                }

                return method;
            }

            private static readonly Dictionary<string, Func<bool>> _cachedMethods = new Dictionary<string, Func<bool>>();
            private static readonly object _parserLock = new object();

            private readonly string _expression;
            private readonly int? _line;
            private Func<bool> _method;
            private bool _methodAssigned;
        }

        private static class QuickConditionMatcher
        {
            public static Func<bool> TryMatch(string expression)
            {
                if (string.IsNullOrWhiteSpace(expression))
                    throw new ArgumentException("Expression cannot be null or whitespace", "expression");

                foreach (PatternEntry entry in _patterns)
                {
                    Match match = entry.Regex.Match(expression);
                    if (!match.Success)
                        continue;

                    List<object> args = new List<object>();
                    int argIndex = 1;
                    while (true)
                    {
                        Group group = match.Groups["Arg" + argIndex];
                        if (!group.Success)
                            break;

                        args.Add(group.Value);
                        argIndex++;
                    }

                    ParameterInfo[] parameters = entry.Evaluate.Method.GetParameters();
                    if (args.Count != parameters.Length)
                        throw new Exception("Regex was matched but the capture groups count wasn't the same as the number of arguments to the evaluate function!");

                    for (int i = 0; i < args.Count; i++)
                    {
                        try
                        {
                            args[i] = Convert.ChangeType(args[i], parameters[i].ParameterType, CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is InvalidCastException) && !(ex is FormatException) && !(ex is OverflowException))
                                throw;

                            return null;
                        }
                    }

                    object[] arguments = args.ToArray();
                    return () => (bool)entry.Evaluate.DynamicInvoke(arguments);
                }

                return null;
            }

            private static readonly ProfileHelperFunctionsBase _helper = new ProfileHelperFunctionsBase();

            private static readonly List<PatternEntry> _patterns = new List<PatternEntry>
            {
                new PatternEntry(
                    new Regex(@"^\s*HasQuest\s*\(\s*(?<Arg1>\d+)\s*\)\s*&&\s*IsQuestCompleted\s*\((?<Arg2>\d+)\s*\)\s*$"),
                    new Func<uint, uint, bool>((questId, completedQuestId) => _helper.HasQuest(questId) && _helper.IsQuestCompleted(completedQuestId))),
                new PatternEntry(
                    new Regex(@"^\s*HasQuest\s*\(\s*(?<Arg1>\d+)\s*\)\s*&&\s*!IsQuestCompleted\s*\((?<Arg2>\d+)\s*\)\s*$"),
                    new Func<uint, uint, bool>((questId, completedQuestId) => _helper.HasQuest(questId) && !_helper.IsQuestCompleted(completedQuestId))),
                new PatternEntry(
                    new Regex(@"^\s*!HasQuest\s*\(\s*(?<Arg1>\d+)\s*\)\s*&&\s*IsQuestCompleted\s*\((?<Arg2>\d+)\s*\)\s*$"),
                    new Func<uint, uint, bool>((questId, completedQuestId) => !_helper.HasQuest(questId) && _helper.IsQuestCompleted(completedQuestId))),
                new PatternEntry(
                    new Regex(@"^\s*!HasQuest\s*\(\s*(?<Arg1>\d+)\s*\)\s*&&\s*!IsQuestCompleted\s*\((?<Arg2>\d+)\s*\)\s*$"),
                    new Func<uint, uint, bool>((questId, completedQuestId) => !_helper.HasQuest(questId) && !_helper.IsQuestCompleted(completedQuestId))),
                new PatternEntry(
                    new Regex(@"^\s*HasQuest\s*\(\s*(?<Arg1>\d+)\s*\)\s*$"),
                    new Func<uint, bool>(questId => _helper.HasQuest(questId))),
                new PatternEntry(
                    new Regex(@"^\s*!HasQuest\s*\(\s*(?<Arg1>\d+)\s*\)\s*$"),
                    new Func<uint, bool>(questId => !_helper.HasQuest(questId)))
            };

            private class PatternEntry
            {
                public PatternEntry(Regex regex, Delegate evaluate)
                {
                    Regex = regex;
                    Evaluate = evaluate;
                }

                public Regex Regex { get; private set; }
                public Delegate Evaluate { get; private set; }
            }
        }
    }
}

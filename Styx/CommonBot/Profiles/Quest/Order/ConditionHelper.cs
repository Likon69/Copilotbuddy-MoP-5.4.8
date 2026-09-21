using System;
using System.Collections.Generic;
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
    }
}

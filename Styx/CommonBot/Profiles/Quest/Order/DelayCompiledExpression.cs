using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    internal class DelayCompiledExpression
    {
        public DelayCompiledExpression(Type delegateType)
        {
            DelegateType = delegateType;

            FieldInfo compiledExpressionField = this.GetType().GetField("CompiledExpression");
            Expression compiledExpressionAccess = Expression.Field(Expression.Constant(this), compiledExpressionField);
            InvalidOperationException notCompiledException = new InvalidOperationException("The expression set that owns this expression has not yet been compiled");

            ParameterExpression[] parameters = delegateType.GetMethod("Invoke")
                .GetParameters()
                .Select(pm => Expression.Parameter(pm.ParameterType))
                .ToArray();

            CallableExpression = Expression.Lambda(
                delegateType,
                Expression.Block(
                    Expression.IfThen(
                        Expression.Equal(compiledExpressionAccess, Expression.Constant(null)),
                        Expression.Throw(Expression.Constant(notCompiledException))),
                    Expression.Call(
                        Expression.Convert(compiledExpressionAccess, delegateType),
                        delegateType.GetMethod("Invoke"),
                        parameters)),
                parameters).Compile();
        }

        public object CallableExpression { get; private set; }

        public Type DelegateType { get; private set; }

        public object CompiledExpression;
    }
}

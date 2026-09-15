using System.Linq.Expressions;
using System.Reflection;

namespace BudgetManager.Web.Extensions;

public static class ObjectExtensions
{
    public static TValue? GetValue<TValue, TTarget>(this TTarget target, Expression<Func<TTarget, TValue>> expr, TValue? defaultValue = default)
    {
        if (target == null)
            return defaultValue;

        if (expr == null)
            return defaultValue;

        var body = expr.Body;
        if (body is UnaryExpression)
            body = ((UnaryExpression)expr.Body).Operand;

        if (body is MemberExpression)
        {
            var mExpressions = new List<MemberExpression>();
            var mexpr = body;
            while (mexpr != null && mexpr is not ParameterExpression)
            {
                mExpressions.Add((MemberExpression)mexpr);
                mexpr = ((MemberExpression)mexpr).Expression;
            }

            object? value = target;
            if (mExpressions.Count > 0)
            {
                for (int i = mExpressions.Count - 1; i >= 0; i--)
                {
                    PropertyInfo? prop = (PropertyInfo?)mExpressions[i]?.Member;
                    value = prop?.GetValue(value);
                }
            }

            return (value is TValue v) ? v : defaultValue;
        }
        else
        {
            try
            {
                return expr.Compile().Invoke(target);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

using System;
using System.Linq.Expressions;

namespace Common.Helpers
{
    public static class Reflection
    {
        public static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            MemberExpression memberExpression = (propertyExpression.Body as MemberExpression);

            if (memberExpression == null)
                return null;

            return memberExpression.Member.Name;
        }
    }
}

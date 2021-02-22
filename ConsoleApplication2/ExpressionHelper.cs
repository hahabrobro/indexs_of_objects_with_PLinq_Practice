using System;
using System.Linq.Expressions;

namespace i4o
{
    static class ExpressionHelper
    {
        public static string GetMemberName<T, TProperty>(this Expression<Func<T, TProperty>> propertyExpression)
        {
            return ((MemberExpression)(((LambdaExpression)(propertyExpression)).Body)).Member.Name;
        }
    }

}

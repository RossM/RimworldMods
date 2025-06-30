using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace XylRacesCore
{
    public static class ReflectionHelpers
    {
        private static readonly Dictionary<(Type, string), object> getAccessors = new ();

        public static TResult Get<TResult>(this object obj, string fieldName)
        {
            return GetAccessor<TResult>(obj.GetType(), fieldName)(obj);
        }

        public static Func<object, TResult> GetAccessor<TResult>(Type type, string fieldName)
        {
            if (!getAccessors.TryGetValue((type, fieldName), out var accessor))
            {
                FieldInfo fieldDef = AccessTools.Field(type, fieldName);
                ParameterExpression parameter = Expression.Variable(typeof(object), "obj");
                accessor = Expression.Lambda<Func<object, TResult>>(
                    Expression.MakeMemberAccess(Expression.Convert(parameter, type), fieldDef), parameter).Compile();
                getAccessors.Add((type, fieldName), accessor);
            }

            return (Func<object, TResult>)accessor;
        }
    }
}

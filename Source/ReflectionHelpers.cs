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
        public delegate TResult FieldAccessor<out TResult>(object obj);

        private static readonly Dictionary<(Type, string), object> getAccessors = new ();

        public static TResult Get<TResult>(this object obj, string fieldName)
        {
            var fieldAccessor = GetAccessor<TResult>(obj, fieldName);
            return fieldAccessor(obj);
        }

        public static FieldAccessor<TResult> GetAccessor<TResult>(object obj, string fieldName)
        {
            var type = obj.GetType();
            if (!getAccessors.TryGetValue((type, fieldName), out var accessor))
            {
                FieldInfo fieldDef = AccessTools.Field(type, fieldName);
                var parameter = Expression.Variable(typeof(object), "obj");
                var lambda = Expression.Lambda<FieldAccessor<TResult>>(
                    Expression.MakeMemberAccess(Expression.Convert(parameter, type), fieldDef), parameter);
                accessor = lambda.Compile();
                getAccessors.Add((type, fieldName), accessor);
            }

            var fieldAccessor = (FieldAccessor<TResult>)accessor;
            return fieldAccessor;
        }
    }
}

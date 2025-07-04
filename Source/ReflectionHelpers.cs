using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace XylRacesCore
{
    public static class ReflectionHelpers
    {
        private static readonly Dictionary<(Type, string), object> getAccessors = new();
        private static readonly Dictionary<(Type, string), object> setAccessors = new();

        public static TResult Get<TResult>(this object obj, string fieldName)
        {
            return GetAccessor<TResult>(obj.GetType(), fieldName)(obj);
        }

        public static Func<object, TValue> GetAccessor<TValue>(Type type, string fieldName)
        {
            if (!getAccessors.TryGetValue((type, fieldName), out object accessor))
            {
                FieldInfo fieldDef = AccessTools.Field(type, fieldName);
                if (fieldDef.FieldType != typeof(TValue))
                    throw new NotSupportedException();
                ParameterExpression objParameter = Expression.Variable(typeof(object), "obj");
                accessor = Expression.Lambda<Func<object, TValue>>(
                    Expression.MakeMemberAccess(Expression.Convert(objParameter, type), fieldDef), objParameter).Compile();
                getAccessors.Add((type, fieldName), accessor);
            }

            return (Func<object, TValue>)accessor;
        }

        public static void Set<TResult>(this object obj, string fieldName, TResult value)
        {
            SetAccessor<TResult>(obj.GetType(), fieldName)(obj, value);
        }

        public static Action<object, TValue> SetAccessor<TValue>(Type type, string fieldName)
        {
            if (!setAccessors.TryGetValue((type, fieldName), out object accessor))
            {
                FieldInfo fieldDef = AccessTools.Field(type, fieldName);
                if (fieldDef.FieldType != typeof(TValue))
                    throw new NotSupportedException();
                ParameterExpression objParameter = Expression.Variable(typeof(object), "obj");
                ParameterExpression valueParameter = Expression.Variable(typeof(TValue), "value");
                accessor = Expression.Lambda<Action<object, TValue>>(
                    Expression.Assign(
                        Expression.MakeMemberAccess(Expression.Convert(objParameter, type), fieldDef), valueParameter),
                    objParameter, valueParameter).Compile();
                setAccessors.Add((type, fieldName), accessor);
            }

            return (Action<object, TValue>)accessor;
        }
    }
}

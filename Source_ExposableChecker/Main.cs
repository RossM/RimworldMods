using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Verse;
using UnityEngine;
using XylDisassembler;

namespace XylExposableChecker
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main(ModContentPack content) : Mod(content)
    {
        public static void Check(Type type)
        {
            string exposeDataFunc;
            if (typeof(IExposable).IsAssignableFrom(type))
                exposeDataFunc = "ExposeData";
            else if (typeof(ThingComp).IsAssignableFrom(type))
                exposeDataFunc = "PostExposeData";
            else if (typeof(HediffComp).IsAssignableFrom(type) || type.Name.EndsWith("Comp"))
                exposeDataFunc = "CompExposeData";
            else
                return;

            var fields = type.GetFields().Where(
                field => field.GetCustomAttribute<UnsavedAttribute>() == null &&
                         !field.Attributes.HasFlag(FieldAttributes.Literal) &&
                         !field.Attributes.HasFlag(FieldAttributes.Static) &&
                         field.DeclaringType == type).ToList();

            if (fields.Count == 0)
                return;

            HashSet<FieldInfo> usedFields = [];

            MethodInfo curMethod = type.GetMethod(exposeDataFunc);
            if (curMethod != null)
            {
                var method = Disassembler.Decode(curMethod);
                usedFields.AddRange(method.Instructions.Select(i => i.Value).OfType<FieldInfo>());
            }

            foreach (var field in fields.Except(usedFields))
            {
                Log.Warning($"Possibly unsaved field: {type.Namespace}.{type.Name}.{field.Name}. Either save this field in {exposeDataFunc}, mark it [Unsaved], or make it const or readonly.");
            }
        }

        [UsedImplicitly]
        static Main()
        {
            HashSet<Assembly> checkedAssemblies = [];

            foreach (Type type in GenTypes.AllTypes)
            {
                string assemblyName = type.Assembly.GetName().Name;
                bool skipAssembly = assemblyName == "Assembly-CSharp";
                if (!checkedAssemblies.Contains(type.Assembly))
                {
                    Debug.Log(skipAssembly
                        ? $"IExposable checker: skipping {assemblyName}"
                        : $"IExposable checker: checking {assemblyName}");
                    checkedAssemblies.Add(type.Assembly);
                }

                if (!skipAssembly) 
                    Check(type);
            }
        }
    }
}

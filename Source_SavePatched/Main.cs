using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylSavePatched
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main(ModContentPack content) : Mod(content)
    {
        static Main()
        {
            var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Output.DynamicAssembly"), AssemblyBuilderAccess.Save);

            Dictionary<Module, ModuleBuilder> moduleBuilders = new();
            Dictionary<Type, TypeBuilder> typeBuilders = new();

            foreach (var method in harmony.GetPatchedMethods())
            {
                var module = method.Module;
                if (!moduleBuilders.TryGetValue(module, out ModuleBuilder moduleBuilder))
                {
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(module.Name);
                    moduleBuilders.Add(module, moduleBuilder);
                    Log.Message($"moduleBuilder = {moduleBuilder}");
                }

                var type = method.DeclaringType;
                if (!typeBuilders.TryGetValue(type, out TypeBuilder typeBuilder))
                {
                    typeBuilder = MakeTypeBuilder(type, moduleBuilder);

                    typeBuilders.Add(type, typeBuilder);
                    Log.Message($"typeBuilder = {typeBuilder}");
                }

                var methodBody = method.GetMethodBody();
                if (method is MethodInfo methodInfo)
                {
                    var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodInfo.Attributes,
                        methodInfo.ReturnType, methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());

                    // TODO: Copy the IL to the new method
                    CopyMethodBody(methodBody, methodBuilder.GetILGenerator());

                    Log.Message($"methodBuilder = {methodBuilder}");
                }
            }
        }

        private static void CopyMethodBody(MethodBody methodBody, ILGenerator getIlGenerator)
        {
            throw new NotImplementedException();
        }

        private static TypeBuilder MakeTypeBuilder(Type type, ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder;
            typeBuilder = moduleBuilder.DefineType(type.FullName, type.Attributes, type.BaseType);

            foreach (var field in type.GetFields())
            {
                typeBuilder.DefineField(field.Name, field.FieldType, field.Attributes);
            }

            foreach (var property in type.GetProperties())
            {
                typeBuilder.DefineProperty(property.Name, property.Attributes, property.PropertyType,
                    property.GetIndexParameters().Select(p => p.ParameterType).ToArray());
            }

            return typeBuilder;
        }

        private static ExceptionHandler ExceptionHandler(ExceptionHandlingClause e)
        {
            return new ExceptionHandler(e.TryOffset, e.TryLength, e.FilterOffset, e.HandlerOffset, e.HandlerLength, e.Flags, e.CatchType?.MetadataToken ?? 0);
        }
    }
}

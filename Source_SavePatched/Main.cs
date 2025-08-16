using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Verse;
using XylDisassembler;

namespace XylSavePatched
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main(ModContentPack content) : Mod(content)
    {
        static Main()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("Output.DynamicAssembly"), AssemblyBuilderAccess.Save,
                GenFilePaths.DevOutputFolderPath);

            Dictionary<Module, ModuleBuilder> moduleBuilders = new();
            Dictionary<Type, TypeBuilder> typeBuilders = new();
            Dictionary<MethodInfo, MethodBuilder> methodBuilders = new();
            Dictionary<ConstructorInfo, ConstructorBuilder> constructorBuilders = new();

            foreach (var method in Harmony.GetAllPatchedMethods())
            {
                Log.Message($"{method.Name}: {method.GetType()}");

                if (method.Module.Assembly.GetName().Name != "Assembly-CSharp")
                    continue;

                var module = method.Module;
                if (!moduleBuilders.TryGetValue(module, out ModuleBuilder moduleBuilder))
                {
                    moduleBuilder = assemblyBuilder.DefineDynamicModule("Assembly-Output.dll");
                    moduleBuilders.Add(module, moduleBuilder);
                    //Log.Message($"moduleBuilder = {moduleBuilder}");
                }

                var type = method.DeclaringType;
                if (!typeBuilders.TryGetValue(type, out TypeBuilder typeBuilder))
                {
                    typeBuilder = MakeTypeBuilder(type, moduleBuilder);
                    typeBuilders.Add(type, typeBuilder);
                    //Log.Message($"typeBuilder = {typeBuilder}");

                    foreach (var typeMethod in GetAllMethods(type).Where(m => m.GetMethodBody() != null))
                    {
                            methodBuilders.Add(typeMethod, MakeMethodBuilder(typeMethod, typeBuilder));
                    }
                    foreach (var typeConstructor in GetAllConstructors(type).Where(m => m.GetMethodBody() != null))
                    {
                            constructorBuilders.Add(typeConstructor, MakeMethodBuilder(typeConstructor, typeBuilder));
                    }
                }

                if (method is MethodInfo methodInfo)
                {
                    MethodBuilder methodBuilder = methodBuilders[(MethodInfo)Harmony.GetOriginalMethod(methodInfo)];

                    MethodBase patchedMethod = GetPatchedMethod(method, methodBuilder.GetILGenerator());

                    try
                    {
                        CopyMethodBody(patchedMethod, methodBuilder, methodBuilder.GetILGenerator());
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"{type.Name}.{methodInfo.Name}: Exception in CopyMethodBody (patched): {e}");
                    }

                    //Log.Message($"methodBuilder = {methodBuilder}");
                }
                else
                {
                    Log.Warning($"Not a MethodInfo: {method.Name} {method.GetType()}");
                }
            }

            foreach (var methodBuilder in methodBuilders.Values)
            {
                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                if (ilGenerator.ILOffset == 0)
                    CreateDummyMethodBody(ilGenerator);
            }

            foreach (var constructorBuilder in constructorBuilders.Values)
            {
                ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
                if (ilGenerator.ILOffset == 0)
                    CreateDummyMethodBody(ilGenerator);
            }

            foreach ((Type type, TypeBuilder typeBuilder) in typeBuilders)
            {
                try
                {
                    typeBuilder.CreateType();
                    //Log.Message($"{typeBuilder.FullName}: IsCreated = {typeBuilder.IsCreated()}");
                }
                catch (Exception e)
                {
                    Log.Warning($"{type.Name}: Exception in CreateType: {e}");
                }
            }

            assemblyBuilder.Save("Assembly-Output.dll");
        }

        private static MethodBase GetPatchedMethod(MethodBase method, ILGenerator ilGenerator)
        {
            Patches patchInfo = Harmony.GetPatchInfo(method);

            List<MethodInfo> prefixes = PatchFunctions.GetSortedPatchMethods(method, patchInfo.Prefixes.ToArray(), false);
            List<MethodInfo> postfixes = PatchFunctions.GetSortedPatchMethods(method, patchInfo.Postfixes.ToArray(), false);
            List<MethodInfo> transpilers = PatchFunctions.GetSortedPatchMethods(method, patchInfo.Transpilers.ToArray(), false);
            List<MethodInfo> finalizers = PatchFunctions.GetSortedPatchMethods(method, patchInfo.Finalizers.ToArray(), false);

            //var patcher = new MethodPatcher(method, null, prefixes, postfixes, transpilers, finalizers, false);

            //MethodInfo patchedMethod = patcher.CreateReplacement(out Dictionary<int, CodeInstruction> instructions);

            

            //return patchedMethod;
            return method;
        }

        private static void CreateDummyMethodBody(ILGenerator ilGenerator)
        {
            ilGenerator.ThrowException(typeof(NotImplementedException));
        }

        private static MethodInfo[] GetAllMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
                                   BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }

        private static ConstructorInfo[] GetAllConstructors(Type type)
        {
            return type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | 
                                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }

        private static MethodBuilder MakeMethodBuilder(MethodInfo methodInfo, TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodInfo.Attributes,
                methodInfo.ReturnType, methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
            return methodBuilder;
        }

        private static ConstructorBuilder MakeMethodBuilder(ConstructorInfo methodInfo, TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineConstructor(methodInfo.Attributes, methodInfo.CallingConvention,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
            return methodBuilder;
        }

        private static void CopyMethodBody(MethodBase methodInfo, MethodBuilder builder, ILGenerator ilGenerator)
        {
            var methodBody = methodInfo.GetMethodBody();
            var module = methodInfo.Module;
            var decodedMethod = Disassembler.Decode(methodBody, module);

            if (methodBody == null)
                throw new NotSupportedException("Null MethodBody");

            foreach (var oldLocal in methodBody.LocalVariables)
            {
                var newLocal = ilGenerator.DeclareLocal(oldLocal.LocalType);
                if (newLocal.LocalIndex != oldLocal.LocalIndex)
                    throw new NotSupportedException($"Local index mismatch");
            }

            Dictionary<int, Label> labels = new();
            HashSet<Label> markedLabels = new();

            // Create labels for all instructions which are branch targets
            foreach (var oldInstruction in decodedMethod.Instructions)
            {
                if (IsBranch(oldInstruction.OpCode))
                {
                    var target = (int)oldInstruction.Value;
                    if (labels.ContainsKey(target))
                        continue;
                    labels.Add(target, ilGenerator.DefineLabel());
                }
                else if (oldInstruction.OpCode == OpCodes.Switch)
                {
                    foreach (int target in (int[])oldInstruction.Value)
                    {
                        if (labels.ContainsKey(target))
                            continue;
                        labels.Add(target, ilGenerator.DefineLabel());
                    }
                }
            }

            // Emit the instructions
            foreach (var oldInstruction in decodedMethod.Instructions)
            {
                //Log.Message($"{oldInstruction.ByteIndex} {oldInstruction.OpCode} {oldInstruction.Value}");
                if (labels.TryGetValue(oldInstruction.ByteIndex, out Label label))
                {
                    ilGenerator.MarkLabel(label);
                    markedLabels.Add(label);
                }

                if (IsBranch(oldInstruction.OpCode))
                {
                    ilGenerator.Emit(oldInstruction.OpCode, labels[(int)oldInstruction.Value]);
                }
                else if (oldInstruction.OpCode == OpCodes.Switch)
                {
                    ilGenerator.Emit(oldInstruction.OpCode, ((int[])oldInstruction.Value).Select(i => labels[i]).ToArray());
                }
                else
                {
                    switch (oldInstruction.Value)
                    {
                        case null: ilGenerator.Emit(oldInstruction.OpCode); break;
                        case byte value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case short value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case int value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case long value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case float value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case double value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case string value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case Type value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        case MethodInfo value:
                        {
                            //if (value.DeclaringType == methodInfo.DeclaringType)
                            //{
                            //    value = builder.DeclaringType.GetMethod(value.Name,
                            //        BindingFlags.Public | BindingFlags.NonPublic,
                            //        null,
                            //        value.GetParameters().Select(p => p.ParameterType).ToArray(),
                            //        null);
                            //}
                            ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        }
                        case ConstructorInfo value:
                        {
                            //if (value.DeclaringType == methodInfo.DeclaringType)
                            //{
                            //    value = builder.DeclaringType.GetConstructor(
                            //        BindingFlags.Public | BindingFlags.NonPublic,
                            //        null,
                            //        value.GetParameters().Select(p => p.ParameterType).ToArray(),
                            //        null);
                            //}
                            ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        }
                        case FieldInfo value:
                        {
                            //if (value.DeclaringType == methodInfo.DeclaringType)
                            //{
                            //    value = builder.DeclaringType.GetField(value.Name,
                            //        BindingFlags.Public | BindingFlags.NonPublic);
                            //}
                            ilGenerator.Emit(oldInstruction.OpCode, value); break;
                        }
                        default: throw new NotSupportedException($"Unhandled value type {oldInstruction.Value.GetType()}");
                    }
                }
            }

            foreach (var kvp in labels.Where(kvp => !markedLabels.Contains(kvp.Value)))
            {
                Log.Warning($"{methodInfo.Name}: missing label at byte index {kvp.Key}");
                ilGenerator.MarkLabel(kvp.Value);
            }
        }

        private static bool IsBranch(OpCode opCode)
        {
            return opCode.OperandType is (OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget);
        }

        private static TypeBuilder MakeTypeBuilder(Type type, ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(type.FullName, type.Attributes, type.BaseType);

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

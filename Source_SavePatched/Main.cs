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
            var harmony = new Harmony("net.pardeike.rimworld.lib.harmony");

            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("Output.DynamicAssembly"), AssemblyBuilderAccess.Save,
                GenFilePaths.DevOutputFolderPath);

            Dictionary<Module, ModuleBuilder> moduleBuilders = new();
            Dictionary<Type, TypeBuilder> typeBuilders = new();
            Dictionary<MethodInfo, MethodBuilder> methodBuilders = new();

            foreach (var method in harmony.GetPatchedMethods())
            {
                if (method.Module.Assembly.GetName().Name != "Assembly-CSharp")
                    continue;

                var module = method.Module;
                if (!moduleBuilders.TryGetValue(module, out ModuleBuilder moduleBuilder))
                {
                    moduleBuilder = MakeModuleBuilder(module, assemblyBuilder);
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
                        methodBuilders.Add(typeMethod, MakeMethodBuilder(typeMethod, typeBuilder));
                }

                if (method is MethodInfo methodInfo)
                {
                    var originalMethod = (MethodInfo)Harmony.GetOriginalMethod(methodInfo);
                    MethodBuilder methodBuilder = methodBuilders[originalMethod];

                    try
                    {
                        CopyMethodBody(methodInfo, methodBuilder.GetILGenerator());
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"{methodInfo.Name}: Exception in CopyMethodBody (patched): {e}");
                    }

                    Log.Message($"methodBuilder = {methodBuilder}");
                }
                else
                {
                    Log.Warning($"Not a MethodInfo: {method.Name} {method.GetType()}");
                }
            }

            foreach ((Type type, TypeBuilder typeBuilder) in typeBuilders)
            {
                foreach (var typeMethod in GetAllMethods(type))
                {
                    if (!methodBuilders.TryGetValue(typeMethod, out MethodBuilder methodBuilder)) 
                        continue;

                    if (methodBuilder.GetILGenerator().ILOffset == 0)
                    {
                        try
                        {
                            CopyMethodBody(typeMethod, methodBuilder.GetILGenerator());
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"{typeMethod.Name}: Exception in CopyMethodBody (original): {e}");
                        }
                    }
                }

                try
                {
                    typeBuilder.CreateType();
                }
                catch (Exception e)
                {
                    Log.Warning($"{type.Name}: Exception in CreateType: {e}");
                }
            }

            assemblyBuilder.Save("Assembly-Output.dll");
        }

        private static MethodInfo[] GetAllMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }

        private static MethodBuilder MakeMethodBuilder(MethodInfo methodInfo, TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodInfo.Attributes,
                methodInfo.ReturnType, methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
            return methodBuilder;
        }

        private static ModuleBuilder MakeModuleBuilder(Module module, AssemblyBuilder assemblyBuilder)
        {
            return assemblyBuilder.DefineDynamicModule(module.Name);
        }

        private static void CopyMethodBody(MethodInfo methodInfo, ILGenerator ilGenerator)
        {
            var decodedMethod = Disassembler.Decode(methodInfo);
            var methodBody = methodInfo.GetMethodBody();

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
                if (!IsBranch(oldInstruction.OpCode)) 
                    continue;
                var target = (int)oldInstruction.Value;
                if (labels.ContainsKey(target)) 
                    continue;
                labels.Add(target, ilGenerator.DefineLabel());
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
                else switch (oldInstruction.Value)
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
                    case MethodInfo value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                    case ConstructorInfo value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                    case FieldInfo value: ilGenerator.Emit(oldInstruction.OpCode, value); break;
                    default: throw new NotSupportedException($"Unhandled value type {oldInstruction.Value.GetType()}");
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

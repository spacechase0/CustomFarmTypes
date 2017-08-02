using Harmony;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CustomFarmTypes
{
    // It seems you can't have more than one patch on the same class :/
    internal class TypeFixes
    {
        /*
        ldsfld / ldarg / ...
        callvirt System.Object::GetType
        ldtoken StardewValley.Farm
        call System.Type::GetTypeFromHandle
        call System.Type::op_Equality

        =>

        isinst StardewValley.Farm
        */

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            // TODO: Learn how to use ILGenerator

            Log.trace("Running farm type fixer on function: " + original.DeclaringType + ": " + original);
            var newInsns = new List<CodeInstruction>();
            foreach (var insn in insns)
            {
                if (insn.opcode == OpCodes.Call &&
                    insn.operand is MethodInfo meth)
                {
                    if (meth.ReturnType == typeof(bool) &&
                        meth.DeclaringType == typeof(Type) &&
                        meth.Name == "op_Equality" &&
                        meth.GetParameters().Length == 2 &&
                        meth.GetParameters()[0].ParameterType == typeof(Type) &&
                        meth.GetParameters()[0].ParameterType == typeof(Type))
                    {
                        Log.trace("\tFound a type equality check: " + insn);
                        var prevInsn1 = newInsns[newInsns.Count - 1];
                        if (prevInsn1.opcode == OpCodes.Call &&
                            prevInsn1.operand is MethodInfo prevMeth1)
                        {
                            if (prevMeth1.ReturnType == typeof(Type) &&
                                prevMeth1.DeclaringType == typeof(Type) &&
                                prevMeth1.Name == "GetTypeFromHandle" &&
                                prevMeth1.GetParameters().Length == 1 &&
                                prevMeth1.GetParameters()[0].ParameterType == typeof(System.RuntimeTypeHandle))
                            {
                                Log.trace("\tFound a call to GetTypeFromHandle: " + prevInsn1);
                                var prevInsn2 = newInsns[newInsns.Count - 2];
                                if (prevInsn2.opcode == OpCodes.Ldtoken &&
                                    prevInsn2.operand.GetType().ToString() == "System.RuntimeType" &&
                                    prevInsn2.operand.ToString() == "StardewValley.Farm")
                                {
                                    Log.trace("\tFound loading the farm type: " + prevInsn2);
                                    var prevInsn3 = newInsns[newInsns.Count - 3];
                                    if (prevInsn3.opcode == OpCodes.Callvirt &&
                                        prevInsn3.operand is MethodInfo prevMeth3)
                                    {
                                        if (prevMeth3.ReturnType == typeof(Type) &&
                                            prevMeth3.DeclaringType == typeof(object) &&
                                            prevMeth3.Name == "GetType" &&
                                            prevMeth3.GetParameters().Length == 0)
                                        {
                                            Log.trace("\tFound getting the type of the object: " + prevInsn3);
                                            Log.trace("Found; fixing...");
                                            newInsns.RemoveRange(newInsns.Count - 3, 3); // The 4th instruction hasn't been added yet
                                            newInsns.Add(new CodeInstruction(OpCodes.Isinst, typeof(StardewValley.Farm)));
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                newInsns.Add(insn);
            }
            Log.trace("Done");

            return newInsns;
        }
    }

    [HarmonyPatch(typeof(Game1), "performTenMinuteClockUpdate")]
    internal class TypeFixes1
    {
        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            return TypeFixes.Transpiler(gen, original, insns);
        }
    }
    
    [HarmonyPatch(typeof(FarmAnimal), "updateWhenCurrentLocation")]
    internal class TypeFixes2
    {
        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            return TypeFixes.Transpiler(gen, original, insns);
        }
    }
    
    [HarmonyPatch(typeof(BlueprintsMenu), "receiveLeftClick")]
    internal class TypeFixes3
    {
        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            return TypeFixes.Transpiler(gen, original, insns);
        }
    }
    
    [HarmonyPatch(typeof(CataloguePage), "receiveLeftClick")]
    internal class TypeFixes4
    {
        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            return TypeFixes.Transpiler(gen, original, insns);
        }
    }
}

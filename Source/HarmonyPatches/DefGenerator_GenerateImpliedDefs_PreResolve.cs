using HarmonyLib;
using RimWorld;

namespace rimstocks.HarmonyPatches;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public class DefGenerator_GenerateImpliedDefs_PreResolve
{
    public static void Prefix()
    {
        Core.PatchDef();
    }
}
using HarmonyLib;
using Verse;

namespace rimstocks;

public class HarmonyPatch_core : Mod
{
    public HarmonyPatch_core(ModContentPack content) : base(content)
    {
        /*
        harmony.Patch(
            AccessTools.Method(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve"),
            null,
            new HarmonyMethod(typeof(rimstocks.harmonyPatch_core), nameof(rimstocks.harmonyPatch_core.DefGenerator_GenerateImpliedDefs_PreResolve))
        );
        */
        new Harmony("husko.warbonds.lite.1").PatchAll();
    }
    /*
    static public void DefGenerator_GenerateImpliedDefs_PreResolve()
    {
        rimstocks.Core.PatchDef();
    }
    */
}
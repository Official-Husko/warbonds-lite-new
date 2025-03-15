using HarmonyLib;
using Verse;

namespace rimstocks;

public class harmonyPatch_core : Mod
{
    public harmonyPatch_core(ModContentPack content) : base(content)
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
        rimstocks.Core.patchDef();
    }
    */
}
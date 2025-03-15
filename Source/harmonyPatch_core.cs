using HarmonyLib;
using Verse;

namespace rimstocks;

public class HarmonyPatch_core : Mod
{
    public HarmonyPatch_core(ModContentPack content) : base(content)
    {
        new Harmony("husko.warbonds.lite.1").PatchAll();
    }
}
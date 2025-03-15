using HarmonyLib;
using RimWorld;

namespace rimstocks.HarmonyPatches;

[HarmonyPatch(typeof(Tradeable), "InitPriceDataIfNeeded")]
internal class Tradeable_InitPriceDataIfNeeded
{
    [HarmonyPostfix]
    private static void Postfix(Tradeable __instance, ref float ___pricePlayerBuy, ref float ___pricePlayerSell)
    {
        if (__instance.ThingDef.tradeTags == null || !__instance.ThingDef.tradeTags.Contains("warbond"))
        {
            return;
        }

        ___pricePlayerBuy = __instance.ThingDef.BaseMarketValue;
        ___pricePlayerSell = __instance.ThingDef.BaseMarketValue * ModBase.sellPrice *
                             (__instance.AnyThing.HitPoints / (float)__instance.AnyThing.MaxHitPoints);
    }
}
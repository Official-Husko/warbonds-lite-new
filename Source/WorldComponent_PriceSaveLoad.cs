using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace rimstocks;

public class WorldComponent_PriceSaveLoad : WorldComponent
{
    public static WorldComponent_PriceSaveLoad staticInstance;
    public Dictionary<string, FactionData> ar_factionData = [];
    public Dictionary<string, FactionPriceData> factionToPriceData = [];
    public bool initialized;

    public WorldComponent_PriceSaveLoad(World world) : base(world)
    {
        staticInstance = this;
    }

    public static void SavePrice(FactionDef faction, float tick, float price)
    {
        staticInstance.GetFactionPriceDataFrom(faction).SavePrice(tick, price);
    }

    public static float LoadPrice(FactionDef faction, float tick)
    {
        return staticInstance.GetFactionPriceDataFrom(faction).LoadPrice(tick);
    }

    public static void SaveTrend(FactionDef faction, float tick, float price)
    {
        staticInstance.GetFactionPriceDataFrom(faction).SaveTrend(tick, price);
    }

    public static float LoadTrend(FactionDef faction, float tick)
    {
        return staticInstance.GetFactionPriceDataFrom(faction).LoadTrend(tick);
    }

    public FactionPriceData GetFactionPriceDataFrom(FactionDef f)
    {
        var Key = Util.FactionDefNameToKey(f.defName);
        if (factionToPriceData.TryGetValue(Key, out var from))
        {
            return from;
        }

        var fpdn = new FactionPriceData
        {
            defname = f.defName,
            label = f.label,
            color = f.colorSpectrum is { Count: > 0 } ? f.colorSpectrum[0] : Color.white
        };

        factionToPriceData.Add(Key, fpdn);
        return factionToPriceData[Key];
    }

    public FactionPriceData GetFactionPriceDataByKey(string key)
    {
        factionToPriceData.TryGetValue(key, out var value);
        return value;
    }

    public override void FinalizeInit()
    {
        if (!initialized)
        {
            initialized = true;
            float ticksNow = Core.AbsTickGame;
            foreach (var f in from f in DefDatabase<FactionDef>.AllDefs
                              where
                                  Core.IsWarbondFaction(f)
                              select f)
            {
                if (ModBase.Use_rimwar)
                {
                    SavePrice(f, ticksNow, Core.GetRimwarPriceByDef(f));
                }
                else if (f != null)
                {
                    SavePrice(f, ticksNow, Core.GetDefaultPrice(f));
                }
                else
                {
                    SavePrice(null, ticksNow, Rand.Range(200f, 6000f));
                }
            }
        }
        else
        {
            foreach (var f in Core.ar_faction)
            {
                var key = Util.FactionDefNameToKey(f.defName);
                if (!staticInstance.factionToPriceData.Keys.Contains(key))
                {
                    continue;
                }

                var rs = staticInstance.GetFactionPriceDataByKey(key);
                rs.defname = Util.KeyToFactionDefName(key);
            }
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref initialized, "initialized");
        Scribe_Collections.Look(ref factionToPriceData, "husko_FactionPriceData", LookMode.Value, LookMode.Deep);
        Scribe_Collections.Look(ref ar_factionData, "husko_FactionData", LookMode.Value, LookMode.Deep);
        if (ar_factionData != null)
        {
            return;
        }

        foreach (var f in Find.FactionManager.AllFactions)
        {
            var data = new FactionData();
            ar_factionData?.Add(f.GetUniqueLoadID(), data);
        }
    }
}
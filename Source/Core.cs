using System;
using System.Collections.Generic;
using System.Linq;
using RimWar.Planet;
using RimWorld;
using UnityEngine;
using Verse;

namespace rimstocks;

public class Core(Map map) : MapComponent(map)
{
    public enum GraphStyle
    {
        small,
        normal,
        big
    }

    public static readonly List<ThingDef> ar_warbondDef = [];
    public static readonly List<FactionDef> ar_faction = [];
    public static readonly List<GraphStyle> ar_graphStyle = [];

    public static readonly float basicPrice = 500f;
    private readonly float maxPrice = 10000f;

    private readonly float minPrice = 1f;


    public static int AbsTickGame => Find.TickManager.TicksGame + (GenDate.GameStartHourOfDay * GenDate.TicksPerHour);


    public static bool IsWarbondFaction(FactionDef f)
    {
        if (f.pawnGroupMakers == null ||
            f.hidden ||
            f.isPlayer)
        {
            return false;
        }

        if (!f.naturalEnemy &&
            !f.mustStartOneEnemy &&
            !f.permanentEnemy)
        {
            return true;
        }

        if (ModBase.useEnemyFaction)
        {
            if (!f.modContentPack.PackageId.Contains("ludeon"))
            {
                return true;
            }
        }

        if (!ModBase.useVanillaEnemyFaction)
        {
            return f.defName == "Pirate";
        }

        if (f.modContentPack.PackageId.Contains("ludeon"))
        {
            return true;
        }

        return f.defName == "Pirate";
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (map != Find.AnyPlayerHomeMap)
        {
            return;
        }

        if (AbsTickGame % GenDate.TicksPerDay == 0)
        {
            if (ModBase.Use_rimwar)
            {
                // RimWar
                try
                {
                    ((Action)(() =>
                    {
                        // Changes according to events
                        foreach (var gc in Find.World.gameConditionManager.ActiveConditions)
                        {
                            switch (gc.def.defName)
                            {
                                case "wl_warbond_rise":
                                    // Stock price surge
                                    ChangeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.8f);
                                    break;
                                case "wl_warbond_fall":
                                    // Stock price plunge
                                    ChangeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.2f);
                                    break;
                                case "wl_warbond_change":
                                    // Stock price fluctuations
                                    ChangeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.5f);
                                    break;
                            }
                        }

                        // Apply faction power to price now
                        foreach (var fd in ar_faction)
                        {
                            var price = GetRimwarPriceByDef(fd);
                            WorldComponent_PriceSaveLoad.SaveTrend(fd, AbsTickGame, price);
                            WorldComponent_PriceSaveLoad.SavePrice(fd, AbsTickGame, price);
                            ar_warbondDef[ar_faction.IndexOf(fd)].SetStatBaseValue(StatDefOf.MarketValue, price);
                        }
                    }))();
                }
                catch (TypeLoadException)
                {
                }
            }
            else
            {
                // common

                // Stock price fluctuations
                var tickGap = GenDate.TicksPerDay;


                for (var i = 0; i < ar_faction.Count; i++)
                {
                    var f = ar_faction[i];
                    var style = ar_graphStyle[i];
                    var prevTrend = WorldComponent_PriceSaveLoad.LoadTrend(f, AbsTickGame - tickGap);
                    var prevTrend2 = WorldComponent_PriceSaveLoad.LoadTrend(f, AbsTickGame - (tickGap * 2));

                    // Trend angle
                    float slope = style switch
                    {
                        GraphStyle.small => prevTrend / prevTrend2 * Rand.Range(0.85f, 1.15f),
                        GraphStyle.big => prevTrend / prevTrend2 * Rand.Range(0.995f, 1.005f),
                        _ => prevTrend / prevTrend2 * Rand.Range(0.96f, 1.04f)
                    };

                    // vibration    
                    var shake = 1f + Rand.Range(-0.05f, 0.05f);

                    // bounce off the upper and lower limits
                    /*
                        if ((prevTrend >= maxPrice && Rand.Chance(0.2f)) || (prevTrend <= minPrice && Rand.Chance(0.2f)))
                        {
                            slope += 1f / slope;
                        }
                        */
                    if (prevTrend <= minPrice && Rand.Chance(0.2f))
                    {
                        slope += 1f / slope;
                    }

                    // Bounce off the upper and lower limits with a low probability
                    slope = style switch
                    {
                        GraphStyle.small when Rand.Chance(0.15f) => 1f / slope,
                        GraphStyle.big when Rand.Chance(0.1f) => 1f / slope,
                        _ when Rand.Chance(0.12f) => 1f / slope,
                        _ => slope
                    };


                    // The higher the angle, the greater the probability of becoming gentler
                    switch (style)
                    {
                        case GraphStyle.small:
                            if (Rand.Chance(Mathf.Abs(slope - 1f) * 0.8f))
                            {
                                slope = 1f + ((slope - 1f) * Rand.Range(0.1f, 0.4f));
                            }

                            break;
                        default:
                            if (Rand.Chance(Mathf.Abs(slope - 1f) * 1.2f))
                            {
                                slope = 1f + ((slope - 1f) * Rand.Range(0.1f, 0.4f));
                            }

                            break;
                        case GraphStyle.big:
                            if (Rand.Chance(Mathf.Abs(slope - 1f) * 2.4f))
                            {
                                slope = 1f + ((slope - 1f) * Rand.Range(0.1f, 0.4f));
                            }

                            break;
                    }


                    // Changes according to events
                    var eventDir = 0;
                    foreach (var gc in Find.World.gameConditionManager.ActiveConditions)
                    {
                        switch (gc.def.defName)
                        {
                            case "rs_warbond_rise":
                                // Stock price surge
                                if (Rand.Chance(0.8f))
                                {
                                    eventDir = 1;
                                }
                                else
                                {
                                    eventDir = -1;
                                }

                                break;
                            case "rs_warbond_fall":
                                // Stock price plunge
                                if (Rand.Chance(0.2f))
                                {
                                    eventDir = 1;
                                }
                                else
                                {
                                    eventDir = -1;
                                }

                                break;
                            case "rs_warbond_change":
                                // Stock price fluctuations
                                if (Rand.Chance(0.5f))
                                {
                                    eventDir = 1;
                                }
                                else
                                {
                                    eventDir = -1;
                                }

                                break;
                        }
                    }

                    if (eventDir != 0)
                    {
                        slope = style switch
                        {
                            GraphStyle.small => 1f + (Rand.Range(0.1f, 0.5f) * eventDir),
                            GraphStyle.big => 1f + (Rand.Range(0.04f, 0.2f) * eventDir),
                            _ => 1f + (Rand.Range(0.07f, 0.35f) * eventDir),
                        };
                    }


                    // The higher the price, the greater the probability of becoming gentler
                    switch (style)
                    {
                        case GraphStyle.small:
                            if (slope > 1f && Rand.Chance(prevTrend / 700f * 0.2f))
                            {
                                slope = 1f / slope * Rand.Range(0.9f, 0.95f);
                            }

                            break;
                        default:
                            if (slope > 1f && Rand.Chance(Mathf.Max(0f, prevTrend - 2000f) / 2000f * 0.2f))
                            {
                                slope = 1f / slope * Rand.Range(0.9f, 0.95f);
                            }

                            break;
                        case GraphStyle.big:
                            if (slope > 1f && Rand.Chance(Mathf.Max(0f, prevTrend - 4000f) / 3500f * 0.2f))
                            {
                                slope = 1f / slope * Rand.Range(0.9f, 0.95f);
                            }

                            break;
                    }


                    // The lower the price, the greater the probability of becoming gentler
                    switch (style)
                    {
                        case GraphStyle.small:
                            if (slope < 1f && Rand.Chance(Mathf.Clamp((400f - prevTrend) / 400f * 0.02f, 0f, 1f)))
                            {
                                slope *= Rand.Range(1.05f, 1.1f);
                            }

                            break;
                        default:
                            if (slope < 1f && Rand.Chance(Mathf.Clamp((400f - prevTrend) / 400f * 0.015f, 0f, 1f)))
                            {
                                slope *= Rand.Range(1.05f, 1.1f);
                            }

                            break;
                        case GraphStyle.big:
                            if (slope < 1f && Rand.Chance(Mathf.Clamp((400f - prevTrend) / 400f * 0.015f, 0f, 1f)))
                            {
                                slope *= Rand.Range(1.05f, 1.1f);
                            }

                            break;
                    }


                    var newTrend = Mathf.Clamp(prevTrend * slope, minPrice, maxPrice);
                    var newPrice = Mathf.Clamp(newTrend * shake, minPrice, maxPrice);
                    ar_warbondDef[i].SetStatBaseValue(StatDefOf.MarketValue, newPrice);
                    WorldComponent_PriceSaveLoad.SaveTrend(f, AbsTickGame, newTrend);
                    WorldComponent_PriceSaveLoad.SavePrice(f, AbsTickGame, newPrice);


                    // Delisting
                    if (!(newPrice < ModBase.DelistingPrice))
                    {
                        continue;
                    }

                    WorldComponent_PriceSaveLoad.SaveTrend(f, AbsTickGame - GenDate.TicksPerDay,
                        GetDefaultPrice(f));
                    WorldComponent_PriceSaveLoad.SaveTrend(f, AbsTickGame, GetDefaultPrice(f));
                    WorldComponent_PriceSaveLoad.SavePrice(f, AbsTickGame, GetDefaultPrice(f));

                    if (Util.RemoveAllThingByDef(ar_warbondDef[i]))
                    {
                        Messages.Message(new Message(
                            "bond.delisting.destroy".Translate(ar_warbondDef[i].label, ModBase.DelistingPrice),
                            MessageTypeDefOf.ThreatSmall));
                    }
                    else
                    {
                        Messages.Message(new Message(
                            "bond.delisting".Translate(ar_warbondDef[i].label, ModBase.DelistingPrice),
                            MessageTypeDefOf.ThreatSmall));
                    }
                }
            }
        }


        // Tick ​​- Quarter
        if (Find.TickManager.TicksAbs % GenDate.TicksPerQuadrum != GenDate.TicksPerHour)
        {
            return;
        }

        // Pay dividends
        for (var i = 0; i < ar_faction.Count; i++)
        {
            Util.GiveDividend(ar_faction[i], ar_warbondDef[i]);
        }
    }


    public static void OnQuestResult(FactionDef f, FactionDef f2, bool success)
    {
        var targetTime = AbsTickGame;
        float changeScale;
        int index;
        float change;


        if (ModBase.Use_rimwar)
        {
            // RimWar
            try
            {
                ((Action)(() =>
                {
                    float price;
                    if (f != null)
                    {
                        index = ar_faction.IndexOf(f);
                        if (index >= 0)
                        {
                            price = 0;
                            foreach (var faction in Find.FactionManager.AllFactions)
                            {
                                if (faction.def != f)
                                {
                                    continue;
                                }

                                var data =
                                    WorldUtility.GetRimWarDataForFaction(faction);
                                if (data == null)
                                {
                                    continue;
                                }

                                change = 1f;
                                resetChangeScale();
                                changeScale *= Mathf.Min(1f,
                                    1500f * ModBase.rimwarPriceFactor / GetRimwarPriceByDef(f));
                                if (success)
                                {
                                    change = 1f + changeScale;
                                    Messages.Message(new Message(
                                        "bond.quest.up".Translate(ar_warbondDef[index].label,
                                            (changeScale * 100f).ToString("0.#")),
                                            MessageTypeDefOf.ThreatSmall));
                                }
                                else
                                {
                                    change = 1f - changeScale;
                                    Messages.Message(new Message(
                                        "bond.quest.down".Translate(ar_warbondDef[index].label,
                                            "-" + (changeScale * 100f).ToString("0.#")),
                                            MessageTypeDefOf.ThreatSmall));
                                }

                                foreach (var st in data.WarSettlementComps)
                                {
                                    st.RimWarPoints = Mathf.RoundToInt(st.RimWarPoints * change);
                                }

                                price += data.TotalFactionPoints;
                            }

                            price *= ModBase.rimwarPriceFactor;
                            WorldComponent_PriceSaveLoad.SaveTrend(f, AbsTickGame, price);
                            WorldComponent_PriceSaveLoad.SavePrice(f, AbsTickGame, price);
                            ar_warbondDef[ar_faction.IndexOf(f)].SetStatBaseValue(StatDefOf.MarketValue, price);
                        }
                    }

                    if (f2 == null)
                    {
                        return;
                    }

                    index = ar_faction.IndexOf(f2);
                    if (index < 0)
                    {
                        return;
                    }

                    price = 0;
                    foreach (var faction in Find.FactionManager.AllFactions)
                    {
                        if (faction.def != f2)
                        {
                            continue;
                        }

                        var data =
                            WorldUtility.GetRimWarDataForFaction(faction);
                        if (data == null)
                        {
                            continue;
                        }

                        change = 1f;
                        resetChangeScale();
                        changeScale *= Mathf.Min(1f,
                            1500f * ModBase.rimwarPriceFactor / GetRimwarPriceByDef(f));
                        if (!success)
                        {
                            change = 1f + changeScale;
                            Messages.Message(new Message(
                                "bond.quest.up".Translate(ar_warbondDef[index].label,
                                    (changeScale * 100f).ToString("0.#")),
                                    MessageTypeDefOf.ThreatSmall));
                        }
                        else
                        {
                            change = 1f - changeScale;
                            Messages.Message(new Message(
                                "bond.quest.down".Translate(ar_warbondDef[index].label,
                                    "-" + (changeScale * 100f).ToString("0.#")),
                                    MessageTypeDefOf.ThreatSmall));
                        }

                        foreach (var st in data.WarSettlementComps)
                        {
                            st.RimWarPoints = Mathf.RoundToInt(st.RimWarPoints * change);
                        }

                        price += data.TotalFactionPoints;
                    }

                    price *= ModBase.rimwarPriceFactor;
                    WorldComponent_PriceSaveLoad.SaveTrend(f2, AbsTickGame, price);
                    WorldComponent_PriceSaveLoad.SavePrice(f2, AbsTickGame, price);
                    ar_warbondDef[ar_faction.IndexOf(f2)].SetStatBaseValue(StatDefOf.MarketValue, price);
                }))();
            }
            catch (TypeLoadException)
            {
            }

            return;
        }

        // common
        float prev;
        if (f != null)
        {
            index = ar_faction.IndexOf(f);
            if (index >= 0)
            {
                prev = WorldComponent_PriceSaveLoad.LoadTrend(f, targetTime);
                resetChangeScale();
                changeScale *= Mathf.Min(1f, 1500f / prev);
                if (success)
                {
                    change = 1f + changeScale;
                    Messages.Message(new Message(
                        "bond.quest.up".Translate(ar_warbondDef[index].label, (changeScale * 100f).ToString("0.#")),
                        MessageTypeDefOf.ThreatSmall));
                }
                else
                {
                    change = 1f - changeScale;
                    Messages.Message(new Message(
                        "bond.quest.down".Translate(ar_warbondDef[index].label,
                            "-" + (changeScale * 100f).ToString("0.#")), MessageTypeDefOf.ThreatSmall));
                }

                WorldComponent_PriceSaveLoad.SaveTrend(f, targetTime, change * prev);
                prev = WorldComponent_PriceSaveLoad.LoadPrice(f, targetTime);
                WorldComponent_PriceSaveLoad.SavePrice(f, targetTime, change * prev);
                ar_warbondDef[index].SetStatBaseValue(StatDefOf.MarketValue, change * prev);
            }
        }


        if (f2 == null)
        {
            return;
        }

        index = ar_faction.IndexOf(f2);
        if (index < 0)
        {
            return;
        }

        prev = WorldComponent_PriceSaveLoad.LoadTrend(f2, targetTime);
        resetChangeScale();
        changeScale *= Mathf.Min(1f, 1500f / prev);
        if (!success)
        {
            change = 1f + changeScale;
            Messages.Message(new Message(
                "bond.quest.up".Translate(ar_warbondDef[index].label, (changeScale * 100f).ToString("0.#")),
                MessageTypeDefOf.ThreatSmall));
        }
        else
        {
            change = 1f - changeScale;
            Messages.Message(new Message(
                "bond.quest.down".Translate(ar_warbondDef[index].label,
                    "-" + (changeScale * 100f).ToString("0.#")), MessageTypeDefOf.ThreatSmall));
        }

        WorldComponent_PriceSaveLoad.SaveTrend(f2, targetTime, change * prev);
        prev = WorldComponent_PriceSaveLoad.LoadPrice(f2, targetTime);
        WorldComponent_PriceSaveLoad.SavePrice(f2, targetTime, change * prev);
        ar_warbondDef[index].SetStatBaseValue(StatDefOf.MarketValue, change * prev);
        return;

        void resetChangeScale()
        {
            changeScale = Rand.Range(0.10f, 0.25f);
        }
    }


    public static void PatchDef()
    {
        // Create a warbond item DEF
        foreach (var f in from f in DefDatabase<FactionDef>.AllDefs
                          where
                              IsWarbondFaction(f)
                          select f)
        {
            var t = new ThingDef
            {
                // base
                thingClass = typeof(ThingWithComps),
                category = ThingCategory.Item,
                resourceReadoutPriority = ResourceCountPriority.Middle,
                selectable = true,
                altitudeLayer = AltitudeLayer.Item,
                comps = [new CompProperties_Forbiddable()],
                alwaysHaulable = true,
                drawGUIOverlay = true,
                rotatable = false,
                pathCost = 14,
                // detail
                defName = $"oh_warbond_{f.defName}",
                label = string.Format("warbond_t".Translate(), f.label),
                description = string.Format("warbond_d".Translate(), f.label),
                graphicData = new GraphicData
                {
                    texPath = f.factionIconPath
                }
            };

            if (f.colorSpectrum is { Count: > 0 })
            {
                t.graphicData.color = f.colorSpectrum[0];
            }

            t.graphicData.graphicClass = typeof(Graphic_Single);
            t.soundInteract = SoundDef.Named("Silver_Drop");
            t.soundDrop = SoundDef.Named("Silver_Drop");

            t.healthAffectsPrice = true;
            t.statBases = [];


            t.useHitPoints = true;
            t.SetStatBaseValue(StatDefOf.MaxHitPoints, 30f);
            t.SetStatBaseValue(StatDefOf.Flammability, 1f);

            t.SetStatBaseValue(StatDefOf.MarketValue, basicPrice);
            t.SetStatBaseValue(StatDefOf.Mass, 0.008f);

            t.thingCategories = [];

            t.stackLimit = 999;

            t.burnableByRecipe = true;
            t.smeltable = false;
            t.terrainAffordanceNeeded = TerrainAffordanceDefOf.Medium;

            var thingCategoryDef = ThingCategoryDef.Named("warbond");
            if (thingCategoryDef != null)
            {
                t.thingCategories.Add(thingCategoryDef);
            }

            t.tradeability = Tradeability.All;

            t.tradeTags = ["warbond"];

            t.tickerType = TickerType.Rare;

            if (ModBase.limitDate > 0)
            {
                var cp_lifespan = new CompProperties_Lifespan
                {
                    lifespanTicks = GenDate.TicksPerDay
                };
                t.comps.Add(cp_lifespan);
            }


            // Register
            ar_warbondDef.Add(t);
            ar_faction.Add(f);
            switch (f.defName)
            {
                default:
                    if (f.modContentPack.PackageId.Contains("ludeon"))
                    {
                        ar_graphStyle.Add(!f.naturalEnemy ? GraphStyle.normal : GraphStyle.small);
                    }
                    else
                    {
                        switch (ar_graphStyle.Count % 4)
                        {
                            default:
                                ar_graphStyle.Add(GraphStyle.normal);
                                break;
                            case 0:
                                ar_graphStyle.Add(GraphStyle.big);
                                break;
                            case 2:
                                ar_graphStyle.Add(GraphStyle.small);
                                break;
                        }
                    }

                    break;
                case "Pirate":
                    ar_graphStyle.Add(GraphStyle.small);
                    break;
                case "Empire":
                    ar_graphStyle.Add(GraphStyle.big);
                    break;
            }

            DefGenerator.AddImpliedDef(t);
        }

        PatchIncident();
    }


    public static void PatchDef2()
    {
        foreach (var t in ar_warbondDef)
        {
            var cp = t.GetCompProperties<CompProperties_Lifespan>();
            if (cp == null)
            {
                continue;
            }

            cp.lifespanTicks = GenDate.TicksPerDay * ModBase.limitDate;
        }
    }

    public static void PatchIncident()
    {
        foreach (var i in from i in DefDatabase<IncidentDef>.AllDefs
                          where
                              i.defName.Contains("rs_warbond")
                          select i)
        {
            i.baseChance = 3f * ModBase.priceEvent_multiply;
        }
    }


    public static float GetDefaultPrice(FactionDef fd)
    {
        var index = ar_faction.IndexOf(fd);
        if (index < 0 || index >= ar_graphStyle.Count)
        {
            return Rand.Range(200f, 6000f);
        }

        var style = ar_graphStyle[index];
        return style switch
        {
            GraphStyle.small => Rand.Range(350f, 450f),
            GraphStyle.big => Rand.Range(4100f, 4500f),
            _ => Rand.Range(1750f, 2050f)
        };
    }

    public static void ChangeRimwarAllFactionPower(FloatRange changeScaleRange, float increasePer)
    {
        if (!ModBase.Use_rimwar)
        {
            return;
        }

        foreach (var f in Find.FactionManager.AllFactions)
        {
            var data = WorldUtility.GetRimWarDataForFaction(f);
            if (data == null)
            {
                continue;
            }

            var multiply = 1f;
            if (Rand.Chance(increasePer))
            {
                var nerfForTooMuchPowerful = Mathf.Min(1f, 1500f * ModBase.rimwarPriceFactor / GetRimwarPrice(f));
                multiply += Rand.Range(changeScaleRange.min, changeScaleRange.max) * nerfForTooMuchPowerful;
            }
            else
            {
                multiply -= Rand.Range(changeScaleRange.min, changeScaleRange.max);
            }

            foreach (var st in data.WarSettlementComps)
            {
                st.RimWarPoints = Mathf.RoundToInt(st.RimWarPoints * multiply);
            }
        }
    }

    public static float GetRimwarPriceByDef(FactionDef fd)
    {
        var price = -1f;
        if (!ModBase.Use_rimwar)
        {
            return price;
        }

        // RimWar
        try
        {
            ((Action)(() =>
            {
                price = 0;
                foreach (var f in Find.FactionManager.AllFactions)
                {
                    if (f.def == fd)
                    {
                        price += WorldUtility.GetRimWarDataForFaction(f) != null
                            ? WorldUtility.GetRimWarDataForFaction(f).TotalFactionPoints
                            : 0;
                    }
                }

                price *= ModBase.rimwarPriceFactor;
                price = Mathf.Max(1f, price);
            }))();
        }
        catch (TypeLoadException)
        {
        }

        return price;
    }

    public static float GetRimwarPrice(Faction f)
    {
        var price = -1f;
        if (!ModBase.Use_rimwar)
        {
            return price;
        }

        // RimWar
        try
        {
            ((Action)(() =>
            {
                price = WorldUtility.GetRimWarDataForFaction(f) != null
                    ? WorldUtility.GetRimWarDataForFaction(f).TotalFactionPoints
                    : 0;
                price *= ModBase.rimwarPriceFactor;
                price = Mathf.Max(1f, price);
            }))();
        }
        catch (TypeLoadException)
        {
        }

        return price;
    }
}
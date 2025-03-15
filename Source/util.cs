using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace rimstocks;

public static class Util
{
    private static List<Thing> rewards = [];


    // 특정 채권 모두제거 (상장폐지용)
    public static bool RemoveAllThingByDef(ThingDef def)
    {
        var removed = false;
        foreach (var m in from m in Find.Maps where m.ParentFaction == Find.FactionManager.OfPlayer select m)
        {
            var ar = GetAllWarbondInMap(m, def);
            for (var i = ar.Count - 1; i >= 0; i--)
            {
                ar[i].Kill();
                removed = true;
            }
        }

        foreach (var cv in from cv in Find.WorldObjects.Caravans where true select cv)
        {
            var ar = GetAllWarbondInCaravan(cv, def);
            for (var i = ar.Count - 1; i >= 0; i--)
            {
                ar[i].Kill();
                removed = true;
            }
        }


        return removed;
    }


    // 배당금 지급
    public static void GiveDividend(FactionDef f, ThingDef warbondDef)
    {
        foreach (var m in from m in Find.Maps where m.ParentFaction == Find.FactionManager.OfPlayer select m)
        {
            // 보상 가격 규모 설정
            var bondCount = AmountWarbondForDividend(m, warbondDef);
            var marketValue = bondCount * warbondDef.BaseMarketValue * ModBase.dividendPer;


            var intVec = DropCellFinder.TradeDropSpot(m);


            // 보상 물품 DEF 리스트 생성
            rewards.Clear();
            var ar_thingDef = new List<ThingDef>();
            if (!f.modContentPack?.IsOfficialMod == true)
            {
                // 모드 팩션
                foreach (var t in from t in DefDatabase<ThingDef>.AllDefs
                                  where
                                      basicThingCheck(t)
                                      && t.modContentPack is { PackageId: not null }
                                      && t.modContentPack.PackageId == f.modContentPack.PackageId
                                  select t)
                {
                    ar_thingDef.Add(t);
                }
            }

            if (ar_thingDef.Count == 0)
            {
                switch (f.techLevel)
                {
                    case >= TechLevel.Spacer:
                        {
                            foreach (var t in from t in DefDatabase<ThingDef>.AllDefs
                                              where
                                                  basicThingCheck(t)
                                                  && t.techLevel == TechLevel.Spacer
                                                  && t.modContentPack is { IsOfficialMod: true }
                                              select t)
                            {
                                ar_thingDef.Add(t);
                            }

                            break;
                        }
                    case >= TechLevel.Industrial:
                        {
                            foreach (var t in from t in DefDatabase<ThingDef>.AllDefs
                                              where
                                                  basicThingCheck(t)
                                                  && t.techLevel == TechLevel.Industrial
                                                  && t.modContentPack is { IsOfficialMod: true }
                                              select t)
                            {
                                ar_thingDef.Add(t);
                            }

                            break;
                        }
                    case >= TechLevel.Medieval:
                        {
                            foreach (var t in from t in DefDatabase<ThingDef>.AllDefs
                                              where
                                                  basicThingCheck(t) && t.techLevel == TechLevel.Medieval &&
                                                  t.modContentPack is { IsOfficialMod: true }
                                              select t)
                            {
                                ar_thingDef.Add(t);
                            }

                            break;
                        }
                    default:
                        {
                            foreach (var t in from t in DefDatabase<ThingDef>.AllDefs
                                              where
                                                  basicThingCheck(t) && t.techLevel == TechLevel.Neolithic &&
                                                  t.modContentPack is { IsOfficialMod: true }
                                              select t)
                            {
                                ar_thingDef.Add(t);
                            }

                            break;
                        }
                }

                if (ar_thingDef.Count == 0)
                {
                    foreach (var t in from t in DefDatabase<ThingDef>.AllDefs
                                      where
                                          basicThingCheck(t)
                                          && t.modContentPack is { IsOfficialMod: true }
                                      select t)
                    {
                        ar_thingDef.Add(t);
                    }
                }
            }


            if (ar_thingDef.Count == 0)
            {
                continue;
            }

            marketValue = Math.Min(marketValue, ModBase.maxReward);

            rewards = MakeThings(marketValue, ar_thingDef, f);

            if (rewards.Count == 0)
            {
                continue;
            }

            DropPodUtility.DropThingsNear(intVec, m, rewards, 110, false, false, false, false);

            rewards.Clear();


            Messages.Message(new Message("bond.dividendArrived".Translate(warbondDef.label, bondCount, marketValue),
                MessageTypeDefOf.NeutralEvent));
        }

        return;

        // 배당금 물품 DEF 기본 체크
        static bool basicThingCheck(ThingDef t)
        {
            return t.tradeability != Tradeability.None && t.race == null && !t.IsBuildingArtificial;
        }
    }

    private static List<Thing> MakeThings(float marketValue, List<ThingDef> ar_def, FactionDef f)
    {
        var ar_thing = new List<Thing>();
        Thing t;

        //ar_def.Shuffle();
        ar_def.SortByDescending(e => e.BaseMarketValue);
        //ar_tmp0.SortBy<DrugPolicyEntry, float>((Func<DrugPolicyEntry, float>)(e => (float)e.drug.techLevel + (e.drug.defName.Contains("fire") ? 0.1f : e.drug.defName.Contains("emp") ? 0.2f : 0f)));
        for (var i = 0; i < ar_def.Count && marketValue > 0; i++)
        {
            var needCount = Mathf.FloorToInt(marketValue / ar_def[i].BaseMarketValue);
            var count = Mathf.Min(ar_def[i].stackLimit, needCount);
            if (ar_def[i].MadeFromStuff)
            {
                while (needCount > 0)
                {
                    if (!GenStuff.TryRandomStuffFor(ar_def[i], out var stuff, f.techLevel))
                    {
                        continue;
                    }

                    t = ThingMaker.MakeThing(ar_def[i], stuff);

                    t.stackCount = count;
                    ar_thing.Add(t);
                    marketValue -= ar_def[i].BaseMarketValue * t.stackCount;

                    needCount -= count;
                }
            }
            else
            {
                while (needCount > 0)
                {
                    t = ThingMaker.MakeThing(ar_def[i]);

                    t.stackCount = count;
                    ar_thing.Add(t);
                    marketValue -= ar_def[i].BaseMarketValue * t.stackCount;

                    needCount -= count;
                }
            }
        }

        if (marketValue >= 1f)
        {
            t = ThingMaker.MakeThing(ThingDefOf.Silver);
            t.stackCount = Mathf.FloorToInt(marketValue);
            ar_thing.Add(t);
        }

        foreach (var t2 in ar_thing.ToList())
        {
            if (t2 == null)
            {
                ar_thing.Remove(null);
            }
            else if (t2.stackCount == 0)
            {
                ar_thing.Remove(t2);
            }
        }

        return ar_thing;
    }

    private static List<Thing> GetAllWarbondInCaravan(Caravan cv, ThingDef td)
    {
        var ar_thing = new List<Thing>();
        foreach (var t in from t in cv.AllThings
                          where
                              t.def == td
                          select t)
        {
            ar_thing.Add(t);
        }

        foreach (var p in cv.pawns)
        {
            if (p.inventory?.innerContainer == null)
            {
                continue;
            }

            foreach (var t2 in from t2 in p.inventory.innerContainer
                               where
                                   t2.def == td
                               select t2)
            {
                ar_thing.Add(t2);
            }
        }

        return ar_thing;
    }

    private static List<Thing> GetAllWarbondInMap(Map map, ThingDef td)
    {
        var ar_thing = new List<Thing>();
        foreach (var t in from t in map.listerThings.AllThings
                          where
                              t.def == td
                          select t)
        {
            ar_thing.Add(t);
        }

        foreach (var t in from t in map.listerThings.AllThings
                          where
                              t.TryGetComp<CompTransporter>() != null
                          select t)
        {
            var cp = t.TryGetComp<CompTransporter>();

            foreach (var t2 in from t2 in cp.innerContainer
                               where
                                   t2.def == td
                               select t2)
            {
                ar_thing.Add(t2);
            }
        }

        foreach (var p in map.mapPawns.FreeColonistsAndPrisoners)
        {
            if (p.inventory?.innerContainer == null)
            {
                continue;
            }

            foreach (var t2 in from t2 in p.inventory.innerContainer
                               where
                                   t2.def == td
                               select t2)
            {
                ar_thing.Add(t2);
            }
        }

        return ar_thing;
    }


    private static int AmountWarbondForDividend(Map map, ThingDef td)
    {
        return (from t in CaravanFormingUtility.AllReachableColonyItems(map)
                where t.def == td
                select t).Sum(t => t.stackCount);
    }

    private static int AmountSendableWarbond(Map map, ThingDef td)
    {
        return (from t in TradeUtility.AllLaunchableThingsForTrade(map)
                where t.def == td
                select t).Sum(t => t.stackCount);
    }

    // 통신기 메뉴 - 군사요청
    public static DiaOption RequestMilitaryAidOptionWarbond(Map map, Faction faction, Pawn negotiator)
    {
        var warbondDef = ThingDef.Named($"oh_warbond_{faction.def.defName}");

        string text = "warbond.requestMilitaryAid".Translate(ModBase.militaryAid_cost, warbondDef.label);

        if (AmountSendableWarbond(map, warbondDef) < ModBase.militaryAid_cost)
        {
            var diaOption = new DiaOption(text);
            diaOption.Disable("warbond.noCost".Translate());
            return diaOption;
        }

        /*
        if (faction.PlayerRelationKind == FactionRelationKind.Hostile)
        {
            DiaOption diaOption = new DiaOption(text);
            diaOption.Disable("warbond.mustBeNotHostile".Translate());
            return diaOption;
        }
        */
        if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp))
        {
            var diaOption2 = new DiaOption(text);
            diaOption2.Disable("BadTemperature".Translate());
            return diaOption2;
        }

        var num = faction.lastMilitaryAidRequestTick + 60000 - Find.TickManager.TicksGame;
        if (num > 0)
        {
            var diaOption3 = new DiaOption(text);
            diaOption3.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
            return diaOption3;
        }

        if (faction.PlayerRelationKind != FactionRelationKind.Hostile &&
            NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
        {
            var diaOption4 = new DiaOption(text);
            diaOption4.Disable("HostileVisitorsPresent".Translate());
            return diaOption4;
        }

        var diaOption5 = new DiaOption(text);
        if ((int)faction.def.techLevel < 4)
        {
            diaOption5.link = FactionDialogMaker.CantMakeItInTime(faction, negotiator);
        }
        else
        {
            var source = (from x in map.attackTargetsCache.TargetsHostileToColony
                          where GenHostility.IsActiveThreatToPlayer(x)
                          select ((Thing)x).Faction
                into x
                          where x != null && !x.HostileTo(faction)
                          select x).Distinct();
            if (source.Any())
            {
                var diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name,
                    source.Select(fa => fa.Name).ToCommaList(true)));
                var diaOption6 = new DiaOption("CallConfirm".Translate())
                {
                    action = delegate { CallForAidWarBond(map, faction, warbondDef); },
                    link = FactionDialogMaker.FightersSent(faction, negotiator)
                };
                var diaOption7 = new DiaOption("CallCancel".Translate())
                {
                    linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator)
                };
                diaNode.options.Add(diaOption6);
                diaNode.options.Add(diaOption7);
                diaOption5.link = diaNode;
            }
            else
            {
                diaOption5.action = delegate { CallForAidWarBond(map, faction, warbondDef); };
                diaOption5.link = FactionDialogMaker.FightersSent(faction, negotiator);
            }
        }

        return diaOption5;
    }

    private static void CallForAidWarBond(Map map, Faction faction, ThingDef warbondDef)
    {
        TradeUtility.LaunchThingsOfType(warbondDef, ModBase.militaryAid_cost, map, null);
        var incidentParms = new IncidentParms
        {
            target = map,
            faction = faction,
            raidArrivalModeForQuickMilitaryAid = true,
            points = warbondDef.BaseMarketValue * ModBase.militaryAid_multiply * 2f
        };

        if (faction.PlayerRelationKind == FactionRelationKind.Hostile)
        {
            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
        }

        faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
        IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
    }

    public static string FactionDefNameToKey(string defname)
    {
        return $"warbondPrice_{defname}";
    }

    public static string KeyToFactionDefName(string key)
    {
        return key.Substring(key.IndexOf('_') + 1, key.Length - key.IndexOf('_') - 1);
    }
}
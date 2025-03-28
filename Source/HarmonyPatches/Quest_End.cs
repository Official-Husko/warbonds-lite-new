﻿using System.Linq;
using HarmonyLib;
using RimWorld;

namespace rimstocks.HarmonyPatches;

// Stock price fluctuations when a quest is completed
[HarmonyPatch(typeof(Quest), nameof(Quest.End))]
internal class Quest_End
{
    private static void Postfix(Quest __instance, QuestEndOutcome outcome)
    {
        FactionDef f = null;
        FactionDef f2 = null;
        if (__instance.InvolvedFactions != null && __instance.InvolvedFactions.Count() >= 2)
        {
            f = __instance.InvolvedFactions.ToList()[0].def;
            f2 = __instance.InvolvedFactions.ToList()[1].def;
        }
        else if (__instance.InvolvedFactions != null && __instance.InvolvedFactions.Count() == 1)
        {
            foreach (var p in __instance.PartsListForReading)
            {
                switch (p)
                {
                    case QuestPart_SpawnWorldObject o:
                        {
                            if (o.worldObject.def == WorldObjectDefOf.Site && o.worldObject.Faction != null)
                            {
                                f2 = o.worldObject.Faction.def;
                            }

                            break;
                        }
                    case QuestPart_Incident p2:
                        {
                            if (p2.incident == IncidentDefOf.RaidEnemy)
                            {
                                f2 = __instance.InvolvedFactions.ToList()[0].def;
                            }

                            break;
                        }
                    default:
                        {
                            if (p is not QuestPart_InvolvedFactions)
                            {
                                if (p.InvolvedFactions.Any())
                                {
                                    f = p.InvolvedFactions.ToList()[0].def;
                                }
                            }

                            break;
                        }
                }
            }
        }

        switch (outcome)
        {
            case QuestEndOutcome.Fail:
                Core.OnQuestResult(f, f2, false);
                break;
            case QuestEndOutcome.Success:
                Core.OnQuestResult(f, f2, true);
                break;
            case QuestEndOutcome.InvalidPreAcceptance:
                Core.OnQuestResult(f, f2, true);
                break;
            case QuestEndOutcome.Unknown:
                Core.OnQuestResult(f, f2, true);
                break;
        }
    }
}

// Add options when contacting a faction

// Merge when bonds expire

// Fixed bond purchase/sale price
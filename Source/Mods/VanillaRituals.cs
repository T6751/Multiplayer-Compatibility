using System.Collections.Generic;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>Vanilla Rituals desync fix</summary>
    /// Fixes desync caused by LordToil_Ritual.UpdateAllDuties() creating jobs with GetNextJobID()
    /// during ritual start, which happens in a synced context but job creation is not synced.
    /// Also fixes desync caused by CompRitualEffect_IntervalSpawnCircle.SpawnPos using RNG without synchronization.
    /// Also fixes desync caused by ThinkNode_PrioritySorter.TryIssueJobPackage using RNG for job priority sorting.
    /// Also fixes desync caused by RitualVisualEffectComp.SpawnFleck using RNG without synchronization.
    /// Also fixes desync caused by HediffGiver_RandomAgeCurved.OnIntervalPassed using RNG without synchronization.
    internal class VanillaRituals
    {
        private static readonly HashSet<LordToil_Ritual> pendingUpdates = new HashSet<LordToil_Ritual>();

        public VanillaRituals()
        {
            MpCompatPatchLoader.LoadPatch(this);

            // Fix RNG desync in ritual visual effects
            PatchingUtilities.PatchPushPopRand("RimWorld.CompRitualEffect_IntervalSpawnCircle:SpawnPos");

            // Fix RNG desync in job priority sorting
            // ThinkNode_PrioritySorter uses Rand.Range to randomly sort jobs with equal priority,
            // which causes desyncs as different clients get different random values
            PatchingUtilities.PatchPushPopRand("Verse.AI.ThinkNode_PrioritySorter:TryIssueJobPackage");

            // Fix RNG desync in ritual visual effect fleck spawning
            // RitualVisualEffectComp.SpawnFleck uses FloatRange.get_RandomInRange() which calls Rand.Range
            // without synchronization, causing desyncs when different clients get different random values
            PatchingUtilities.PatchPushPopRand("RimWorld.RitualVisualEffectComp:SpawnFleck");

            // Fix RNG desync in random age-based hediff events
            // HediffGiver_RandomAgeCurved.OnIntervalPassed uses Rand.MTBEventOccurs without synchronization,
            // causing desyncs when different clients get different random values for health events
            PatchingUtilities.PatchPushPopRand("Verse.HediffGiver_RandomAgeCurved:OnIntervalPassed");
        }

        [MpCompatPrefix("RimWorld.LordToil_Ritual", nameof(LordToil_Ritual.UpdateAllDuties))]
        private static bool Prefix_UpdateAllDuties(LordToil_Ritual __instance)
        {
            if (!MP.IsInMultiplayer)
                return true;

            // If we're in a synced context (during RitualSession.Start), defer the update
            // to the next tick to avoid desyncs from GetNextJobID() calls
            if (MP.IsExecutingSyncCommand)
            {
                pendingUpdates.Add(__instance);
                return false;
            }

            return true;
        }

        [MpCompatPostfix("Verse.TickManager", nameof(TickManager.TickManagerUpdate))]
        private static void Postfix_TickManagerUpdate()
        {
            if (!MP.IsInMultiplayer || pendingUpdates.Count == 0)
                return;

            // Process pending updates on the next tick
            var toUpdate = new List<LordToil_Ritual>(pendingUpdates);
            pendingUpdates.Clear();

            foreach (var toil in toUpdate)
            {
                if (toil?.lord != null)
                {
                    toil.UpdateAllDuties();
                }
            }
        }
    }
}
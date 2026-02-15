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
    internal class VanillaRituals
    {
        private static readonly HashSet<LordToil_Ritual> pendingUpdates = new HashSet<LordToil_Ritual>();

        public VanillaRituals()
        {
            MpCompatPatchLoader.LoadPatch(this);
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
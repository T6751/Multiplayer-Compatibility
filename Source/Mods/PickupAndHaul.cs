using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>Pick Up And Haul by Mehni</summary>
    /// <see href="https://github.com/Mehni/PickUpAndHaul/"/>
    /// <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=1279012058"/>
    [MpCompatFor("Mehni.PickUpAndHaul")]
    public class PickupAndHaul
    {
        public PickupAndHaul(ModContentPack mod)
        {
            // Sorts the ListerHaulables list from UI, causes issues
            MpCompat.harmony.Patch(AccessTools.Method("PickUpAndHaul.WorkGiver_HaulToInventory:PotentialWorkThingsGlobal"),
                transpiler: new HarmonyMethod(typeof(PickupAndHaul), nameof(Transpiler)));

            // Fix desync: CheckIfPawnShouldUnloadInventory creates jobs with GetNextJobID() in unsynced context
            // The original patch used PatchCancelInInterface, but the issue is that the method is called
            // from simulation during pawn tick, not from interface. We need to prevent job creation
            // when called from interface (to avoid UI issues) but allow it in simulation.
            // However, the real issue is that GetNextJobID() is called during pawn tick which should be synced.
            // The problem might be timing-related, so we keep the original patch for interface cancellation.
            PatchingUtilities.PatchCancelInInterface("PickUpAndHaul.HarmonyPatches:DropUnusedInventory_PostFix");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var target = AccessTools.Method(typeof(ListerHaulables), nameof(ListerHaulables.ThingsPotentiallyNeedingHauling));
            var newListCtor = AccessTools.Constructor(typeof(List<Thing>), new[] { typeof(IEnumerable<Thing>) });

            var patched = false;
            foreach (var ci in instr)
            {
                yield return ci;

                if (ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo method && method == target)
                {
                    yield return new CodeInstruction(OpCodes.Newobj, newListCtor);
                    patched = true;
                }
            }

            if (!patched)
                throw new Exception("Failed patching Pickup and Haul");
        }
    }
}
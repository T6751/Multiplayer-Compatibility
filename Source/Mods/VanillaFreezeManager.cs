using Verse;

namespace Multiplayer.Compat
{
    /// <summary>Vanilla FreezeManager desync fix</summary>
    /// Fixes desync caused by FreezeManager.DoIceMelting using Rand.Chance without RNG synchronization.
    internal class VanillaFreezeManager
    {
        public VanillaFreezeManager()
        {
            // Patch DoIceMelting to use synchronized RNG
            PatchingUtilities.PatchPushPopRand("Verse.FreezeManager:DoIceMelting");
        }
    }
}
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ShovelFix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const int SHOVEL_OF_DIRT = 5;
        public const int SHOVEL_OF_SAND = 704;
        public const int SHOVEL_OF_MUD = 705;
        public const int SHOVEL_OF_RED_SAND = 706;

        private static readonly int[] mappedIds = [SHOVEL_OF_DIRT, SHOVEL_OF_SAND, SHOVEL_OF_MUD, SHOVEL_OF_RED_SAND];

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).Patch(
                original: AccessTools.Method(typeof(Inventory), nameof(Inventory.useItemWithFuel)),
                transpiler: new(typeof(Plugin), nameof(UseItemWithFuel_Transpiler))
            );

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static IEnumerable<CodeInstruction> UseItemWithFuel_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher matcher = new(instructions, generator);

            matcher.End().CreateLabel(out var l);

            List<CodeInstruction> insert = [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, AccessTools.Method(typeof(Plugin), nameof(ShouldAvoidBreak))),
                new(OpCodes.Brtrue_S, l)
            ];

            matcher.Start().MatchEndForward([
                new(OpCodes.Ldelem_Ref),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(InventoryItem), nameof(InventoryItem.fuelMax))),
                new(OpCodes.Call),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(InventorySlot), nameof(InventorySlot.updateSlotContentsAndRefresh))),
                new(OpCodes.Ldarg_0)
            ]).InsertAndAdvance(insert);

            return matcher.Instructions();
        }

        private static bool ShouldAvoidBreak(Inventory inv)
        {
            var item = inv.invSlots[inv.selectedSlot];
            foreach (var id in mappedIds)
                if (id == item.itemNo)
                    return true;
            Log.LogDebug($"Item: {item.itemNo} - {item.itemInSlot.name}");
            return false;
        }
    }
}

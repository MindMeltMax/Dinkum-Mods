using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PeacefulHot
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const int LavaSpoutId = 36;
        public const int LavaSlimeId = 37;
        public const int YobbolinId = 40;
        public const int YobbolinHealerId = 41;

        public static readonly int[] MappedIds = [LavaSlimeId, LavaSpoutId, YobbolinId, YobbolinHealerId];

        private static ConfigEntry<bool> Enabled;
        private static ConfigEntry<bool> Always;
        private static ConfigEntry<ProvocationStrategy> Provocation;

        internal static ManualLogSource Log;

        private static WishManager? WishManager => NetworkMapSharer.Instance?.wishManager;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("Settings", "Enabled", true, "Whether or not the mod is enabled");
            Always = Config.Bind("Settings", "Always", false, "Whether or not creatures will be peaceful even without peaceful wish active");
            Provocation = Config.Bind("Settings", "Provocation", ProvocationStrategy.Default, "How creatures handle being attacked");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

            harmony.Patch(
                original: AccessTools.Method("AnimalAI_Attack:returnClosestPrey"),
                prefix: new(typeof(Plugin), nameof(AnimalAI_Attack_ReturnClosestPrey_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method("AnimalAI:damageTaken"),
                prefix: new(typeof(Plugin), nameof(AnimalAI_DamageTaken_Prefix)),
                postfix: new(typeof(Plugin), nameof(AnimalAI_DamageTaken_Postfix))
            );

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            Log.LogDebug($"Loaded with configuration: Enabled={(Enabled.Value ? "True" : "False")}, Always={(Always.Value ? "True" : "False")}");
        }

        private static bool AnimalAI_Attack_ReturnClosestPrey_Prefix(AnimalAI_Attack __instance, AnimalAI ___myAi, ref Transform __result)
        {
            if (!ShouldRun() || !In(___myAi.animalId, MappedIds))
                return true;

            __result = null;
            return false;
        }

        private static void AnimalAI_DamageTaken_Prefix(AnimalAI __instance)
        {
            //Not sure if this affects anything, but beter safe than sorry I guess
            if (!ShouldRun() || !In(__instance.animalId, MappedIds) || Provocation.Value != ProvocationStrategy.Run)
                return;
            __instance.isSkiddish = true;
        }

        private static void AnimalAI_DamageTaken_Postfix(AnimalAI __instance, Transform attackedBy, ref Transform ___currentlyRunningFrom, AnimalAI_Attack ___attacks)
        {
            if (!ShouldRun() || !In(__instance.animalId, MappedIds) || Provocation.Value == ProvocationStrategy.Default)
                return;

            if (Provocation.Value == ProvocationStrategy.Run)
            {
                __instance.isSkiddish = true;
                ___currentlyRunningFrom = attackedBy;
                __instance.myAgent.SetDestination(__instance.getSureRunPos(attackedBy.root));
            }
            ___attacks.setNewCurrentlyAttacking(null);
        }

        private static bool In(int id, IEnumerable<int> ids)
        {
            foreach (var item in ids)
                if (item == id)
                    return true;
            return false;
        }

        private static bool ShouldRun() => Enabled.Value && ((WishManager?.IsWishActive(WishManager.WishType.PeacefulWish) ?? false) || Always.Value);

        public enum ProvocationStrategy
        {
            Default,
            Ignore,
            Run
        }
    }
}

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace LavaWalker
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const int LavaTileId = 881;

        private static ConfigEntry<bool> Enabled;
        private static ConfigEntry<bool> OnFire;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("Settings", "Enabled", true, "Whether or not the mod is enabled");
            OnFire = Config.Bind("Settings", "OnFire", false, "Whether or not to still set the player on fire when stepping on lava");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

            harmony.Patch(
                original: AccessTools.Method("CharStatusEffects:CmdSetOnFire"),
                prefix: new(typeof(Plugin), nameof(CharStatusEffects_CmdSetOnFire_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method("CharStatusEffects:CmdTakeDamageFromGround"),
                prefix: new(typeof(Plugin), nameof(CharStatusEffects_CmdTakeDamageFromGround_Prefix))
            );

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            Log.LogDebug($"Loaded with configuration: Enabled={(Enabled.Value ? "True" : "False")}, OnFire={(OnFire.Value ? "True" : "False")}");
        }

        private static bool CharStatusEffects_CmdSetOnFire_Prefix(CharStatusEffects __instance)
        {
            if (!Enabled.Value || !CheckPositionIsLava(__instance))
                return true;

            return OnFire.Value;
        }

        private static bool CharStatusEffects_CmdTakeDamageFromGround_Prefix(CharStatusEffects __instance)
        {
            if (!Enabled.Value || !CheckPositionIsLava(__instance))
                return true;

            return false;
        }

        private static bool CheckPositionIsLava(Component c)
        {
            if (!WorldManager.Instance.isPositionOnMap(c.transform.position)) 
                return false;

            int x = Mathf.RoundToInt(c.transform.position.x / 2f);
            int z = Mathf.RoundToInt(c.transform.position.z / 2f);

            if (WorldManager.Instance.onTileMap[x, z] != LavaTileId)
                return false;
            return true;
        }
    }
}

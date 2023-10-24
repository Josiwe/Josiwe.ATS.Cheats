using BepInEx;
using Eremite;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Services;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace Josiwe.ATS.Cheats
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private Harmony harmony;        

        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));  
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        // Enables infinite cornerstone rerolls
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GetRerollsLeft))]
        [HarmonyPrefix]
        public static bool GetRerollsLeft_PrePatch(ref int __result)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || cheatConfig.EnableInfiniteCornerstoneRerolls == false) { return true; }

            __result = 99;
            return false;
        }

        // Generates extra cornerstone picks per season
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GenerateRewardsFor))]
        [HarmonyPrefix]
        public static bool GenerateRewardsFor_PrePatch(CornerstonesService __instance, SeasonRewardModel model, string viewConfiguration, bool isExtra)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null) { return true; }

            WriteLog($"Generating extra cornerstone picks for season change");

            List<RewardPickState> thisPicks = (List<RewardPickState>)AccessTools.Property(typeof(CornerstonesService), "Picks").GetValue(__instance);
            MethodInfo thisCreatePick = typeof(CornerstonesService).GetMethod("CreatePick", BindingFlags.NonPublic | BindingFlags.Instance);

            // create only the extras (i starts at 1, not 0)
            for (int i = 1; i < cheatConfig.CornerstonePicksPerSeason; i++)
            {
                RewardPickState rps = (RewardPickState)thisCreatePick.Invoke(__instance, new object[] { model, new List<EffectModel>(), viewConfiguration, true });
                thisPicks.Add(rps);
            }

            return true; // now run the original method
        }

        // Replaces initial 3 building picks at the start of a game with 3 wildcard picks
        [HarmonyPatch(typeof(ReputationRewardsService), nameof(ReputationRewardsService.PrepareInitialPoints))]
        [HarmonyPrefix]
        private static bool PrepareInitialPoints_PrePatch(ReputationRewardsService __instance)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || cheatConfig.EnableWildcardBlueprints == false) { return true; }

            if (__instance.State.initialReputationPicksGranted == false)
            {
                __instance.State.initialReputationPicksGranted = true;

                WriteLog("Generating initial 3 wildcard blueprint picks");
                Serviceable.EffectsService.GrantWildcardPick(3);
            }

            // test code
            // Serviceable.ReputationService.AddReputationPoints(0.99f, ReputationChangeSource.Other);

            return false; // do not run the original game method
        }

        // Replaces normal reputation rewards (blueprint picks) with wildcard blueprint picks
        [HarmonyPatch(typeof(ReputationRewardsService), nameof(ReputationRewardsService.UpdateRegularReputationReward))]
        [HarmonyPrefix]
        private static bool UpdateRegularReputationReward_PrePatch(ReputationRewardsService __instance, int points)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || cheatConfig.EnableWildcardBlueprints == false) { return true; }

            // copied from original function
            if (__instance.State.lastGrantedReputationReward >= points)
            {
                return false; // do not run original function
            }

            // for each regular reward we would normally collect, increment the granted tracker            
            int regularRewardsToCollect = __instance.CountRegularRewardsToCollect();
            __instance.State.lastGrantedReputationReward += regularRewardsToCollect;

            // now grant a wildcard pick instead
            for (int i = 0; i < regularRewardsToCollect; i++)
            {
                WriteLog($"Generating new wildcard blueprint pick for reputation threshold {points}");
                Serviceable.EffectsService.GrantWildcardPick(1);
            }

            // complete the rest of the original function
            __instance.UpdateWaitingRewards();
            if (__instance.State.currentPick == null)
            {
                __instance.GenerateNewPick();
            }

            return false; // do not run original function
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void HookMainControllerSetup()
        { 
            // This method will run after game load (Roughly on entering the main menu)
            // At this point a lot of the game's data will be available.
            // Your main entry point to access this data will be `Serviceable.Settings` or `MainController.Instance.Settings`
            Instance.Logger.LogInfo($"Performing game initialization on behalf of {PluginInfo.PLUGIN_GUID}.");
            Instance.Logger.LogInfo($"The game has loaded {MainController.Instance.Settings.effects.Length} effects.");
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart()
        {
            // Too difficult to predict when GameController will exist and I can hook observers to it
            // So just use Harmony and save us all some time. This method will run after every game start
            var isNewGame = MB.GameSaveService.IsNewGame();
            WriteLog($"Entered a game. Is this a new game: {isNewGame}.");
        }

        static CheatConfig GetCheatConfig()
        {
            CheatConfig cheatConfig = null;

            string basePath = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\Josiwe.ATS.Cheats.Config.json";
           // WriteLog($"loading cheat config using basePath {basePath}");

            // Tries to load the cheat config from json
            if (File.Exists(basePath))
            {
                try
                {
                    string json = File.ReadAllText(basePath);
                    cheatConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<CheatConfig>(File.ReadAllText(basePath));                    
                }
                catch { }
            }
            else
            {
                WriteLog("Cheat config file not found");
            }

            return cheatConfig;
        }

        static void WriteLog(string message)
        {
            Instance.Logger.LogInfo("Josiwe.ATS.Cheats:: " + message);
        }
    }
}

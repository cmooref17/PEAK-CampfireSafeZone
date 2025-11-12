using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PEAK_CampfireSafeZone
{
    [HarmonyPatch]
    public static class RunManagerPatcher
    {
        public static float timeSinceRunStarted { get { return RunManager.Instance != null ? RunManager.Instance.timeSinceRunStarted : 0; } }
        internal static float timePaused = 0;
        public static List<Campfire> campfires = new List<Campfire>();

        [HarmonyPatch(typeof(RunManager), "StartRun")]
        [HarmonyPostfix]
        public static void RunManagerStartRun()
        {
            timePaused = 0;
            CharacterPatcher.playersNearCampfire.Clear();
            CharacterPatcher.allPlayersNearCampfire = false;
        }


        [HarmonyPatch(typeof(RunManager), "Update")]
        [HarmonyPrefix]
        public static bool RunManagerUpdate()
        {
            bool pauseTime = CharacterPatcher.AllPlayersInRangeCampfire();
            if (pauseTime)
            {
                timePaused += Time.deltaTime;
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(EndScreen), "GetTimeString")]
        [HarmonyPrefix]
        public static void AdjustRunTimeString(ref float totalSeconds)
        {
            float compareSeconds = 0;
            try { compareSeconds = Character.localCharacter.refs.stats.timelineInfo[Character.localCharacter.refs.stats.timelineInfo.Count - 1].time - Character.localCharacter.refs.stats.timelineInfo[0].time; }
            catch(Exception e)
            {
                Plugin.LogError("Error: " + e);
                return;
            }
            Plugin.LogWarning("TotalSeconds: " + totalSeconds + " CompareSeconds: " + compareSeconds + " Delta: " + (compareSeconds - totalSeconds) + " TimePaused: " + timePaused);
            if (Mathf.Abs(totalSeconds - compareSeconds) < 0.1f)
                totalSeconds = Mathf.Max(0, totalSeconds - timePaused);
        }


        [HarmonyPatch(typeof(Campfire), "Awake")]
        [HarmonyPostfix]
        public static void CampfireAwake(Campfire __instance)
        {
            if (__instance.name.ToLower().StartsWith("campfire") && !campfires.Contains(__instance))
                campfires.Add(__instance);
        }
    }
}

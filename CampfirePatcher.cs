using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using static CharacterAfflictions;

namespace PEAK_AfkCampfire
{
    [HarmonyPatch]
    public static class AfkCampfire
    {
        internal static PhotonView fogView;
        public static List<Campfire> campfires = new List<Campfire>();
        public static HashSet<Character> playersNearFire = new HashSet<Character>();


        [HarmonyPatch(typeof(Campfire), "Awake")]
        [HarmonyPrefix]
        public static void CampfireAwake(Campfire __instance)
        {
            campfires.Add(__instance);
        }


        [HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
        [HarmonyPrefix]
        public static bool StopHungerDrainNearCampfire(STATUSTYPE statusType, float amount, CharacterAfflictions __instance)
        {
            if (statusType == STATUSTYPE.Hunger && !__instance.character.isBot && !__instance.character.data.dead && IsPlayerInRangeCampfire(__instance.character))
                return false;
            return true;
        }


        [HarmonyPatch(typeof(Character), "UseStamina")]
        [HarmonyPrefix]
        public static bool StopStaminaDrainNearCampfire(float usage, Character __instance)
        {
            if (!__instance.isBot && !__instance.data.dead && __instance.data.isSprinting && IsPlayerInRangeCampfire(__instance))
                return false;
            return true;
        }


        [HarmonyPatch(typeof(Fog), "Start")]
        [HarmonyPrefix]
        public static void FogStart(Fog __instance)
        {
            fogView = __instance.GetComponent<PhotonView>();
        }


        [HarmonyPatch(typeof(Fog), "Movement")]
        [HarmonyPrefix]
        public static bool StopFogMovementAndTimer(Fog __instance)
        {
            if (!fogView.IsMine)
                return true;

            bool atLeastOnePlayerAlive = false;
            var allCharacters = PlayerHandler.GetAllPlayerCharacters();
            for (int i = 0; i < allCharacters.Count; i++)
            {
                if (allCharacters[i].isBot)
                    continue;
                var character = allCharacters[i];
                atLeastOnePlayerAlive = atLeastOnePlayerAlive || !character.data.dead;
                if (!character.isBot && !character.data.dead && !IsPlayerInRangeCampfire(character))
                    return true;
            }
            return !atLeastOnePlayerAlive; // Only stop if a player is alive
        }


        /*[HarmonyPatch(typeof(Fog), "Move")]
        [HarmonyPrefix]
        public static bool StopFogMovement(Fog __instance)
        {
            if (!fogView.IsMine)
                return true;

            bool atLeastOnePlayerAlive = false;
            var allCharacters = PlayerHandler.GetAllPlayerCharacters();
            for (int i = 0; i < allCharacters.Count; i++)
            {
                if (allCharacters[i].isBot)
                    continue;
                var character = allCharacters[i];
                atLeastOnePlayerAlive = atLeastOnePlayerAlive || !character.data.dead;
                if (!character.isBot && !character.data.dead && !IsPlayerInRangeCampfire(character))
                    return true;
            }
            return !atLeastOnePlayerAlive; // Only stop if a player is alive
        }*/


        public static bool IsPlayerInRangeCampfire(Character character)
        {
            for (int i = 0; i < campfires.Count; i++)
            {
                var campfire = campfires[i];
                if (campfire == null)
                {
                    campfires.RemoveAt(i--);
                    continue;
                }
                if (Vector3.Distance(character.Center, campfire.transform.position) < 15)
                {
                    if (!playersNearFire.Contains(character))
                        Plugin.Log("Player: (" + character.characterName + ") ENTERED campfire proximity.");
                    playersNearFire.Add(character);
                    return true;
                }
            }
            if (playersNearFire.Contains(character))
                Plugin.Log("Player: (" + character.characterName + ") LEFT campfire proximity.");
            playersNearFire.Remove(character);
            return false;
        }
    }
}

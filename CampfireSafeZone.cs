using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using static CharacterAfflictions;

namespace PEAK_CampfireSafeZone
{
    [HarmonyPatch]
    public static class CampfireSafeZone
    {
        public static List<Campfire> campfires = new List<Campfire>();
        public static HashSet<Character> playersNearCampfire = new HashSet<Character>();
        public static bool allPlayersNearCampfire = false;


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


        [HarmonyPatch(typeof(Fog), "Movement")]
        [HarmonyPatch(typeof(OrbFogHandler), "Move")]
        [HarmonyPatch(typeof(OrbFogHandler), "WaitToMove")]
        [HarmonyPrefix]
        public static bool StopFogMovementAndTimer()
        {
            bool atLeastOnePlayerAlive = false;
            var allCharacters = PlayerHandler.GetAllPlayerCharacters();
            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
                atLeastOnePlayerAlive = atLeastOnePlayerAlive || !character.data.dead;
                if (!character.isBot && !character.data.dead && !IsPlayerInRangeCampfire(character))
                {
                    if (allPlayersNearCampfire)
                        Plugin.Log("Not all players are near campfire. Fog progression resumed.");
                    allPlayersNearCampfire = false;
                    return true;
                }
            }
            if (atLeastOnePlayerAlive)
            {
                if (!allPlayersNearCampfire)
                    Plugin.Log("All players are near campfire. Fog progression halted.");
                allPlayersNearCampfire = true;
                return false;
            }
            allPlayersNearCampfire = false;
            return true;
        }


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
                    if (!playersNearCampfire.Contains(character))
                        Plugin.Log("Player: (" + character.characterName + ") ENTERED campfire proximity.");
                    playersNearCampfire.Add(character);
                    return true;
                }
            }
            if (playersNearCampfire.Contains(character))
                Plugin.Log("Player: (" + character.characterName + ") LEFT campfire proximity.");
            playersNearCampfire.Remove(character);
            return false;
        }
    }
}

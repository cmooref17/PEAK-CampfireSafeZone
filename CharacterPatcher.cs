using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using static CharacterAfflictions;

namespace PEAK_CampfireSafeZone
{
    [HarmonyPatch]
    public static class CharacterPatcher
    {
        public static HashSet<Character> playersNearCampfire = new HashSet<Character>();
        public static bool allPlayersNearCampfire = false;

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


        [HarmonyPatch(typeof(Fog), "Movement")] // May be unused, or will be used in the future?
        [HarmonyPatch(typeof(OrbFogHandler), "Move")]
        [HarmonyPatch(typeof(OrbFogHandler), "WaitToMove")]
        [HarmonyPrefix]
        public static bool StopFogMovementAndTimer()
        {
            bool stopFog = AllPlayersInRangeCampfire();
            return stopFog;
        }


        public static bool IsPlayerInRangeCampfire(Character character)
        {
            Vector3 characterPosition;
            try { characterPosition = character.Center; }
            catch { return false; }

            for (int i = 0; i < RunManagerPatcher.campfires.Count; i++)
            {
                Campfire campfire = RunManagerPatcher.campfires[i];
                if (!campfire)
                {
                    RunManagerPatcher.campfires.RemoveAt(i--);
                    continue;
                }

                if (Vector3.Distance(characterPosition, campfire.transform.position) < 15)
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


        public static bool AllPlayersInRangeCampfire()
        {
            bool _allPlayersNearCampfire = true;
            bool atLeastOnePlayerAlive = false;
            foreach (var character in PlayerHandler.GetAllPlayerCharacters())
            {
                atLeastOnePlayerAlive = atLeastOnePlayerAlive || !character.data.dead;
                if (!character.isBot && !character.data.dead && !IsPlayerInRangeCampfire(character))
                {
                    _allPlayersNearCampfire = false;
                    break;
                }
            }

            if (!atLeastOnePlayerAlive)
                _allPlayersNearCampfire = false;

            if (_allPlayersNearCampfire != allPlayersNearCampfire)
            {
                allPlayersNearCampfire = _allPlayersNearCampfire;
                Plugin.Log(allPlayersNearCampfire ? "All players are near campfire. Fog progression and run timer halted." : "Not all players are near campfire. Fog progression and run timer resumed.");
            }

            return allPlayersNearCampfire;
        }
    }
}

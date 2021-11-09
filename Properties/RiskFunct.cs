using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;
using VRC.Core;
using SIDictionary = System.Collections.Generic.Dictionary<string, int>;

namespace DBMod
{
    class RiskFunct
    {//I borrowed from https://github.com/Adnezz/VoiceFalloffOverride/blob/f1e6d300b0997e139e0bb616f32f8a9f7752f350/Utilities.cs#L42
        //Borrowed parts from https://github.com/loukylor/VRC-Mods/blob/main/VRChatUtilityKit/Utilities/VRCUtils.cs
        //And also https://github.com/Psychloor/PlayerRotater/blob/master/PlayerRotater/Utilities.cs

        private static bool alreadyCheckingWorld;
        private static SIDictionary checkedWorlds = new SIDictionary();
        //0: Unblocked
        //1: Club World
        //2: Game World
        //3: Emm Website Blacklisted, Mod Disabled
        //4: Emm GameObject Blacklisted, Mod Disabled
        //10: Not checked yet.
        public static string WorldType()
        {
            switch (NDB.WorldType)
            {
                case 0: return "World Allowed";
                case 1: return "Club World";
                case 2: return "Game World";
                case 3: return "EmmVRC DB Blacklisted";
                case 4: return "GameObject Blacklisted";
                case 10: return "Not checked yet - Error?";
                default: MelonLoader.MelonLogger.Error($"Something Broke - WorldType Switch - {NDB.WorldType}"); return "Error";
            }
        }

        internal static System.Collections.IEnumerator CheckWorld()
        {
            if (alreadyCheckingWorld)
            {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }

            // Wait for RoomManager to exist before continuing.
            ApiWorld currentWorld = null;
            while (currentWorld == null)
            {
                currentWorld = RoomManager.field_Internal_Static_ApiWorld_0;
                yield return new WaitForSecondsRealtime(1);
            }
            var worldId = currentWorld.id;
            //MelonLogger.Msg($"Checking World with Id {worldId}");

            // Check cache for world, so we keep the number of API calls lower.
            //if (checkedWorlds.ContainsKey(worldId))
            if (checkedWorlds.TryGetValue(worldId, out int outres))
            {
                //checkedWorlds.TryGetValue(worldId, out int outres);
                NDB.WorldType = outres;
                //checkedWorlds[worldId];
                //MelonLogger.Msg($"Using cached check {Main.WorldType} for world '{worldId}'");
                yield break;
            }

            // Check for Game Objects first, as it's the lowest cost check.
            if (GameObject.Find("eVRCRiskFuncEnable") != null || GameObject.Find("UniversalRiskyFuncEnable") != null || GameObject.Find("ModCompatRiskyFuncEnable ") != null)
            {
                NDB.WorldType = 0;
                checkedWorlds.Add(worldId, 0);
                yield break;
            }
            else if (GameObject.Find("eVRCRiskFuncDisable") != null || GameObject.Find("UniversalRiskyFuncDisable") != null || GameObject.Find("ModCompatRiskyFuncDisable ") != null)
            {
                NDB.WorldType = 4;
                checkedWorlds.Add(worldId, 4);
                yield break;
            }

            alreadyCheckingWorld = true;
            // Check if black/whitelisted from EmmVRC - thanks Emilia and the rest of EmmVRC Staff
            var uwr = UnityWebRequest.Get($"https://dl.emmvrc.com/riskyfuncs.php?worldid={worldId}");
            uwr.SendWebRequest();
            while (!uwr.isDone)
                yield return new WaitForEndOfFrame();

            var result = uwr.downloadHandler.text?.Trim().ToLower();
            uwr.Dispose();
            if (!string.IsNullOrWhiteSpace(result))
            {
                switch (result)
                {
                    case "allowed":
                        NDB.WorldType = 0;
                        checkedWorlds.Add(worldId, 0);
                        alreadyCheckingWorld = false;
                        //MelonLogger.Msg($"EmmVRC allows world '{worldId}'");
                        yield break;

                    case "denied":
                        NDB.WorldType = 3;
                        checkedWorlds.Add(worldId, 3);
                        alreadyCheckingWorld = false;
                        //MelonLogger.Msg($"EmmVRC denies world '{worldId}'");
                        yield break;
                }
            }

            // No result from server or they're currently down
            // Check tags then. should also be in cache as it just got downloaded
            API.Fetch<ApiWorld>(
                worldId,
                new Action<ApiContainer>(
                    container =>
                    {
                        ApiWorld apiWorld;
                        if ((apiWorld = container.Model.TryCast<ApiWorld>()) != null)
                        {
                            short tagResult = 0;
                            foreach (var worldTag in apiWorld.tags)
                            {
                                if (worldTag.IndexOf("game", StringComparison.OrdinalIgnoreCase) != -1 && worldTag.IndexOf("games", StringComparison.OrdinalIgnoreCase) == -1)
                                {
                                    tagResult = 2;
                                    //MelonLogger.Msg($"Found game tag in world world '{worldId}'");
                                    break;
                                }
                                else if (worldTag.IndexOf("club", StringComparison.OrdinalIgnoreCase) != -1)
                                    tagResult = 1;
                            }
                            NDB.WorldType = tagResult;
                            checkedWorlds.Add(worldId, tagResult);
                            alreadyCheckingWorld = false;
                            //MelonLogger.Msg($"Tag search result: '{tagResult}' for '{worldId}'");
                        }
                        else
                        {
                            MelonLogger.Error("Failed to cast ApiModel to ApiWorld");
                        }
                    }),
                disableCache: false);

        }

    }
}

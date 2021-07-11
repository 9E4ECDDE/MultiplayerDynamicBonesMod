using ExternalDynamicBoneEditor.IPCSupport;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using ConsoleColor = System.ConsoleColor;
using IntPtr = System.IntPtr;
using UIExpansionKit.API;
using System.Reflection.Emit;

namespace DBMod
{
    internal class NDB : MelonMod
    {
        public NDB()
        { LoaderIntegrityCheck.CheckIntegrity(); }

        public const string VERSION_STR = "1041.5";

        private static class NDBConfig
        {
            public static float distanceToDisable;
            public static float colliderSizeLimit;
            public static int dynamicBoneUpdateRate;
            public static bool dynamicBoneUpdateRateAdjSettings;
            public static bool distanceDisable;
            public static bool enabledByDefault;
            public static bool disallowInsideColliders;
            public static bool destroyInsideColliders;
            public static bool onlyForMyBones;
            public static bool onlyForMeAndFriends;
            public static bool disallowDesktoppers;
            public static bool enableBoundsCheck;
            public static float visiblityUpdateRate;
            public static bool onlyHandColliders;
            public static bool keybindsEnabled;
            public static bool onlyOptimize;
            public static int updateMode;
            //public static bool hasShownCompatibilityIssueMessage;
            //public static HashSet<string> avatarsToWhichNotApply;
            public static Dictionary<string, bool> avatarsToWhichNotApply;
            public static bool enableEditor;
            public static bool breastsOnly;
            //public static bool enableUserPanelButton;
            //public static int userPanelButtonX;
            //public static int userPanelButtonY;
            public static HashSet<string> bonesToExclude;
            public static HashSet<string> collidersToExclude;
            public static HashSet<string> bonesToAlwaysExclude;
            public static bool excludeSpecificBones;
            public static bool interactSelf;
            public static bool othersInteractSelf;
            public static int logLevel;
            public static int debugLog;
            public static Dictionary<string, int> avatarsToAdjustDBRadius;
            public static Dictionary<string, bool> avatarsToAddColliders;
            public static int boneRadiusDivisor;
            public static float endBoneRadius;
            public static bool addAutoCollidersAll;
            public static bool adjustRadiusExcludeZero;
            public static bool adjustRadiusForAllZeroBones;
            public static bool disableAllBones;
            public static bool moarBones;
            public static bool moarBonesPrefLimit;
            public static bool moarBonesNotLocal;
            public static HashSet<string> bonesToInclude;
            public static HashSet<string> collidersToInclude;
            public static bool includeSpecificBones;
        }


        struct OriginalBoneInformation
        {
            public float updateRate;
            public float distanceToDisable;
            public List<DynamicBoneCollider> colliders;
            public DynamicBone referenceToOriginal;
            public bool distantDisable;
            public float Elasticity;
            public float Stiffness;
            public float Damping;
            public float Inert;
            public float Radius;
            public bool Enabled;
        }

        private static NDB _Instance;

        public Dictionary<string, System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>>> avatarsInScene;
        private Dictionary<string, List<OriginalBoneInformation>> originalSettings;
        public Dictionary<string, System.Tuple<Renderer, DynamicBone[]>> avatarRenderers;
        private GameObject localPlayer;
        private Dictionary<string, DynamicBone> localPlayerDBbyRootName;
        private Transform toggleButton;
        private bool enabled = true;

        private float nextUpdateVisibility = 0;
        private float nextEditorUpdate = 0;
        private const float visiblityUpdateRate = 1f;

        private (MethodBase, MethodBase) reloadDynamicBoneParamInternalFuncs;

        //private static AvatarInstantiatedDelegate onAvatarInstantiatedDelegate;
        //private static HarmonyInstance harmonyInstance;
        private static MethodInfo onJoinedRoomPatch;
        private static MethodInfo onAvatarInstantiatedPatch;
        public static bool HookLIC = false;

        public static HashSet<string> bonesExcluded = new HashSet<string>();
        public static HashSet<string> collidersExcluded = new HashSet<string>();
        public static HashSet<string> bonesIncluded = new HashSet<string>();
        public static HashSet<string> collidersIncluded = new HashSet<string>();
        public static Dictionary<string, Transform> specificButtonList = new Dictionary<string, Transform>();
        public static Dictionary<string, Transform> otherAvatarButtonList = new Dictionary<string, Transform>();

        public string AvatarsToWhichNotApplyPath = "UserData/MDB/AvatarsToWhichNotApply.txt";
        public string BonesToExcludePath = "UserData/MDB/BonesToExclude.txt";
        public string CollidersToExcludePath = "UserData/MDB/CollidersToExclude.txt";
        public string BonesToAlwaysExcludePath = "UserData/MDB/BonesToAlwaysExclude.txt";
        public string AvatarsToAdjustDBRadiusPath = "UserData/MDB/AvatarsToAdjustDBRadius.txt";
        public string AvatarsToAddCollidersPath = "UserData/MDB/AvatarsToAddColliders.txt";
        public string BonesToIncludePath = "UserData/MDB/BonesToInclude.txt";
        public string CollidersToIncludePath = "UserData/MDB/CollidersToInclude.txt";

        public static int moarbonesCount;
        public bool firstrun = true;

        public static string ExtraLogPath;
        //private static void Hook(IntPtr target, IntPtr detour)
        //{
        //    Imports.Hook(target, detour);
        //}


        //private delegate void AvatarInstantiatedDelegate(IntPtr @this, IntPtr avatarPtr, IntPtr avatarDescriptorPtr, bool loaded);



        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        static extern int MessageBox(IntPtr nWnd, string text, string title, uint type);

        public unsafe override void OnApplicationStart()
        {
            _Instance = this;

            RegisterModPrefs();

            InitFileLists();

            OnPreferencesSaved();

            firstrun = false;

            if (NDBConfig.updateMode == 2)
            {
                new Thread(new ThreadStart(CheckForUpdates)).Start();
            }
            else LogDebugInt(1, ConsoleColor.DarkGreen, $"Not checking for updates");

            if (DateTime.Today.Month == 4 && DateTime.Today.Day == 1)
            {
                new Thread(new ThreadStart(MoarBoneCheck)).Start();
            }


            enabled = NDBConfig.enabledByDefault;



            //HookCallbackFunctions();

            //to forcefully disable the limit
            PlayerPrefs.SetInt("VRC_LIMIT_DYNAMIC_BONE_USAGE", 0);

            MethodBase[] methods = XrefScanner.XrefScan(typeof(DynamicBone).GetMethod("OnValidate"))
                .Where(r => r.Type == XrefType.Method)
                .Select(xref => xref.TryResolve())
                .Where(m => m != null)
                .OrderBy(m => m.GetMethodBody().GetILAsByteArray().Length).ToArray();
            reloadDynamicBoneParamInternalFuncs = (methods[0], methods[1]);

            //Obsolete?
            //if (!NDBConfig.hasShownCompatibilityIssueMessage && MelonHandler.Mods.Any(m => m.Info.Name.ToLowerInvariant().Contains("emmvrc")))
            //{
            //    MessageBox(IntPtr.Zero, "Looks like you are using the 'emmVRC' mod. Please disable all emmVRC dynamic bones functionality in emmVRC settings to avoid compatibility issues with Multiplayer Dynamic Bones.\nThis is a onetime notification, you will not be prompted again.", "Multiplayer Dynamic Bones mod", 0x40 | 0x1000 | 0x010000);
            //    MelonPreferences.SetEntryValue<bool>("NDB", "HasShownCompatibilityIssueMessage", true);
            //}

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("MDB Avatar Config", () => AvatarMenu(QuickMenu.prop_QuickMenu_0.field_Private_Player_0, false));
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.AvatarMenu).AddSimpleButton("MDB Avatar Config", () => AvatarMenu(localPlayer.transform.root.GetComponentInChildren<VRC.Player>(), true));

            if (MelonPreferences.GetEntryValue<int>("NDB", "QuickMenuButton") == 0) //Quick Menu Button - 1:Settings Menu(default), 2:Just Toggle, 0:Off
            {
                LogDebugInt(0, ConsoleColor.White, $"Quick Menu button is disabled - 'QuickMenuButton' pref is set to 0");
            }
            else if (MelonPreferences.GetEntryValue<int>("NDB", "QuickMenuButton") == 2)
            {
                ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton($"Press to {((enabled) ? "disable" : "enable")} Dynamic Bones mod", () => ToggleState(), (button) => toggleButton = button.transform);
            }
            else ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("MDB Settings", () => SettingsMenuMain());
        }

        private void AvatarMenu(Player selectedPlayer, bool useBigMenu)
        {

            string avatarName = selectedPlayer.prop_ApiAvatar_0.name;
            string aviID = selectedPlayer.prop_VRCPlayer_0.prop_VRCAvatarManager_0.prop_ApiAvatar_0.id;
            string avatarHash = avatarName.Substring(0, Math.Min(avatarName.Length, 20)) + ":" + String.Format("{0:X}", aviID.GetHashCode()).Substring(4);
            NDB.otherAvatarButtonList.Clear();

            ICustomShowableLayoutedMenu otherAvatarMenu = null;
            otherAvatarMenu = useBigMenu ? ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList) : ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu1Column11Row);

            otherAvatarMenu.AddSimpleButton("MDB Avatar Config     -     Close", () => otherAvatarMenu.Hide());
            otherAvatarMenu.AddLabel($"Include/Exclude a Specific Avatar");
            otherAvatarMenu.AddLabel($"Excluded avatars wont be multiplayered\nIncluded avatars will bypass filtering, ie: 'Only Friends'");
            string AvatarExcludeText() // True - Exluded, False - Included 
            {
                return $"Avatar is currently: {(NDBConfig.avatarsToWhichNotApply.ContainsKey(avatarHash) ? (NDBConfig.avatarsToWhichNotApply[avatarHash] ? "Excluded from MDB" : "Included & Bypassing filters") : "N/A")}";
            }
            otherAvatarMenu.AddSimpleButton(AvatarExcludeText(), () =>
            {
                if (NDBConfig.avatarsToWhichNotApply.ContainsKey(avatarHash))
                {
                    if (NDBConfig.avatarsToWhichNotApply[avatarHash]) // If enabled
                    {
                        NDBConfig.avatarsToWhichNotApply[avatarHash] = false; //Change to Disable
                    }
                    else NDBConfig.avatarsToWhichNotApply.Remove(avatarHash); //Change to N/A
                }
                else NDBConfig.avatarsToWhichNotApply.Add(avatarHash, true); //Change to Enable

                NDB.otherAvatarButtonList["SpecificAvatar"].GetComponentInChildren<Text>().text = AvatarExcludeText();
                if (enabled) { RestoreOriginalColliderList(); AddAllCollidersToAllPlayers(); }
                //SaveListFiles();
            }, (button) => NDB.otherAvatarButtonList["SpecificAvatar"] = button.transform);


            otherAvatarMenu.AddLabel($"Include/Exclude Specific Bones or Colliders\nExlude:{(NDBConfig.excludeSpecificBones ? "Enabled" : "DISABLED")}, Include:{(NDBConfig.includeSpecificBones ? "Enabled" : "DISABLED")} <-- In Mod Settings");
            otherAvatarMenu.AddLabel($"Excluded objects wont be multiplayered\nIncluded will bypass filtering, ie: 'Only Hand Colliders'");
            otherAvatarMenu.AddSimpleButton("Bones", (() => ShowBoneSpecificMenu(selectedPlayer, useBigMenu)));
            otherAvatarMenu.AddSimpleButton("Colliders", (() => ShowColliderSpecificMenu(selectedPlayer, useBigMenu)));
            //
            otherAvatarMenu.AddLabel(CurrentText(), (button) => NDB.otherAvatarButtonList["Current"] = button.transform);
            string CurrentText()
            {
                return $"Adjust avatar's DB radius based on a Multiplier or replace entirely - Currently: {(NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash) ? (NDBConfig.avatarsToAdjustDBRadius[avatarHash] == -2 ? "Excluded" : (NDBConfig.avatarsToAdjustDBRadius[avatarHash] == 0 ? $"Replacing" : $"Multiplied by {(float)NDBConfig.avatarsToAdjustDBRadius[avatarHash] / 10f}")) : "N/A")}";
            }

            otherAvatarMenu.AddSimpleButton($"Multiply/Replace/Zero Radius Exclude - Menu", (() =>
            {
                otherAvatarMenu.Hide();
                MultiReplaceRadiusMenu(selectedPlayer, useBigMenu, avatarName, aviID, avatarHash);
            }));


            otherAvatarMenu.AddLabel($"Auto add hand colliders to this avatar\n{(NDBConfig.addAutoCollidersAll ? "-Currently ENABLED for all avatars in Mod Settings-" : "-This can be enabled for all avatars in Mod Settings-")}");

            string HandCollidersText()
            {
                return $"Hand Colliders: {(NDBConfig.avatarsToAddColliders.ContainsKey(avatarHash) ? (NDBConfig.avatarsToAddColliders[avatarHash] ? "Enabled for this Avatar" : "Disabled for this Avatar") : "N/A")}";
            }

            otherAvatarMenu.AddSimpleButton(HandCollidersText(), () =>
            {
                if (NDBConfig.avatarsToAddColliders.ContainsKey(avatarHash))
                {
                    if (NDBConfig.avatarsToAddColliders[avatarHash]) // If enabled
                    {
                        NDBConfig.avatarsToAddColliders[avatarHash] = false; //Change to Disable
                    }
                    else NDBConfig.avatarsToAddColliders.Remove(avatarHash); //Change to N/A
                }
                else NDBConfig.avatarsToAddColliders.Add(avatarHash, true); //Change to Enable

                NDB.otherAvatarButtonList["HandColliders"].GetComponentInChildren<Text>().text = HandCollidersText();
                //MelonPreferences.SetEntryValue<string>("NDB", "AvatarsToAddColliders", string.Join("; ", NDBConfig.avatarsToAddColliders.Select(p => string.Format("{0}, {1}", p.Key, p.Value))));
                //SaveListFiles();
                try { VRCPlayer.Method_Public_Static_Void_APIUser_0(selectedPlayer.prop_APIUser_0); } //Reload Avatar - Thanks loukylor - https://github.com/loukylor/VRC-Mods/blob/main/ReloadAvatars/ReloadAvatarsMod.cs
                catch { } // Ignore
            }, (button) => NDB.otherAvatarButtonList["HandColliders"] = button.transform);

            if (useBigMenu) otherAvatarMenu.AddSimpleButton("Close", () => otherAvatarMenu.Hide());

            otherAvatarMenu.Show();
        }

        private void MultiReplaceRadiusMenu(Player selectedPlayer, bool useBigMenu, string avatarName, string aviID, string avatarHash)
        {
            ICustomShowableLayoutedMenu mrrMenu = null;
            mrrMenu = useBigMenu ? ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList) : ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu1Column11Row);

            mrrMenu.AddLabel("Adjust avatar's DB radius based on a Multiplier or replace entirely");
            mrrMenu.AddLabel(CurrentText(), (button) => NDB.otherAvatarButtonList["Current"] = button.transform);
            //
            string CurrentText()
            {
                return $"Currently: {(NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash) ? (NDBConfig.avatarsToAdjustDBRadius[avatarHash] == -2 ? "Excluded" : (NDBConfig.avatarsToAdjustDBRadius[avatarHash] == 0 ? $"Replacing" : $"Multiplied by {(float)NDBConfig.avatarsToAdjustDBRadius[avatarHash] / 10f}")) : "N/A")}";
            }
            void RadiusButtonEnd()
            {
                NDB.otherAvatarButtonList["Current"].GetComponentInChildren<Text>().text = CurrentText();
                //SaveListFiles();
                if (enabled) { RestoreOriginalColliderList(); AddAllCollidersToAllPlayers(); }
            }

            mrrMenu.AddSimpleButton($"Multiplier+", (() =>
            {
                if (!NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash)) NDBConfig.avatarsToAdjustDBRadius.Add(avatarHash, 11);
                else if (NDBConfig.avatarsToAdjustDBRadius[avatarHash] == 0) NDBConfig.avatarsToAdjustDBRadius[avatarHash] = 11; //If currently replacing switch to x1+1 
                else NDBConfig.avatarsToAdjustDBRadius[avatarHash] += 1;
                RadiusButtonEnd();
            }));

            mrrMenu.AddSimpleButton($"Multiplier-", (() =>
            {
                if (!NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash)) NDBConfig.avatarsToAdjustDBRadius.Add(avatarHash, 9);
                else if (NDBConfig.avatarsToAdjustDBRadius[avatarHash] >= 2) NDBConfig.avatarsToAdjustDBRadius[avatarHash] -= 1; //Avoid 0
                else if (NDBConfig.avatarsToAdjustDBRadius[avatarHash] == 0) NDBConfig.avatarsToAdjustDBRadius[avatarHash] = 9; //If currently replacing switch to x1-1 
                RadiusButtonEnd();
            }));

            mrrMenu.AddSimpleButton($"Replace DB Radius", (() =>
            {
                if (!NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash)) NDBConfig.avatarsToAdjustDBRadius.Add(avatarHash, 0);
                else NDBConfig.avatarsToAdjustDBRadius[avatarHash] = 0;
                RadiusButtonEnd();
            }));

            //if (NDBConfig.adjustRadiusForAllZeroBones)
            //{
            mrrMenu.AddSimpleButton("Exclude from Adjusting All Zero Radius Bones", () =>
            {
                if (!NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash)) NDBConfig.avatarsToAdjustDBRadius.Add(avatarHash, -2);
                else NDBConfig.avatarsToAdjustDBRadius[avatarHash] = -2;
                RadiusButtonEnd();
            });
            mrrMenu.AddLabel("^Exclude the selected avatar from being adjusted by 'Replace DB radius: Adjust All Zero Radius Bones' in Mod Settings");
            //}
            mrrMenu.AddSimpleButton("Remove Multiplier/Replace/Exclude", () =>
            {
                if (NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash))
                {
                    NDBConfig.avatarsToAdjustDBRadius.Remove(avatarHash);
                    RadiusButtonEnd();
                }
            });

            mrrMenu.AddSimpleButton("<--Back", () => { mrrMenu.Hide(); AvatarMenu(selectedPlayer, useBigMenu); });
            mrrMenu.AddSimpleButton("--Close--", () => { mrrMenu.Hide(); });
            mrrMenu.Show();
        }

        private void SettingsMenuMain()
        {
            var settingsMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu3Column);
            settingsMenu.AddLabel("\n\n  MDB Settings");
            settingsMenu.AddSimpleButton($"Press to {((enabled) ? "disable" : "enable")} Dynamic Bones mod", () =>
            {
                ToggleState();
                settingsMenu.Hide(); settingsMenu = null; SettingsMenuMain();
            });
            settingsMenu.AddSimpleButton("Page 2", () =>
            {
                settingsMenu.Hide();
                SettingsMenuTwo();
            });//End Row 1

            string[,] settings ={ {"OnlyMe", "Only I can interact with other bones\n"},
                                  {"OnlyFriends", "Only friends and I can interact w/ eachother"},
                                  {"OnlyHandColliders", "Only enable colliders in hands\n"},//End Row 2
                                  {"OptimizeOnly", "Optimize bones, don't enable interaction"},
                                  {"DisallowDesktoppers", "Desktopers's colliders and bones won't be multiplayer'd"},
                                  {"OnlyDynamicBonesOnBreasts", "Only the breast bones will be multiplayer'd\n"},//End Row 3
                                  {"EnableJustIfVisible", "Enable dynamic bones only if they are in view"},
                                  {"DistanceDisable", "Disable bones if beyond a distance"},
                                  {"MoarBones", "~MoarBones~"}};//End Row 4

            for (int i = 0; i < settings.GetLength(0); i++)
            {
                if (settings[i, 0] == "") { settingsMenu.AddLabel(settings[i, 1]); continue; } //If desc is blank, then skip

                string settingsName = settings[i, 0];
                settingsMenu.AddToggleButton(settings[i, 1], (action) =>
                {
                    MelonPreferences.SetEntryValue<bool>("NDB", settingsName, action); MelonPreferences.Save();
                    if (enabled) { RestoreOriginalColliderList(); AddAllCollidersToAllPlayers(); }
                }
                , () => MelonPreferences.GetEntryValue<bool>("NDB", settingsName));
            }
            settingsMenu.Show();
        }

        private void SettingsMenuTwo()
        {
            var settingsMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu3Column);
            settingsMenu.AddLabel("\n\n  MDB Settings");
            settingsMenu.AddSimpleButton($"Press to {((enabled) ? "disable" : "enable")} Dynamic Bones mod", () =>
            {
                ToggleState();
                settingsMenu.Hide(); settingsMenu = null; SettingsMenuMain();
            });
            settingsMenu.AddSimpleButton("Page 1", () =>
            {
                settingsMenu.Hide();
                SettingsMenuMain();
            });//End Row 1

            string[,] settings ={

                                  { "DestroyInsideCollider", "Destroy inside colliders\n"},
                                  { "DisallowInsideColliders", "Disallow inside colliders from being multiplayered"},
                                  { "ExcludeSpecificBones", "Exclude Specific Objects per avatar"},//End Row 1
                                  { "AddAutoCollidersAll","AutoAdd Hand Colliders to All (Require Avi Reload)" },
                                  { "DisableAllBones", "Disable All Bones"},
                                  { "IncludeSpecificBones", "Include Specific Objects per avatar"},//End Row 2
                                  { "InteractSelf", "Add your own colliders to yourself"},
                                  { "OthersInteractSelf", "Add others colliders to themselves"},
                                  { "", "<--\nThese can cause buggy interaction"}
                                   };

            for (int i = 0; i < settings.GetLength(0); i++)
            {
                if (settings[i, 0] == "") { settingsMenu.AddLabel(settings[i, 1]); continue; } //If desc is blank, then skip

                string settingsName = settings[i, 0];
                settingsMenu.AddToggleButton(settings[i, 1], (action) =>
                {
                    MelonPreferences.SetEntryValue<bool>("NDB", settingsName, action); MelonPreferences.Save();
                    if (enabled) { RestoreOriginalColliderList(); AddAllCollidersToAllPlayers(); }
                }
                , () => MelonPreferences.GetEntryValue<bool>("NDB", settingsName));
            }
            settingsMenu.Show();
        }


        private void ShowBoneSpecificMenu(Player selectedPlayer, bool useBigMenu)
        {
            if (selectedPlayer is null) return;
            NDB.specificButtonList.Clear(); //Clear list of buttons
            DynamicBone[] boneList = avatarsInScene[selectedPlayer._vrcplayer.prop_String_0].Item3;
            ICustomShowableLayoutedMenu boneSpecificMenu = null;
            boneSpecificMenu = useBigMenu ? ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList) : ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu1Column);
            //string playerName = selectedPlayer.field_Private_APIUser_0.displayName;
            string avatarName = selectedPlayer.prop_ApiAvatar_0.name;
            string aviID = selectedPlayer.prop_VRCPlayer_0.prop_VRCAvatarManager_0.prop_ApiAvatar_0.id;
            boneSpecificMenu.AddLabel($"Specificly Exclude/Include multiplayering bones on avatar:\n{avatarName}");
            boneSpecificMenu.AddSimpleButton("<--Back", () => { boneSpecificMenu.Hide(); AvatarMenu(selectedPlayer, useBigMenu); });

            foreach (var bone in boneList)
            {
                if (bone.m_Root is null || bone.m_Root.Equals(null)) continue;
                try
                {
                    string hashBone = avatarName.Substring(0, Math.Min(avatarName.Length, 20)) + ":" + String.Format("{0:X}", aviID.GetHashCode()).Substring(4) + ":db:" + bone.m_Root.name;
                    if (NDB.specificButtonList.ContainsKey(hashBone)) continue; //For the instance where a bone may have more than one db/dbc
                    NDB.specificButtonList.Add(hashBone, null);
                    string boneName = bone.m_Root.name; //To stop an NRE if the player leaves or switches when menu is open
                    boneSpecificMenu.AddSimpleButton($"{(NDBConfig.bonesToExclude.Contains(hashBone) ? "Excluded - " : (NDBConfig.bonesToInclude.Contains(hashBone) ? "Included - " : "N/A - "))} {boneName}", () =>
                    {
                        ToggleBoneSpecific(hashBone, boneName);
                    }, (button) => NDB.specificButtonList[hashBone] = button.transform);

                }
                catch (System.Exception ex) { LogDebugInt(0, ConsoleColor.DarkRed, $"Error in BoneList\n" + ex.ToString()); }
            }
            boneSpecificMenu.AddSimpleButton("Reload MDB/Apply Changes", () =>
            {
                if (enabled) //This is lazy
                { RestoreOriginalColliderList(); AddAllCollidersToAllPlayers(); }
                else ToggleState();
            });
            boneSpecificMenu.AddSimpleButton("Debug: Print excluded to console", () => PrintBonesSpecific());
            boneSpecificMenu.AddSimpleButton("Close", () => { boneSpecificMenu.Hide(); });
            boneSpecificMenu.Show();
        }


        private void ToggleBoneSpecific(string hashBone, string boneName)
        {
            if (!NDBConfig.bonesToExclude.Contains(hashBone) && !NDBConfig.bonesToInclude.Contains(hashBone))
            {
                NDBConfig.bonesToExclude.Add(hashBone);
                NDB.specificButtonList[hashBone].GetComponentInChildren<Text>().text = $"Excluded - {boneName}";
            }
            else if (NDBConfig.bonesToExclude.Contains(hashBone))
            {
                NDBConfig.bonesToExclude.Remove(hashBone);
                NDBConfig.bonesToInclude.Add(hashBone);
                NDB.specificButtonList[hashBone].GetComponentInChildren<Text>().text = $"Included - {boneName}";
            }
            else
            { //Removing both to ensure consistency
                if (NDBConfig.bonesToExclude.Contains(hashBone)) NDBConfig.bonesToExclude.Remove(hashBone);
                if (NDBConfig.bonesToInclude.Contains(hashBone)) NDBConfig.bonesToInclude.Remove(hashBone);
                NDB.specificButtonList[hashBone].GetComponentInChildren<Text>().text = $"N/A - {boneName}";
            }
        }

        private void ShowColliderSpecificMenu(Player selectedPlayer, bool useBigMenu)
        {
            if (selectedPlayer is null) return;
            NDB.specificButtonList.Clear(); //Clear list of buttons
            DynamicBoneCollider[] boneList = avatarsInScene[selectedPlayer._vrcplayer.prop_String_0].Item4;
            ICustomShowableLayoutedMenu colliderSpecificMenu = null;
            colliderSpecificMenu = useBigMenu ? ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList) : ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu1Column);
            //string playerName = selectedPlayer.field_Private_APIUser_0.displayName;
            string avatarName = selectedPlayer.prop_ApiAvatar_0.name;
            string aviID = selectedPlayer.prop_VRCPlayer_0.prop_VRCAvatarManager_0.prop_ApiAvatar_0.id;
            colliderSpecificMenu.AddLabel($"Specificly Exclude/Include multiplayering colliders on avatar:\n{avatarName}");
            colliderSpecificMenu.AddSimpleButton("<--Back", () => { colliderSpecificMenu.Hide(); AvatarMenu(selectedPlayer, useBigMenu); });

            foreach (var bone in boneList)
            {
                if (bone.gameObject is null || bone.gameObject.Equals(null)) continue;
                try
                {
                    string hashBone = avatarName.Substring(0, Math.Min(avatarName.Length, 20)) + ":" + String.Format("{0:X}", aviID.GetHashCode()).Substring(4) + ":dbc:" + bone.name;
                    if (NDB.specificButtonList.ContainsKey(hashBone)) continue; //For the instance where a bone may have more than one db/dbc
                    NDB.specificButtonList.Add(hashBone, null);
                    string boneName = bone.name; //To stop an NRE if the player leaves or switches when menu is open
                    colliderSpecificMenu.AddSimpleButton($"{((NDBConfig.collidersToExclude.Contains(hashBone)) ? "Excluded - " : (NDBConfig.collidersToInclude.Contains(hashBone) ? "Included - " : "N/A - "))} {boneName}", () =>
                    {
                        ToggleColliderSpecific(hashBone, boneName);
                    }, (button) => NDB.specificButtonList[hashBone] = button.transform);
                }
                catch (System.Exception ex) { LogDebugInt(0, ConsoleColor.DarkRed, $"Error in ColliderList\n" + ex.ToString()); }
            }
            colliderSpecificMenu.AddSimpleButton("Reload MDB/Apply Changes", () =>
            {
                if (enabled) //This is lazy
                { RestoreOriginalColliderList(); AddAllCollidersToAllPlayers(); }
                else ToggleState();
            });
            colliderSpecificMenu.AddSimpleButton("Debug: Print excluded to console", () => PrintBonesSpecific());
            colliderSpecificMenu.AddSimpleButton("Close", () => { colliderSpecificMenu.Hide(); });
            colliderSpecificMenu.Show();
        }
        private void ToggleColliderSpecific(string hashBone, string boneName)
        {
            if (!NDBConfig.collidersToExclude.Contains(hashBone) && !NDBConfig.collidersToInclude.Contains(hashBone))
            {
                NDBConfig.collidersToExclude.Add(hashBone);
                NDB.specificButtonList[hashBone].GetComponentInChildren<Text>().text = $"Excluded - {boneName}";
            }
            else if (NDBConfig.collidersToExclude.Contains(hashBone))
            {
                NDBConfig.collidersToExclude.Remove(hashBone);
                NDBConfig.collidersToInclude.Add(hashBone);
                NDB.specificButtonList[hashBone].GetComponentInChildren<Text>().text = $"Included - {boneName}";
            }
            else
            { //Removing both to ensure consistency
                if (NDBConfig.collidersToExclude.Contains(hashBone)) NDBConfig.collidersToExclude.Remove(hashBone);
                if (NDBConfig.collidersToInclude.Contains(hashBone)) NDBConfig.collidersToInclude.Remove(hashBone);
                NDB.specificButtonList[hashBone].GetComponentInChildren<Text>().text = $"N/A - {boneName}";
            }
        }


        private void PrintBonesSpecific()
        {
            if (!enabled) { LogDebug(ConsoleColor.Cyan, $"DBM is disabled, nothing can be excluded"); return; }
            if (NDBConfig.onlyOptimize) { LogDebug( ConsoleColor.Cyan, $"DBM is set to only optimized bones, nothing can be excluded"); return; }

            LogDebug( ConsoleColor.Cyan, $"Printing Specificly Excluded DynamicBones and DynamicBoneColliders");
            if (NDB.bonesExcluded != null && NDB.bonesExcluded.Count > 0)
            {
                List<string> templist = NDB.bonesExcluded.ToList<string>(); templist.Sort();
                foreach (string excludedBone in templist) LogDebug( ConsoleColor.Cyan, $"db - {excludedBone}");
            }
            else LogDebug( ConsoleColor.Cyan, $"No bones excluded");

            if (NDB.collidersExcluded != null && NDB.collidersExcluded.Count > 0)
            {
                List<string> templist = NDB.collidersExcluded.ToList<string>(); templist.Sort();
                foreach (string excludedColliders in templist) LogDebug( ConsoleColor.Cyan, $"dbc - {excludedColliders}");
            }
            else LogDebug( ConsoleColor.Cyan, $"No colliders excluded");

            LogDebug( ConsoleColor.Cyan, $"Printing Specificly Included DynamicBones and DynamicBoneColliders");
            if (NDB.bonesIncluded != null && NDB.bonesIncluded.Count > 0)
            {
                List<string> templist = NDB.bonesIncluded.ToList<string>(); templist.Sort();
                foreach (string includedBones in templist) LogDebug( ConsoleColor.Cyan, $"db - {includedBones}");
            }
            else LogDebug( ConsoleColor.Cyan, $"No bones included");

            if (NDB.collidersIncluded != null && NDB.collidersIncluded.Count > 0)
            {
                List<string> templist = NDB.collidersIncluded.ToList<string>(); templist.Sort();
                foreach (string includedColliders in templist) LogDebug(ConsoleColor.Cyan, $"dbc - {includedColliders}");
            }
            else LogDebug( ConsoleColor.Cyan, $"No colliders included");
        }

        private void CheckForUpdates()
        {
            try
            {
                string url = "https://raw.githubusercontent.com/9E4ECDDE/MultiplayerDynamicBonesMod/master/updateCheck";
                WebClient client = new WebClient();
                string updateCheckString = client.DownloadString(url);

                if (float.Parse(updateCheckString) > float.Parse(NDB.VERSION_STR))
                {
                    LogDebug( ConsoleColor.Green, $"New version found - Creating MessageBox to notify user. Local:{NDB.VERSION_STR} Remote:{updateCheckString}");
                    if (MessageBox(IntPtr.Zero, "There is an update avaiable for Multiplayer Dynamic Bones. Not updating could result in the mod not working or the game crashing. Do you want to launch the internet browser?", "Multiplayer Dynamic Bones Mod", 0x04 | 0x40 | 0x1000) == 6)
                    {
                        Process.Start("https://github.com/9E4ECDDE/MultiplayerDynamicBonesMod/releases");
                        MessageBox(IntPtr.Zero, "Please replace the file and restart VRChat for the update to apply", "Multiplayer Dynamic Bones Mod", 0x40 | 0x1000);
                    }
                }
                else LogDebugInt(1, ConsoleColor.DarkGreen, $"No Update Found. Local:{NDB.VERSION_STR} Remote:{updateCheckString}");
            }
            catch (Exception ex) { LogDebugError($"Update check error " + ex.ToString()); return; }

        }

        private void MoarBoneCheck()
        {
            try
            { //Change this next time to disable MoarBones if No is clicked - Add more explaination to what this does and how to disable.
                LogDebug( ConsoleColor.Green, $"It is 4/1 - Checking if user wants to enable this feature");
                if (MessageBox(IntPtr.Zero, "There is a new feature for Multiplayer Dynamic Bones! Do you want to enable it? \nYou can disable it later in Mod Settings: Moarbones", "Multiplayer Dynamic Bones Mod", 0x04 | 0x40 | 0x1000) == 6)
                {
                    LogDebug( ConsoleColor.Magenta, $"~~~~~~~~~~~~~~~Moarbones Enabled~~~~~~~~~~~~~~~");
                    LogDebug( ConsoleColor.Magenta, $"THIS CAN BE DISABLED IN MOD SETTINGS - MOARBONES");
                    MelonPreferences.SetEntryValue<bool>("NDB", "MoarBones", true);
                    NDBConfig.moarBones = true;
                }
            }
            catch (Exception ex) { LogDebugError(ex.ToString()); return; }
        }

        private static unsafe void RegisterModPrefs()
        {
            MelonPreferences.CreateCategory("NDB", "Multiplayer Dynamic Bones");
            MelonPreferences.CreateEntry<bool>("NDB", "EnabledByDefault", true, "Enabled by default");
            MelonPreferences.CreateEntry<bool>("NDB", "OptimizeOnly", false, "Optimize bones, don't enable interaction [QM]");
            //What gets Mutliplayered?
            MelonPreferences.CreateEntry<bool>("NDB", "OnlyMe", false, "Only I can interact with other bones [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "OnlyFriends", false, "Only friends and I can interact w/ eachothers bones [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "DisallowDesktoppers", false, "Desktopers's colliders and bones won't be multiplayer'd [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "OnlyHandColliders", false, "Only enable colliders in hands [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "OnlyDynamicBonesOnBreasts", false, "Only the breast bones will be multiplayer'd [QM]");

            MelonPreferences.CreateEntry<bool>("NDB", "InteractSelf", false, "Add your colliders to your own bones (May cause buggy interactions) [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "OthersInteractSelf", false, "Add other avatar's colliders to their own bones (May cause buggy interactions) [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "AddAutoCollidersAll", false, "Auto add hand colliders to avatars that don't have them (Requires reload of avatar) [QM]");

            MelonPreferences.CreateEntry<bool>("NDB", "ExcludeSpecificBones", true, "Exclude Specific Bones or Colliders from being Multiplayered[QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "IncludeSpecificBones", true, "Include Specific Bones or Colliders to be Multiplayered[QM]");

            //Bone settings
            MelonPreferences.CreateEntry<bool>("NDB", "DistanceDisable", true, "Disable bones if beyond a distance [QM]");
            MelonPreferences.CreateEntry<float>("NDB", "DistanceToDisable", 4f, "Distance limit");
            MelonPreferences.CreateEntry<bool>("NDB", "DisallowInsideColliders", true, "Disallow inside colliders from being multiplayered (Default Enabled) [QM]");
            MelonPreferences.CreateEntry<bool>("NDB", "DestroyInsideColliders", false, "Destroy inside colliders (Requires reload of avatar) [QM]");
            MelonPreferences.CreateEntry<float>("NDB", "ColliderSizeLimit", 1f, "Collider size limit ");
            MelonPreferences.CreateEntry<int>("NDB", "DynamicBoneUpdateRate", 60, "Dynamic bone update rate");
            MelonPreferences.CreateEntry<bool>("NDB", "DynamicBoneUpdateRateAdjSettings", true, "Adjust bone properties in a ratio from update rate change");
            MelonPreferences.CreateEntry<bool>("NDB", "EnableJustIfVisible", true, "Enable dynamic bones only if they are in visible [QM]");
            MelonPreferences.CreateEntry<float>("NDB", "VisibilityUpdateRate", 1f, "Visibility update rate (seconds)");

            MelonPreferences.CreateEntry<int>("NDB", "BoneRadiusDivisor", 4, "Replace DB radius: Divisor - New Radius = BoneLegnth / ThisValue");
            MelonPreferences.CreateEntry<float>("NDB", "EndBoneRadius", 0.05f, "Replace DB radius: This is the fallback radius if the calculated value is 0");
            MelonPreferences.CreateEntry<bool>("NDB", "AdjustRadiusExcludeZero", false, "Replace DB radius: Excludes bone with a radius of 0 from being changed");
            MelonPreferences.CreateEntry<bool>("NDB", "AdjustRadiusForAllZeroBones", false, "Replace DB radius: Adjust All Zero Radius Bones - Replace the radius for all bones with a radius of 0 on all avatars");
            MelonPreferences.CreateEntry<bool>("NDB", "DisableAllBones", false, "Disable all Dynamic Bones in Scene [QM]");

            //Mod settings
            MelonPreferences.CreateEntry<bool>("NDB", "KeybindsEnabled", true, "Enable keyboard actuation(F1, F4 and F8)");
            MelonPreferences.CreateEntry<int>("NDB", "UpdateMode", 2, "A value of 2 will notify the user when a new version of the mod is available, while 1 will not.");
            MelonPreferences.CreateEntry<bool>("NDB", "EnableEditor", false, "EnableEditor (F5)");
            //MelonPreferences.CreateEntry<bool>("NDB", "EnableUserPanelButton-AvatarsExclude", true, "Enable the button that allows per-avatar dynamic bones enable or disable (Restart Req)");
            //MelonPreferences.CreateEntry<int>("NDB", "UserPanelButtonX", 0, "X offset for the user panel button (Restart Req - Default:0)");
            //MelonPreferences.CreateEntry<int>("NDB", "UserPanelButtonY", -1, "Y offset for the user panel button (Restart Req - Default:-1)");
            MelonPreferences.CreateEntry<int>("NDB", "QuickMenuButton", 1, "Quick Menu Button - 1:Settings Menu, 2:Just Toggle, 0:None (Restart Req)");
            
            MelonPreferences.CreateEntry<string>("NDB", "LogLevelS", "0", "Console Logging Level:"); // 1-Just info, 2-Limited to once per avatar or behind a filter IF, 3-Filters/Logs in 1st Level Loops, 4-Filters/Logs in 2nd+ Level Loops , 5-Extra Debug Lines
            ExpansionKitApi.RegisterSettingAsStringEnum("NDB", "LogLevelS", new[] { ("0", "Default"), ("1", "Info"), ("2", "Debug"), ("3", "Debug Loops"), ("4", "Debug Deep Loops(Very laggy)"), ("5", "All Possible(Very laggy)"), ("-1", "Silent Mode") });
            MelonPreferences.CreateEntry<string>("NDB", "DebugLogs", "0", "DebugLog - Writes a seperate Debug log to disk"); 
            ExpansionKitApi.RegisterSettingAsStringEnum("NDB", "DebugLogs", new[] { ("0", "Off"), ("1", "Info"), ("2", "Debug"), ("3", "Debug Loops"), ("4", "Debug Deep Loops(Laggy)"), ("5", "All Possible(Laggy)") });

            MelonPreferences.CreateEntry<bool>("NDB", "MoarBones", false, "MoarBones: I hear you like bones~ (Makes all bones Dynamic)");
            MelonPreferences.CreateEntry<bool>("NDB", "MoarBonesPref", true, "MoarBones: Performance Limit");
            MelonPreferences.CreateEntry<bool>("NDB", "MoarBonesNotLocal", true, "MoarBones: Don't effect local avatar");
            //Change to use this at one point
            //ExpansionKitApi.RegisterSettingAsStringEnum("NDB", "QuickMenuButton", new[] { ("0", "Disable Trust Colours"), ("friends", "Trust Colours (with friend colour)"), ("trustonly", "Trust Colours (Ignore friend colour)"), ("trustname", "Trust Colours on Names Only") });

            //MelonPreferences.CreateEntry<int>("NDB", "removeme", 0, "removeme"); // 1-Just info, 2-Limited to once per avatar or behind a filter IF, 3-Spammy stuff in loops
            //Not shown
            //MelonPreferences.CreateEntry<bool>("NDB", "HasShownCompatibilityIssueMessage", false, null, true);
            MelonPreferences.CreateEntry<string>("NDB", "AvatarsToWhichNotApply", "", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "BonesToExclude", "", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "CollidersToExclude", "", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "BonesToAlwaysExclude", "Left Z_Wing_Bone_3;Left Z_Wing_Bone_2;Left Z_Wing_Bone_1;Right Z_Wing_Bone_3;Right Z_Wing_Bone_2;Right Z_Wing_Bone_1", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "AvatarsToAdjustDBRadius", "", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "AvatarsToAddColliders", "", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "BonesToInclude", "", null, true);
            MelonPreferences.CreateEntry<string>("NDB", "CollidersToInclude", "", null, true);
        }

        private static int scenesLoaded;
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            scenesLoaded++;
            if (scenesLoaded == 2)
                HookCallbackFunctions();
        }

        private unsafe void HookCallbackFunctions()
        {
            bool isPlayerEventAdded1 = false;
            bool isPlayerEventAdded2 = false;
            try
            {
                //harmonyInstance = HarmonyInstance.Create("NDB");

                //IntPtr funcToHook = (IntPtr)typeof(VRCAvatarManager.MulticastDelegateNPublicSealedVoGaVRBoUnique).GetField("NativeMethodInfoPtr_Invoke_Public_Virtual_New_Void_GameObject_VRC_AvatarDescriptor_Boolean_0", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                //Hook(funcToHook, new System.Action<IntPtr, IntPtr, IntPtr, bool>(OnAvatarInstantiated).Method.MethodHandle.GetFunctionPointer());
                //onAvatarInstantiatedDelegate = Marshal.GetDelegateForFunctionPointer<AvatarInstantiatedDelegate>(*(IntPtr*)funcToHook);
                //MelonLogger.Msg(ConsoleColor.Blue, $"Hooked OnAvatarInstantiated? {((onAvatarInstantiatedDelegate != null) ? "Yes!" : "No: critical error!!")}");

                //MethodBase funcToHook = typeof(VRCAvatarManager.MulticastDelegateNPublicSealedVoGaVRBoUnique).GetMethods().First(mi => mi.Name.StartsWith("Invoke")); 
                //MethodBase funcToHook = typeof(VRCPlayer).GetMethods().First(mi => mi.Name.StartsWith("Method_Private_Void_GameObject_VRC_AvatarDescriptor_Boolean_") && !Xref.CheckMethod(mi, "Avatar is Ready, Initializing")); //Thanks to https://github.com/loukylor/VRC-Mods/blob/7b0663da72b775ea5018ec546f914a5fc06b0013/PlayerList/Utilities/NetworkEvents.cs#L84
                //Above methods are always false
                MethodBase funcToHook = typeof(VRCAvatarManager).GetMethods().First(mb => mb.Name.StartsWith("Method_Private_Void_ApiAvatar_GameObject_Action_1_Boolean_")); //Thanks to loukylor
                onAvatarInstantiatedPatch = HarmonyInstance.Patch(funcToHook, null, new HarmonyMethod(typeof(NDB).GetMethod(nameof(OnAvatarInstantiated), BindingFlags.NonPublic | BindingFlags.Static)));
                LogDebug(ConsoleColor.Blue, $"Hooked OnAvatarInstantiated? {((onAvatarInstantiatedPatch != null) ? "Yes!" : "No: critical error!!")}");

                onJoinedRoomPatch = HarmonyInstance.Patch(typeof(NetworkManager).GetMethods().Single((fi) => fi.Name.Contains("OnJoinedRoom")), new HarmonyMethod(typeof(NDB).GetMethod(nameof(Reset))));
                LogDebug(ConsoleColor.Blue, $"Patched OnJoinedRoom? {((onJoinedRoomPatch != null) ? "Yes!" : "No: critical error!!")}");

                AddToDelegate(NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0, EventA);
                isPlayerEventAdded1 = true;
                LogDebug(ConsoleColor.Blue, $"Added Delegate for Player Event (1/2)? {((isPlayerEventAdded1) ? "Yes!" : "No: critical error!!")}");

                AddToDelegate(NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1, EventB);
                isPlayerEventAdded2 = true;
                LogDebug(ConsoleColor.Blue, $"Added Delegate for Player Event (2/2)? {((isPlayerEventAdded2) ? "Yes!" : "No: critical error!!")}");
            }
            catch (Exception ex) { LogDebugError(ex.ToString()); return; }
            finally
            {
                if (onAvatarInstantiatedPatch == null || onJoinedRoomPatch == null || !isPlayerEventAdded1 || !isPlayerEventAdded2 || !HookLIC)
                {
                    this.enabled = false;
                    LogDebugError("Multiplayer Dynamic Bones mod suffered a critical error! Mod version may be obsolete.");
                }
            }
            LogDebug(ConsoleColor.Green, $"NDBMod is {((enabled == true) ? "enabled" : "disabled")}");
        }

        private static void AddToDelegate(VRCEventDelegate<Player> theDelegate, Action<Player> theEvent)
        {
            theDelegate.field_Private_HashSet_1_UnityAction_1_T_0.Add(theEvent);
        }

        private enum SeenFirst
        {
            None,
            A,
            B
        }

        private static SeenFirst seenFirst = SeenFirst.None;

        public static void EventA(Player player)
        {
            if (seenFirst == SeenFirst.None)
                seenFirst = SeenFirst.A;

            if (player == null)
                return;

            if (seenFirst == SeenFirst.A)
                OnPlayerJoin(player);
            else
                OnPlayerLeave(player);
        }

        public static void EventB(Player player)
        {
            if (seenFirst == SeenFirst.None)
                seenFirst = SeenFirst.B;

            if (player == null)
                return;

            if (seenFirst == SeenFirst.B)
                OnPlayerJoin(player);
            else
                OnPlayerLeave(player);
        }

        private static void OnPlayerJoin(Player player)
        {
            //MelonLogger.Msg(ConsoleColor.Blue, $"{player.prop_APIUser_0?.displayName} Joined");
        }
        private static void OnPlayerLeave(Player player)
        {
            //MelonLogger.Msg(ConsoleColor.Blue, $"{player.prop_APIUser_0?.displayName} Left");
            OnPlayerLeft(player);
        }

        public void LogLevelWarning(int loglvl)
        {
            if (loglvl >= 3) LogDebug(ConsoleColor.Magenta, $"Log level set to debug extra (3+), this will spam the console a lot and will cause lag. \n ======================== Disable this unless you are actively trying to debug something ======================== \n ======================== Disable this unless you are actively trying to debug something ========================");
            else if (loglvl == 2) LogDebug(ConsoleColor.Magenta, $"Log level set to debug (2), this is mostly limited to once per avatar items or if(s) that get met.");
            else if (loglvl == 1) LogDebug(ConsoleColor.Yellow, $"Log level set to info (1).");
            else if (loglvl == -1) LogDebug(ConsoleColor.Yellow, $"Log level set to silent mode (-1). \n ======================== No Add/Leave Log Messages Will Print ======================== \n");
        }

        public override void OnPreferencesSaved()
        {
            int loglvl = 0;
            try { loglvl = int.Parse(MelonPreferences.GetEntryValue<string>("NDB", "LogLevelS")); }
            catch { LogDebug( ConsoleColor.Yellow, $"Log level value is invalid"); }
            if (NDBConfig.logLevel != loglvl) LogLevelWarning(loglvl); //If settings changed, send warning

            if (!firstrun && MelonPreferences.GetEntryValue<bool>("NDB", "MoarBones") != NDBConfig.moarBones)
            {
                LogDebug( ConsoleColor.Magenta, "MoarBones State Changed, Reload All Avatars");
                moarbonesCount = 0;
                try
                {   // Reload All Avatar - Thanks loukylor - https://github.com/loukylor/VRC-Mods/blob/main/ReloadAvatars/ReloadAvatarsMod.cs
                    MethodInfo reloadAllAvatarsMethod = typeof(VRCPlayer).GetMethods().First(mi => mi.Name.StartsWith("Method_Public_Void_Boolean_") && mi.Name.Length < 30 && mi.GetParameters().All(pi => pi.HasDefaultValue ));// Both methods seem to do the same thing
                    reloadAllAvatarsMethod.Invoke(VRCPlayer.field_Internal_Static_VRCPlayer_0, new object[] { null });
                    //VRCPlayer.field_Internal_Static_VRCPlayer_0.Method_Public_Void_Boolean_0(); 
                }
                catch { LogDebugError("Failed to reload all avatars - You will have to rejoin the world - Check for a newer version of this mod or report this bug"); } // Ignore
            }

            NDBConfig.enabledByDefault = MelonPreferences.GetEntryValue<bool>("NDB", "EnabledByDefault");
            NDBConfig.disallowInsideColliders = MelonPreferences.GetEntryValue<bool>("NDB", "DisallowInsideColliders");
            NDBConfig.destroyInsideColliders = MelonPreferences.GetEntryValue<bool>("NDB", "DestroyInsideColliders");
            NDBConfig.distanceToDisable = MelonPreferences.GetEntryValue<float>("NDB", "DistanceToDisable");
            NDBConfig.distanceDisable = MelonPreferences.GetEntryValue<bool>("NDB", "DistanceDisable");
            NDBConfig.colliderSizeLimit = MelonPreferences.GetEntryValue<float>("NDB", "ColliderSizeLimit");
            NDBConfig.onlyForMyBones = MelonPreferences.GetEntryValue<bool>("NDB", "OnlyMe");
            NDBConfig.onlyForMeAndFriends = MelonPreferences.GetEntryValue<bool>("NDB", "OnlyFriends");
            NDBConfig.dynamicBoneUpdateRate = MelonPreferences.GetEntryValue<int>("NDB", "DynamicBoneUpdateRate");
            NDBConfig.dynamicBoneUpdateRateAdjSettings = MelonPreferences.GetEntryValue<bool>("NDB", "DynamicBoneUpdateRateAdjSettings");
            NDBConfig.disallowDesktoppers = MelonPreferences.GetEntryValue<bool>("NDB", "DisallowDesktoppers");
            NDBConfig.enableBoundsCheck = MelonPreferences.GetEntryValue<bool>("NDB", "EnableJustIfVisible");
            NDBConfig.visiblityUpdateRate = MelonPreferences.GetEntryValue<float>("NDB", "VisibilityUpdateRate");
            NDBConfig.onlyHandColliders = MelonPreferences.GetEntryValue<bool>("NDB", "OnlyHandColliders");
            NDBConfig.keybindsEnabled = MelonPreferences.GetEntryValue<bool>("NDB", "KeybindsEnabled");
            NDBConfig.onlyOptimize = MelonPreferences.GetEntryValue<bool>("NDB", "OptimizeOnly");
            NDBConfig.updateMode = MelonPreferences.GetEntryValue<int>("NDB", "UpdateMode");
            //NDBConfig.hasShownCompatibilityIssueMessage = MelonPreferences.GetEntryValue<bool>("NDB", "HasShownCompatibilityIssueMessage");

            NDBConfig.enableEditor = MelonPreferences.GetEntryValue<bool>("NDB", "EnableEditor");
            NDBConfig.breastsOnly = MelonPreferences.GetEntryValue<bool>("NDB", "OnlyDynamicBonesOnBreasts");
            //NDBConfig.enableUserPanelButton = MelonPreferences.GetEntryValue<bool>("NDB", "EnableUserPanelButton-AvatarsExclude");
            //NDBConfig.userPanelButtonX = MelonPreferences.GetEntryValue<int>("NDB", "UserPanelButtonX");
            //NDBConfig.userPanelButtonY = MelonPreferences.GetEntryValue<int>("NDB", "UserPanelButtonY");

            NDBConfig.excludeSpecificBones = MelonPreferences.GetEntryValue<bool>("NDB", "ExcludeSpecificBones");
            NDBConfig.includeSpecificBones = MelonPreferences.GetEntryValue<bool>("NDB", "IncludeSpecificBones");
            NDBConfig.interactSelf = MelonPreferences.GetEntryValue<bool>("NDB", "InteractSelf");
            NDBConfig.othersInteractSelf = MelonPreferences.GetEntryValue<bool>("NDB", "OthersInteractSelf");

            NDBConfig.boneRadiusDivisor = MelonPreferences.GetEntryValue<int>("NDB", "BoneRadiusDivisor");
            NDBConfig.endBoneRadius = MelonPreferences.GetEntryValue<float>("NDB", "EndBoneRadius");

            NDBConfig.addAutoCollidersAll = MelonPreferences.GetEntryValue<bool>("NDB", "AddAutoCollidersAll");
            NDBConfig.adjustRadiusExcludeZero = MelonPreferences.GetEntryValue<bool>("NDB", "AdjustRadiusExcludeZero");
            NDBConfig.adjustRadiusForAllZeroBones = MelonPreferences.GetEntryValue<bool>("NDB", "AdjustRadiusForAllZeroBones");
            NDBConfig.disableAllBones = MelonPreferences.GetEntryValue<bool>("NDB", "DisableAllBones");
            NDBConfig.moarBones = MelonPreferences.GetEntryValue<bool>("NDB", "MoarBones");
            NDBConfig.moarBonesPrefLimit = MelonPreferences.GetEntryValue<bool>("NDB", "MoarBonesPref");
            NDBConfig.moarBonesNotLocal = MelonPreferences.GetEntryValue<bool>("NDB", "MoarBonesNotLocal");


            NDBConfig.logLevel = loglvl;

            try { NDBConfig.debugLog = int.Parse(MelonPreferences.GetEntryValue<string>("NDB", "DebugLogs")); }
            catch { LogDebug(ConsoleColor.Yellow, $"Debug Log level value is invalid"); }
            if (NDBConfig.debugLog > 0) InitDebugLog();

            SaveListFiles();
        }




        private void InitFileLists()
        {
            if (!Directory.Exists("UserData/MDB")) Directory.CreateDirectory("UserData/MDB");
            if (!File.Exists(AvatarsToWhichNotApplyPath)) File.WriteAllText(AvatarsToWhichNotApplyPath, "", Encoding.UTF8);
            if (!File.Exists(BonesToExcludePath)) File.WriteAllText(BonesToExcludePath, "", Encoding.UTF8);
            if (!File.Exists(CollidersToExcludePath)) File.WriteAllText(CollidersToExcludePath, "", Encoding.UTF8);
            if (!File.Exists(BonesToAlwaysExcludePath)) File.WriteAllText(BonesToAlwaysExcludePath, "", Encoding.UTF8);
            if (!File.Exists(AvatarsToAdjustDBRadiusPath)) File.WriteAllText(AvatarsToAdjustDBRadiusPath, "", Encoding.UTF8);
            if (!File.Exists(AvatarsToAddCollidersPath)) File.WriteAllText(AvatarsToAddCollidersPath, "", Encoding.UTF8);
            if (!File.Exists(BonesToIncludePath)) File.WriteAllText(BonesToIncludePath, "", Encoding.UTF8);
            if (!File.Exists(CollidersToIncludePath)) File.WriteAllText(CollidersToIncludePath, "", Encoding.UTF8);

            //MigrateOrLoadHashSet("AvatarsToWhichNotApply", AvatarsToWhichNotApplyPath, ref NDBConfig.avatarsToWhichNotApply);
            MigrateOrLoadHashSet("BonesToExclude", BonesToExcludePath, ref NDBConfig.bonesToExclude);
            MigrateOrLoadHashSet("CollidersToExclude", CollidersToExcludePath, ref NDBConfig.collidersToExclude);
            MigrateOrLoadHashSet("BonesToAlwaysExclude", BonesToAlwaysExcludePath, ref NDBConfig.bonesToAlwaysExclude);
            MigrateOrLoadHashSet("BonesToInclude", BonesToIncludePath, ref NDBConfig.bonesToInclude);
            MigrateOrLoadHashSet("CollidersToInclude", CollidersToIncludePath, ref NDBConfig.collidersToInclude);

            var migrated = false;
            try
            {
                if (MelonPreferences.GetEntryValue<string>("NDB", "AvatarsToWhichNotApply") != "")
                {//If modprefs has value, then convert into standlone file 
                    if (IsTextFileEmpty(AvatarsToWhichNotApplyPath))
                    {//Check if file already had content, if so, abort and error 
                        var temp = new HashSet<string>(MelonPreferences.GetEntryValue<string>("NDB", "AvatarsToWhichNotApply").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        NDBConfig.avatarsToWhichNotApply = new Dictionary<string, bool>();
                        foreach (var value in temp)
                        {//Switching from just a list of values to togglable Enable/Disable/NA
                            NDBConfig.avatarsToWhichNotApply.Add(value, true);
                        }
                        File.WriteAllLines(AvatarsToWhichNotApplyPath, NDBConfig.avatarsToWhichNotApply.Select(p => string.Format("{0}, {1}", p.Key, p.Value)), Encoding.UTF8); //Save file
                        MelonPreferences.SetEntryValue<string>("NDB", "AvatarsToWhichNotApply", "");
                        migrated = true;
                        LogDebug(ConsoleColor.Blue, "Migrated from MelonPreferences to " + AvatarsToWhichNotApplyPath);
                    }
                    else LogDebug(ConsoleColor.Red, "MelonPreferences has content but " + AvatarsToWhichNotApplyPath + " is not empty. Can not migrate records. ");
                }
                if (!IsTextFileEmpty(AvatarsToWhichNotApplyPath))
                {
                    if (!migrated)
                    {
                        NDBConfig.avatarsToWhichNotApply = new Dictionary<string, bool>(File.ReadAllLines(AvatarsToWhichNotApplyPath).Select(s => s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).Where(x => x.Length == 2).ToDictionary(p => p[0].Trim(), p => bool.Parse(p[1].Trim())));
                        LogDebug(ConsoleColor.DarkBlue, "Loaded - " + AvatarsToWhichNotApplyPath);
                    }
                }
                else NDBConfig.avatarsToWhichNotApply = new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                LogDebugError("Possible corrupted file: " + AvatarsToWhichNotApplyPath + " File will be renamed and new file created \n" + ex.ToString());
                NDBConfig.avatarsToWhichNotApply = new Dictionary<string, bool>();
                File.Move(AvatarsToWhichNotApplyPath, AvatarsToWhichNotApplyPath + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss") + ".bkp");
            }
            //======
            migrated = false;
            try
            {
                if (MelonPreferences.GetEntryValue<string>("NDB", "AvatarsToAdjustDBRadius") != "")
                {//If modprefs has value, then convert into standlone file 
                    if (IsTextFileEmpty(AvatarsToAdjustDBRadiusPath))
                    {//Check if file already had content, if so, abort and error 
                        NDBConfig.avatarsToAdjustDBRadius = new Dictionary<string, int>(MelonPreferences.GetEntryValue<string>("NDB", "AvatarsToAdjustDBRadius").Split(';').Select(s => s.Split(',')).ToDictionary(p => p[0].Trim(), p => Int32.Parse(p[1].Trim())));
                        File.WriteAllLines(AvatarsToAdjustDBRadiusPath, NDBConfig.avatarsToAdjustDBRadius.Select(p => string.Format("{0}, {1}", p.Key, p.Value)), Encoding.UTF8); //Save file
                        MelonPreferences.SetEntryValue<string>("NDB", "AvatarsToAdjustDBRadius", "");
                        migrated = true;
                        LogDebug(ConsoleColor.Blue, "Migrated from MelonPreferences to " + AvatarsToAdjustDBRadiusPath);
                    }
                    else LogDebug(ConsoleColor.Red, "MelonPreferences has content but " + AvatarsToAdjustDBRadiusPath + " is not empty. Can not migrate records. ");
                }
                if (!IsTextFileEmpty(AvatarsToAdjustDBRadiusPath))
                {
                    if (!migrated)
                    {
                        NDBConfig.avatarsToAdjustDBRadius = new Dictionary<string, int>(File.ReadAllLines(AvatarsToAdjustDBRadiusPath).Select(s => s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).Where(x => x.Length == 2).ToDictionary(p => p[0].Trim(), p => Int32.Parse(p[1].Trim())));
                        LogDebug(ConsoleColor.DarkBlue, "Loaded - " + AvatarsToAdjustDBRadiusPath);
                    }
                }
                else NDBConfig.avatarsToAdjustDBRadius = new Dictionary<string, int>();
            }
            catch (Exception ex)
            {
                LogDebugError("Possible corrupted file: " + AvatarsToAdjustDBRadiusPath + " File will be renamed and new file created \n" + ex.ToString());
                NDBConfig.avatarsToAdjustDBRadius = new Dictionary<string, int>();
                File.Move(AvatarsToAdjustDBRadiusPath, AvatarsToAdjustDBRadiusPath + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss") + ".bkp");
            }
            //======
            migrated = false;
            try
            {
                if (MelonPreferences.GetEntryValue<string>("NDB", "AvatarsToAddColliders") != "")
                {//If modprefs has value, then convert into standlone file 
                    if (IsTextFileEmpty(AvatarsToAddCollidersPath))
                    {//Check if file already had content, if so, abort and error 
                        NDBConfig.avatarsToAddColliders = new Dictionary<string, bool>(MelonPreferences.GetEntryValue<string>("NDB", "AvatarsToAddColliders").Split(';').Select(s => s.Split(',')).ToDictionary(p => p[0].Trim(), p => bool.Parse(p[1].Trim())));
                        File.WriteAllLines(AvatarsToAddCollidersPath, NDBConfig.avatarsToAddColliders.Select(p => string.Format("{0}, {1}", p.Key, p.Value)), Encoding.UTF8); //Save file
                        MelonPreferences.SetEntryValue<string>("NDB", "AvatarsToAddColliders", "");
                        migrated = true;
                        LogDebug(ConsoleColor.Blue, "Migrated from MelonPreferences to " + AvatarsToAddCollidersPath);
                    }
                    else LogDebug(ConsoleColor.Red, "MelonPreferences has content but " + AvatarsToAddCollidersPath + " is not empty. Can not migrate records. ");
                }
                if (!IsTextFileEmpty(AvatarsToAddCollidersPath))
                {
                    if (!migrated)
                    {
                        NDBConfig.avatarsToAddColliders = new Dictionary<string, bool>(File.ReadAllLines(AvatarsToAddCollidersPath).Select(s => s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).Where(x => x.Length == 2).ToDictionary(p => p[0].Trim(), p => bool.Parse(p[1].Trim())));
                        LogDebug(ConsoleColor.DarkBlue, "Loaded - " + AvatarsToAddCollidersPath);
                    }

                }
                else NDBConfig.avatarsToAddColliders = new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                LogDebugError("Possible corrupted file: " + AvatarsToAddCollidersPath + " File will be renamed and new file created  \n" + ex.ToString());
                NDBConfig.avatarsToAddColliders = new Dictionary<string, bool>();
                File.Move(AvatarsToAddCollidersPath, AvatarsToAddCollidersPath + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss") + ".bkp");
            }
        }


        private void MigrateOrLoadHashSet(string MelonPrefName, string FilePath, ref HashSet<string> Config)
        {
            try
            {
                var migrated = false;
                if (MelonPreferences.GetEntryValue<string>("NDB", MelonPrefName) != "")
                {//If modprefs has value, then convert into standalone file 
                    if (IsTextFileEmpty(FilePath))
                    {//Check if file already had content, if so, abort and error 
                        Config = new HashSet<string>(MelonPreferences.GetEntryValue<string>("NDB", MelonPrefName).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        File.WriteAllLines(FilePath, Config, Encoding.UTF8); //Save file
                        MelonPreferences.SetEntryValue<string>("NDB", MelonPrefName, "");
                        migrated = true;
                        LogDebug(ConsoleColor.Blue, "Migrated from MelonPreferences to " + FilePath);
                    }
                    else LogDebug(ConsoleColor.Red, $"MelonPreferences has content but {FilePath} is not empty. Can not migrate records. ");
                }
                if (!IsTextFileEmpty(FilePath))
                {
                    if (!migrated)
                    {
                        Config = new HashSet<string>(File.ReadAllLines(FilePath).Where(x => !string.IsNullOrWhiteSpace(x))); //Remove Blank Lines
                        LogDebug(ConsoleColor.DarkBlue, "Loaded - " + FilePath);
                    }
                }
                else Config = new HashSet<string>();
            }
            catch (Exception ex)
            {
                LogDebugError("Possible corrupted file: " + FilePath + " File will be renamed and new file created  \n" + ex.ToString());
                Config = new HashSet<string>();
                File.Move(FilePath, FilePath + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss") + ".bkp");
            }
        }


        public static bool IsTextFileEmpty(string fileName)
        { //https://stackoverflow.com/a/58123228
            var info = new FileInfo(fileName);
            if (info.Length == 0)
                return true;
            // only if your use case can involve files with 1 or a few bytes of content.
            if (info.Length < 6)
            {
                var content = File.ReadAllText(fileName);
                return content.Length == 0;
            }
            return false;
        }

        private void SaveListFiles()
        {
            try
            {
                //if (NDBConfig.avatarsToWhichNotApply != null) File.WriteAllLines(AvatarsToWhichNotApplyPath, NDBConfig.avatarsToWhichNotApply, Encoding.UTF8);
                if (NDBConfig.avatarsToWhichNotApply != null) File.WriteAllLines(AvatarsToWhichNotApplyPath, NDBConfig.avatarsToWhichNotApply.Select(p => string.Format("{0}, {1}", p.Key, p.Value)), Encoding.UTF8);
                if (NDBConfig.bonesToExclude != null) File.WriteAllLines(BonesToExcludePath, NDBConfig.bonesToExclude, Encoding.UTF8);
                if (NDBConfig.collidersToExclude != null) File.WriteAllLines(CollidersToExcludePath, NDBConfig.collidersToExclude, Encoding.UTF8);
                if (NDBConfig.bonesToAlwaysExclude != null) File.WriteAllLines(BonesToAlwaysExcludePath, NDBConfig.bonesToAlwaysExclude, Encoding.UTF8);
                if (NDBConfig.bonesToInclude != null) File.WriteAllLines(BonesToIncludePath, NDBConfig.bonesToInclude, Encoding.UTF8);
                if (NDBConfig.collidersToInclude != null) File.WriteAllLines(CollidersToIncludePath, NDBConfig.collidersToInclude, Encoding.UTF8);
                if (NDBConfig.avatarsToAdjustDBRadius != null) File.WriteAllLines(AvatarsToAdjustDBRadiusPath, NDBConfig.avatarsToAdjustDBRadius.Select(p => string.Format("{0}, {1}", p.Key, p.Value)), Encoding.UTF8);
                if (NDBConfig.avatarsToAddColliders != null) File.WriteAllLines(AvatarsToAddCollidersPath, NDBConfig.avatarsToAddColliders.Select(p => string.Format("{0}, {1}", p.Key, p.Value)), Encoding.UTF8);
            }
            catch (Exception ex) { LogDebugError(ex.ToString()); return; }
        }


        public static bool Reset()
        {
            try
            {
                _Instance.originalSettings = new Dictionary<string, List<OriginalBoneInformation>>();
                _Instance.avatarsInScene = new Dictionary<string, System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>>>();
                _Instance.avatarRenderers = new Dictionary<string, System.Tuple<Renderer, DynamicBone[]>>();
                _Instance.localPlayer = null;
                NDB.bonesExcluded.Clear();
                NDB.collidersExcluded.Clear();
                NDB.bonesIncluded.Clear();
                NDB.collidersIncluded.Clear();

                moarbonesCount = 0;

                //Console.WriteLine("ONJOINEDROOM PAST-CALLBACK");
                LogDebug(ConsoleColor.Blue, "New scene loaded; reset");
                //Console.WriteLine("ONJOINEDROOM SUCCESS");
            }
            catch (Exception e)
            {
                LogDebugError(e.ToString());
            }
            return true;
        }

        private static void OnPlayerLeft(Player player)
        {
            if (player.transform.root.gameObject.name.Contains("[Local]"))
            {
                LogDebugInt(2, ConsoleColor.Red, $"OnPlayerLeft: Not removing local player info");
                return;
            }

            if (!_Instance.avatarsInScene.ContainsKey(player._vrcplayer.prop_String_0) && !_Instance.originalSettings.ContainsKey(player._vrcplayer.prop_String_0))
            {
                LogDebugInt(2, ConsoleColor.Red, $"OnPlayerLeft: Just passing to onPlayerLeftDelegate");
                //Console.WriteLine("ONPLAYERLEFT PAST-CALLBACK");
                return;

            }

            _Instance.RemoveBonesOfGameObjectInAllPlayers(_Instance.avatarsInScene[player._vrcplayer.prop_String_0].Item4);
            _Instance.DeleteOriginalColliders(player._vrcplayer.prop_String_0);
            _Instance.RemovePlayerFromDict(player._vrcplayer.prop_String_0);
            _Instance.RemoveDynamicBonesFromVisibilityList(player._vrcplayer.prop_String_0);
            LogDebugInt(0, ConsoleColor.Blue, $"Player {player._vrcplayer.prop_String_0} left the room so all his dynamic bones info was deleted");
            //Console.WriteLine("ONPLAYERLEFT SUCCESS");
        }

        private static void OnAvatarInstantiated(VRCAvatarManager __instance, GameObject __0, ref Il2CppSystem.Delegate __2)
        {
            //Console.WriteLine($"ONAVATARINSTANTIATED START");
            //onAvatarInstantiatedDelegate(@this, avatarPtr, avatarDescriptorPtr, loaded);
            //Console.WriteLine("ONAVATARINSTANTIATED PAST-CALLBACK");

            try
            { 
                if (__instance != null) // && __instance.prop_GameObject_0 != null | I should add, as I think this is causing null Item1's....
                {
                    GameObject avatar = __instance.prop_GameObject_0;
                    //VRC.SDKBase.VRC_AvatarDescriptor avatarDescriptor = new VRC.SDKBase.VRC_AvatarDescriptor(avatarDescriptorPtr);

                    if (avatar.transform.root.gameObject.name.Contains("[Local]"))//Remove broken DB's on local avatar only? 
                    {
                        avatar.GetComponentsInChildren<DynamicBone>(true).Do(b => { if (b.m_Root == null) UnityEngine.Object.Destroy(b); });
                        _Instance.localPlayer = avatar;
                        try
                        {
                            _Instance.localPlayerDBbyRootName = avatar.GetComponentsInChildren<DynamicBone>().ToDictionary((b) => b.GetInstanceID().ToString());
                        }
                        catch (Exception ex) { LogDebugError(ex.ToString()); }
                    }
                    if (NDBConfig.moarBones) MelonCoroutines.Start(MoarBones(avatar));
                    float scaleArmature = 1f;
                    GameObject armature = GetChildObject("Armature", avatar); //Change to something better than just a named check?
                    if (armature != null)
                    {
                        scaleArmature = ((armature.transform.localScale.x + armature.transform.localScale.y + armature.transform.localScale.z) / 3);
                    }
                    else LogDebugInt(1, ConsoleColor.Yellow, $"Armature not found for scale");
                    string aviName = avatar.transform.root.GetComponentInChildren<VRCPlayer>().prop_ApiAvatar_0.name;
                    string aviID = avatar.transform.root.GetComponentInChildren<VRCPlayer>().prop_ApiAvatar_0.id;
                    string aviHash = aviName.Substring(0, Math.Min(aviName.Length, 20)) + ":" + String.Format("{0:X}", aviID.GetHashCode()).Substring(4);
                    LogDebugInt(1, ConsoleColor.Yellow, $"Avatar: {aviName}, ID: {aviID}");
                    AddAutoCollidersToPlayer(avatar, aviHash);
                    _Instance.AddOrReplaceWithCleanup(
                        avatar.transform.root.GetComponentInChildren<VRCPlayer>().prop_String_0,
                        new System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>>(
                            avatar,
                            avatar.transform.root.GetComponentInChildren<VRCPlayer>().prop_VRCPlayerApi_0.IsUserInVR(),
                            avatar.GetComponentsInChildren<DynamicBone>(),
                            avatar.GetComponentsInChildren<DynamicBoneCollider>(),
                            APIUser.IsFriendsWith(avatar.transform.root.GetComponentInChildren<Player>().prop_APIUser_0.id),
                            new System.Tuple<string, string, float>(
                            aviName,
                            aviHash,
                            scaleArmature
                            )
                        ));

                    LogDebugInt(0, ConsoleColor.Blue, "New avatar loaded, added to avatar list");
                    LogDebugInt(0, ConsoleColor.Green, $"Added {avatar.transform.root.GetComponentInChildren<VRCPlayer>().prop_String_0}");
                }
            }
            catch (System.Exception ex)
            {
                LogDebugError("An exception was thrown while working!\n" + ex.ToString());
            }

            //Console.WriteLine("ONAVATARINSTANTIATED SUCCESS");
        }

        private static IEnumerator MoarBones(GameObject avatar)
        {
            yield return new WaitForSeconds(5f); //Wait till avatar loads fully
            try
            {
                LogDebug(ConsoleColor.Magenta, $"~~~~~~~~~~~~~~~Moarbones~~~~~~~~~~~~~~~");
                LogDebug(ConsoleColor.Magenta, $"This makes ALL bones dynamic and can be disabled in Mod Settings:\n'MoarBones: I hear you like bones~' {(NDBConfig.moarBonesPrefLimit ? "\nPerformance Limit Enabled in Mod Settings, limiting to first 10 avatars loaded" : "\nPerformance Limit DISABLED in Mod Settings, applying MoarBones for every avatar")}");

                if (NDBConfig.moarBonesPrefLimit && moarbonesCount > 10) yield break;
                if (NDBConfig.moarBonesNotLocal && avatar.transform.root.gameObject.name.Contains("[Local]")) yield break;
                moarbonesCount++;
                if (!avatar?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? false) yield break;
                if (avatar.transform.root.GetComponentInChildren<VRC.DynamicBoneController>() is null) yield break;
                //foreach (DynamicBone db in avatar.GetComponentsInChildren<DynamicBone>())
                // {
                //UnityEngine.Object.Destroy(db);
                // }
                Transform Hips = avatar.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Hips);

                var flat = new AnimationCurve();
                flat.AddKey(0f, 1f);
                flat.AddKey(1f, 1f);

                DynamicBone newBone = Hips.gameObject.AddComponent<DynamicBone>();
                newBone.m_Root = Hips;
                newBone.m_Damping = 0.05f; newBone.m_Elasticity = 0.02f; newBone.m_Stiffness = 0.2f; newBone.m_Inert = 0.2f;
                newBone.m_Radius = 0.05f / ((Hips.transform.lossyScale.x + Hips.transform.lossyScale.y + Hips.transform.lossyScale.z) / 3);
                newBone.enabled = true;
                newBone.field_Private_Single_4 = 60f; // This appears to drive m_UpdateRate
                newBone.m_UpdateRate = 60f;
                newBone.m_DistantDisable = false;
                newBone.m_Colliders = new Il2CppSystem.Collections.Generic.List<DynamicBoneCollider>(); newBone.m_Exclusions = new Il2CppSystem.Collections.Generic.List<Transform>();
                newBone.m_DampingDistrib = flat; newBone.m_ElasticityDistrib = flat; newBone.m_StiffnessDistrib = flat; newBone.m_InertDistrib = flat; newBone.m_RadiusDistrib = flat;

                var dbList = avatar.transform.root.GetComponentInChildren<VRC.DynamicBoneController>().field_Private_List_1_DynamicBone_0;
                dbList.Add(newBone);
                avatar.transform.root.GetComponentInChildren<VRC.DynamicBoneController>().Update();

                newBone.Method_Public_Void_PDM_0();//Start - was Method_Public_Void_PDM_1, then Method_Public_Void_PDM_0
                //For some reason needed twice now
                newBone.m_Root = Hips;
                newBone.m_Damping = 0.05f; newBone.m_Elasticity = 0.02f; newBone.m_Stiffness = 0.2f; newBone.m_Inert = 0.2f;
                newBone.m_Radius = 0.05f / ((Hips.transform.lossyScale.x + Hips.transform.lossyScale.y + Hips.transform.lossyScale.z) / 3);
                newBone.enabled = true;
                newBone.field_Private_Single_4 = 60f; // This appears to drive m_UpdateRate
                newBone.m_UpdateRate = 60f;
                newBone.m_DistantDisable = false;
                newBone.m_Colliders = new Il2CppSystem.Collections.Generic.List<DynamicBoneCollider>(); newBone.m_Exclusions = new Il2CppSystem.Collections.Generic.List<Transform>();
                newBone.m_DampingDistrib = flat; newBone.m_ElasticityDistrib = flat; newBone.m_StiffnessDistrib = flat; newBone.m_InertDistrib = flat; newBone.m_RadiusDistrib = flat;
                newBone.Method_Private_Void_6();//InitTransforms - Reset positions before adding new particles. If you don't do this the new particle will be offest from the bone
                newBone.Method_Private_Void_5();//SetupParticles
            }
            catch (System.Exception ex) { LogDebugError(ex.ToString()); }
        }

        public void AddOrReplaceWithCleanup(string key, System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>> newValue)
        {
            foreach (DynamicBoneCollider col in newValue.Item4)
            {
                if (NDBConfig.destroyInsideColliders && col.m_Bound == DynamicBoneCollider.EnumNPublicSealedvaOuIn3vUnique.Inside)
                {
                    newValue.Item3.Do((b) => b.m_Colliders.Remove(col));
                    LogDebug(ConsoleColor.Yellow, $"Removing bone {col.transform.name} because settings disallow inside colliders");
                    UnityEngine.Object.Destroy(col);
                }
            }



            if (!avatarsInScene.ContainsKey(key ?? ""))
            {
                SaveOriginalColliderList(key, newValue.Item3);
                AddToPlayerDict(key, newValue);
            }
            else
            {
                DeleteOriginalColliders(key);
                SaveOriginalColliderList(key, newValue.Item3);
                DynamicBoneCollider[] oldColliders = avatarsInScene[key].Item4;
                RemovePlayerFromDict(key);
                AddToPlayerDict(key, newValue);
                RemoveBonesOfGameObjectInAllPlayers(oldColliders);
                RemoveDynamicBonesFromVisibilityList(key);
                LogDebugInt(0, ConsoleColor.Blue, $"User {key} swapped avatar, system updated");
            }
            if (enabled) AddCollidersToAllPlayers(newValue);
            if (newValue.Item1 != localPlayer)
            {
                GameObject bodyObj = GetChildObject("Body", newValue.Item1);
                SkinnedMeshRenderer bodyMesh = null;
                if (bodyObj != null) bodyMesh = bodyObj.GetComponentInChildren<SkinnedMeshRenderer>();
                if (bodyMesh != null && bodyMesh.enabled == true && bodyMesh.gameObject.active == true)
                {
                    AddDynamicBonesToVisibilityList(key, newValue.Item3, bodyMesh);
                }
                else
                {
                    LogDebugInt(1, ConsoleColor.Yellow, "Avatar has no active 'Body' mesh. Finding largest active SkinnedMeshRenderer on Avatar for Visibility");

                    //Find the largest mesh that is active and enabled 
                    SkinnedMeshRenderer[] meshes = newValue.Item1.GetComponentsInChildren<SkinnedMeshRenderer>();
                    SkinnedMeshRenderer visMesh = null;
                    float meshSize = 0f;
                    if (meshes != null && !meshes.Equals(null) && meshes.Length > 0)
                    {
                        try
                        {
                            foreach (SkinnedMeshRenderer mesh in meshes)
                            {
                                try
                                {
                                    if (mesh is null || mesh.Equals(null)) continue;
                                    LogDebugInt(1, ConsoleColor.DarkMagenta, $"Mesh - {mesh.name}, Size - {mesh.bounds.size.magnitude.ToString("F5").TrimEnd('0')}, Enabled - {mesh.enabled}, Active - {mesh.gameObject.active} ");
                                    if (mesh.bounds.size.magnitude > meshSize && mesh.enabled == true && mesh.gameObject.active == true)
                                    {
                                        meshSize = mesh.bounds.size.magnitude;
                                        visMesh = mesh;
                                    }
                                }
                                catch (System.Exception ex) { LogDebug(ConsoleColor.DarkRed, $"Error in visMesh\n" + ex.ToString()); }
                            }
                            LogDebugInt(1, ConsoleColor.Yellow, $"Largest mesh - {visMesh.name}  is {visMesh.bounds.size.magnitude.ToString("F5").TrimEnd('0')} ");
                        }
                        catch (System.Exception ex) { LogDebug(ConsoleColor.DarkRed, $"Error in meshes foreach\n" + ex.ToString()); }
                    }

                    if (visMesh != null)
                    {
                        AddDynamicBonesToVisibilityList(key, newValue.Item3, visMesh);
                    }
                    else
                    {
                        if (newValue.Item1.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                        {
                            AddDynamicBonesToVisibilityList(key, newValue.Item3, newValue.Item1.GetComponentInChildren<SkinnedMeshRenderer>());
                            LogDebugInt(1, ConsoleColor.Yellow, "Defaulting to first SkinnedMeshRender on avatar");

                        }
                        LogDebugInt(1, ConsoleColor.Yellow, "No SkinnedMeshes found, not added to Visibility List. ");
                    }
                }

            }
        }

        private static GameObject GetChildObject(string childName, GameObject parent)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;
                if (child.name.Equals(childName))
                {
                    return child;
                }
            }
            return null;
        }

        private void OnDrawGizmosSelected(DynamicBone db)
        {//Skeleton for visulization, 
            if (!enabled || db.m_Root == null)
                return;

            db.Method_Private_Void_6();//InitTransforms - Reset positions before adding new particles. If you don't do this the new particle will be offest from the bone
            db.Method_Private_Void_5();//SetupParticles

            Gizmos.color = Color.white;
            for (int i = 0; i < db.field_Private_List_1_ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique_0.Count; ++i) //m_Particles
            {
                DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique p = db.field_Private_List_1_ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique_0[i];
                if (p.field_Public_Int32_0 >= 0) //m_ParentIndex
                {
                    DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique p0 = db.field_Private_List_1_ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique_0[p.field_Public_Int32_0]; //Particle p0 = m_Particles[p.m_ParentIndex];
                    Gizmos.DrawLine(p.field_Public_Vector3_0, p0.field_Public_Vector3_0); //m_Position
                }
                if (p.field_Public_Single_4 > 0) // if (p.m_Radius > 0)
                    Gizmos.DrawWireSphere(p.field_Public_Vector3_0, p.field_Public_Single_4 * db.field_Private_Single_1); //Gizmos.DrawWireSphere(p.m_Position, p.m_Radius * m_ObjectScale);
            }
        }


        private void ApplyBoneSettings(DynamicBone bone, string avatarHash)
        {
            try
            {
                if (bone == null || bone.Equals(null)) return;
                if (NDBConfig.dynamicBoneUpdateRate != bone.m_UpdateRate && NDBConfig.dynamicBoneUpdateRateAdjSettings)
                {
                    LogDebugInt(2, ConsoleColor.Magenta, $"Bone {bone.name}'s update rate of {bone.m_UpdateRate} doesn't match NDB config of {NDBConfig.dynamicBoneUpdateRate} - Applying settings changes");
                    bone.m_Elasticity = bone.m_Elasticity * (bone.m_UpdateRate / NDBConfig.dynamicBoneUpdateRate);
                    bone.m_Stiffness = bone.m_Stiffness * (NDBConfig.dynamicBoneUpdateRate / bone.m_UpdateRate);
                    bone.m_Damping = bone.m_Damping * (NDBConfig.dynamicBoneUpdateRate / bone.m_UpdateRate);
                    bone.m_Inert = bone.m_Inert * (NDBConfig.dynamicBoneUpdateRate / bone.m_UpdateRate);
                }
                bone.m_DistantDisable = NDBConfig.distanceDisable;
                bone.m_DistanceToObject = NDBConfig.distanceToDisable;
                bone.field_Private_Single_4 = NDBConfig.dynamicBoneUpdateRate; // This appears to drive m_UpdateRate - is m_BaseUpdateRate
                bone.m_UpdateRate = NDBConfig.dynamicBoneUpdateRate; //Setting both values should make the UpdateRate match instantly, otherwise if lower then default, it will slowly skew to the new UpdateRate
                if (!localPlayer.Equals(null) && !localPlayer.transform.Equals(null)) bone.m_ReferenceObject = localPlayer.transform; //= localPlayer?.transform ?? bone.m_ReferenceObject;  //Null null null null
                if (!NDBConfig.onlyOptimize) ApplyDBRadius(bone, avatarHash); //Dont adjust radius if we aren't multiplayering 
                ApplyBoneChanges(bone);
            }
            catch (Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); }
        }
        private static void ApplyDBRadius(DynamicBone bone, string avatarHash) //Change loglevels in here to 2 in a latter version 
        {
            try
            {                                   //If (Has Key OR adjustRadiusForAllZeroBones) && m_root is NOT null && Keyvalue is NOT -2 (Exclude from Radius change) 
                if ((NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash) || NDBConfig.adjustRadiusForAllZeroBones) && !(bone.m_Root is null) && !(NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash) && NDBConfig.avatarsToAdjustDBRadius[avatarHash] == -2))
                {
                    if (NDBConfig.adjustRadiusExcludeZero && bone.m_Radius == 0) { LogDebugInt(1, ConsoleColor.Yellow, $"Bone {bone.m_Root.name} has a radius of 0 and adjustRadiusExcludeZero is True in Mod settings"); return; }

                    int adj = 0;
                    if (NDBConfig.avatarsToAdjustDBRadius.ContainsKey(avatarHash)) adj = NDBConfig.avatarsToAdjustDBRadius[avatarHash];
                    else
                    { // adjustRadiusForAllZeroBones = True
                        adj = -1;
                        if (bone.m_Radius != 0) return; //Exclude bones that have a radius
                    }
                    float scale = bone.field_Private_Single_1; // m_ObjectScale 
                    float boneTotalLength = bone.field_Private_Single_0; // m_BoneTotalLength 
                    float orgRad = -1;
                    string playerKey = bone.transform.root.GetComponentInChildren<VRCPlayer>().prop_String_0;
                    _Instance.originalSettings.TryGetValue(playerKey, out List<OriginalBoneInformation> origList);
                    origList.DoIf((x) => ReferenceEquals(x.referenceToOriginal, bone), (origData) =>
                    {
                        orgRad = origData.Radius;
                    });

                    LogDebugInt(1, ConsoleColor.Yellow, $"Bone Legnth {boneTotalLength.ToString("F5").TrimEnd('0')}, scale {scale.ToString("F5").TrimEnd('0')}, orig rad {orgRad.ToString("F5").TrimEnd('0')}");

                    if (adj == 0 || adj == -1) //Replacing length with calculated one
                    {
                        //if (bone.m_Root.transform.GetChildCount() == 0) bone.m_Radius = NDBConfig.endBoneRadius / scale; //No child bones means we can't measure anything
                        if (bone.m_Root.transform.GetChildCount() == 0) bone.m_Radius = (boneTotalLength) / scale; //No child bones means we can't measure anything
                        else
                        {
                            float distance = 0;

                            int depth = GetChildDepth(bone.m_Root, 1);
                            float distance1 = boneTotalLength / depth;
                            float distance2 = Vector3.Distance(bone.m_Root.transform.position, bone.m_Root.transform.GetChild(0).position);

                            distance = Math.Max(distance1, distance2);
                            LogDebugInt(1, ConsoleColor.Yellow, $"dist1: {distance1.ToString("F5").TrimEnd('0')} (length {boneTotalLength.ToString("F5").TrimEnd('0')}/depth {depth.ToString("F5").TrimEnd('0')}, dist2 {distance2.ToString("F5").TrimEnd('0')}, Max {distance.ToString("F5").TrimEnd('0')}");
                            bone.m_Radius = (distance / NDBConfig.boneRadiusDivisor) / scale;
                        }

                        if (bone.m_Radius == 0) bone.m_Radius = NDBConfig.endBoneRadius / scale; //If 0 still, set default

                        LogDebugInt(1, ConsoleColor.DarkYellow, $"DB Radius replaced for avatar {avatarHash}, Bone {bone.m_Root.name}, Was: {(orgRad * scale).ToString("F5").TrimEnd('0')}, Now: {(bone.m_Radius * scale).ToString("F5").TrimEnd('0')}");
                    }
                    else if (orgRad != -1)//Multiply existing radius
                    {
                        float radMuti = ((float)adj / 10f);
                        bone.m_Radius *= radMuti;

                        if (orgRad == 0) bone.m_Radius = NDBConfig.endBoneRadius / scale; //If 0 still, set default
                        LogDebugInt(1, ConsoleColor.DarkYellow, $"DB Radius adjusted for avatar {avatarHash}, Bone {bone.m_Root.name}, Was: {(orgRad * scale).ToString("F5").TrimEnd('0')}, Now: {(bone.m_Radius * scale).ToString("F5").TrimEnd('0')}, Multi: {radMuti}");
                    }
                    else LogDebugInt(1, ConsoleColor.Red, $"DB Radius was tagged to be adjusted but was aborted. Was not set to replace, and could not find org radius to be multiplied.");

                }
            }
            catch (Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); }
        }

        private static void ApplyBoneChanges(DynamicBone bone)
        {
            bone.Method_Private_Void_6();//InitTransforms - Reset positions before adding new particles. If you don't do this the new particle will be offest from the bone
            bone.Method_Private_Void_5();//SetupParticles
        }

        private static int GetChildDepth(Transform parent, int depth)
        {
            if (depth < 1) depth = 1;//1 cause we count the first transform this comes from
            if (parent.childCount >= 1)
            {
                depth++;
                depth = GetChildDepth(parent.GetChild(0), depth);
                return depth;
            }
            return depth;
        }

        private void AddAllCollidersToAllPlayers()
        {
            foreach (System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>> player in avatarsInScene.Values)
            {
                LogDebugInt(1, ConsoleColor.Red, $"AddAllCollidersToAllPlayers - {player.Item6.Item1}");
                AddCollidersToAllPlayers(player);
            }
        }
        //Item1 - avatar, Item2 - IsInVR, Item3 - DB[], Item4 - DBC[], Item5 - IsFriends, Item6.Item1 - AvatarName, Item6.Item2 - AvatarName+IDhash, Item6.Item3 - scaleArmature
        private void AddCollidersToAllPlayers(System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>> player)
        {
            LogDebugInt(2, ConsoleColor.Cyan, $"AddCollidersToAllPlayers - player.IsInVR: {player.Item2}, player.IsFriends: {player.Item5}, player.AvatarName+IDhash: {player.Item6.Item2}, player.scaleArm: {player.Item6.Item3}");
            if (NDBConfig.disableAllBones) { DisableAllBones(player.Item3); return; }
            foreach (var bone in player.Item3)
            {
                ApplyBoneSettings(bone, player.Item6.Item2);
            }
            if (NDBConfig.onlyOptimize) return;

            //Players that we want to be excluded from MDB completely, their db and dbc will never be touched. (Also filtered in otherPlayerInfo foreach's below)
            //onlyForMyBones is not included here due to a rare issue which causes the localplayer's dbc to not be added to others if they join a world with only one other person in it until the mod is toggled or they switches avatars.
            if (NDBConfig.avatarsToWhichNotApply.ContainsKey(player.Item6.Item2) && NDBConfig.avatarsToWhichNotApply[player.Item6.Item2] == true)
            { LogDebug(ConsoleColor.Blue, $"Avatar '{player.Item6.Item2}' disabled in quick menu"); return; }
            bool includeAvatar = false;
            if (NDBConfig.avatarsToWhichNotApply.ContainsKey(player.Item6.Item2) && NDBConfig.avatarsToWhichNotApply[player.Item6.Item2] == false)
            { includeAvatar = true; LogDebug(ConsoleColor.Blue, $"Avatar '{player.Item6.Item2}' specifically enabled in quick menu, bypassing checks"); }
            if (NDBConfig.disallowDesktoppers && !includeAvatar && !player.Item2 && player.Item1 != localPlayer)
            { LogDebugInt(2, ConsoleColor.Red, $"Filtered r - disallowDesktoppers.True Player is Desktop & Not localplayer(me)"); return; }
            if (NDBConfig.onlyForMeAndFriends && !includeAvatar && !player.Item5 && player.Item1 != localPlayer)
            { LogDebugInt(2, ConsoleColor.Red, $"Filtered r - onlyForMeAndFriends.True Player is not a friend & Not localplayer(me)"); return; }

            foreach (var otherPlayerInfo in avatarsInScene.Values)
            {
                LogDebugInt(3, ConsoleColor.White, "Adding Player's dbc to OtherPlayer's db: " + otherPlayerInfo.Item6.Item1);
                try
                {

                    if (NDBConfig.avatarsToWhichNotApply.ContainsKey(otherPlayerInfo.Item6.Item2) && NDBConfig.avatarsToWhichNotApply[otherPlayerInfo.Item6.Item2] == true)
                    { LogDebugInt(2, ConsoleColor.Blue, $"Filtered c.1 - Avatar '{otherPlayerInfo.Item6.Item2}' disabled in quick menu - 1.2"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"0.1");
                    bool includeAvatarOther = false;
                    if (NDBConfig.avatarsToWhichNotApply.ContainsKey(otherPlayerInfo.Item6.Item2) && NDBConfig.avatarsToWhichNotApply[otherPlayerInfo.Item6.Item2] == false)
                    { includeAvatarOther = true; LogDebugInt(2, ConsoleColor.Blue, $"Avatar '{otherPlayerInfo.Item6.Item2}' specifically enabled in quick menu, bypassing checks - 1.2"); } LogDebugInt(5, ConsoleColor.DarkRed, $"0.2");
                    if (NDBConfig.disallowDesktoppers && !includeAvatarOther && !otherPlayerInfo.Item2 && otherPlayerInfo.Item1 != localPlayer)
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.1 - disallowDesktoppers.True OtherPlayer is Desktop & Not localplayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"0.3");
                    if (NDBConfig.onlyForMeAndFriends && !includeAvatarOther && !otherPlayerInfo.Item5 && otherPlayerInfo.Item1 != localPlayer)
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.1 - onlyForMeAndFriends.True OtherPlayer is not a friend & Not localplayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"0.4");

                    if (!NDBConfig.interactSelf && (otherPlayerInfo.Item1 == player.Item1) && (otherPlayerInfo.Item1 == localPlayer))
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.1 - interactSelf.False OtherPlayer is the same as Player && is localplayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"0.5");
                    if (!NDBConfig.othersInteractSelf && (otherPlayerInfo.Item1 == player.Item1) && !(otherPlayerInfo.Item1 == localPlayer))
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.1 - othersInteractSelf.False OtherPlayer is the same as Player && Not localplayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"0.6");


                    if ((NDBConfig.onlyForMyBones && !includeAvatar && player.Item1 != localPlayer))
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.1 - onlyForMyBones.True Player is not localPlayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"0.7");

                    foreach (DynamicBone otherPlayerDynamicBone in otherPlayerInfo.Item3)
                    {
                        LogDebugInt(5, ConsoleColor.DarkRed, $"B1");
                        bool includeBone = CheckIfBoneIncluded(otherPlayerInfo.Item6.Item2, otherPlayerDynamicBone);
                        LogDebugInt(5, ConsoleColor.DarkRed, $"B11");
                        if (otherPlayerDynamicBone != null && !otherPlayerDynamicBone.Equals(null) && otherPlayerDynamicBone.m_Root != null && !otherPlayerDynamicBone.m_Root.Equals(null)) 
                            LogDebugInt(4, ConsoleColor.Yellow, "OtherPlayer db - " + otherPlayerDynamicBone.m_Root.name);
                        try
                        {
                            LogDebugInt(5, ConsoleColor.DarkRed, $"1");
                            if (NDBConfig.breastsOnly && !includeBone && (otherPlayerDynamicBone?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? false) && !otherPlayerDynamicBone.m_Root.Equals(null) && !otherPlayerDynamicBone.gameObject.Equals(null) && !(
                            otherPlayerDynamicBone.m_Root.transform.IsChildOf(otherPlayerDynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Chest)) &&
                            !otherPlayerDynamicBone.m_Root.transform.IsChildOf(otherPlayerDynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Neck)) &&
                            !otherPlayerDynamicBone.m_Root.transform.IsChildOf(otherPlayerDynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftShoulder)) &&
                            !otherPlayerDynamicBone.m_Root.transform.IsChildOf(otherPlayerDynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightShoulder))))
                            { LogDebugInt(4, ConsoleColor.Red, $"Filtered c.1 - breastsOnly.True Otherplayer db is not a child of the chest || is not also a child of neck/shoulders"); continue; }
                            
                            foreach (DynamicBoneCollider collider in player.Item4)
                            {
                                try
                                {
                                    LogDebugInt(5, ConsoleColor.DarkRed, $"2");
                                    bool includeCollider = CheckIfColliderIncluded(player.Item6.Item2, collider);
                                    LogDebugInt(5, ConsoleColor.DarkRed, $"22");
                                    if (collider != null && !collider.Equals(null) && collider.gameObject != null && !collider.gameObject.Equals(null)) LogDebugInt(4, ConsoleColor.Yellow, "Player dbc - " + collider.name);
                                    LogDebugInt(5, ConsoleColor.DarkRed, $"3");
                                    if (NDBConfig.onlyHandColliders && !includeCollider && (player?.Item1?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? false) && !collider.Equals(null) && !collider.gameObject.Equals(null) && !(collider.transform.IsChildOf(player.Item1.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftHand) ?? collider.transform) || collider.transform.IsChildOf(player.Item1.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightHand) ?? collider.transform)))
                                    { LogDebugInt(4, ConsoleColor.Red, $"Filtered c.1 - onlyHandColliders.True Player's dbc is not on a hand"); continue; } // Added '?? collider.transform' as an attempt to stop an NRE if the model was human rigged, but missing hands

                                    AddColliderToBone(otherPlayerDynamicBone, collider, otherPlayerInfo.Item6.Item2, player.Item6.Item2);
                                }
                                catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in Player=>OtherPlayer - Foreach collider\n" + ex.ToString()); }
                            }
                        }
                        catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in Player=>OtherPlayer - Foreach otherPlayerDynamicBone\n" + ex.ToString()); }
                    }
                }
                catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in Player=>OtherPlayer - Foreach OtherPlayer\n" + ex.ToString()); }
            }

            
            foreach (var otherPlayerInfo in avatarsInScene.Values)
            {
                LogDebugInt(3, ConsoleColor.White, "Adding OtherPlayer's dbc to Player's db: " + otherPlayerInfo.Item6.Item1);
                try
                {
                    if (NDBConfig.avatarsToWhichNotApply.ContainsKey(otherPlayerInfo.Item6.Item2) && NDBConfig.avatarsToWhichNotApply[otherPlayerInfo.Item6.Item2] == true)
                    { LogDebugInt(2, ConsoleColor.Blue, $"Filtered c.2  - Avatar '{otherPlayerInfo.Item6.Item2}' disabled in quick menu - 2.2"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"00.1");
                    bool includeAvatarOther = false;
                    if (NDBConfig.avatarsToWhichNotApply.ContainsKey(otherPlayerInfo.Item6.Item2) && NDBConfig.avatarsToWhichNotApply[otherPlayerInfo.Item6.Item2] == false)
                    { includeAvatarOther = true; LogDebugInt(2, ConsoleColor.Blue, $"Avatar '{otherPlayerInfo.Item6.Item2}' specifically enabled in quick menu, bypassing checks - 2.2"); }  LogDebugInt(5, ConsoleColor.DarkRed, $"00.2");
                    if (NDBConfig.disallowDesktoppers && !includeAvatarOther && !otherPlayerInfo.Item2 && otherPlayerInfo.Item1 != localPlayer)
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.2  - disallowDesktoppers.True OtherPlayer is Desktop & Not localplayer(me)"); continue; }  LogDebugInt(5, ConsoleColor.DarkRed, $"00.3");
                    if (NDBConfig.onlyForMeAndFriends && !includeAvatarOther && !otherPlayerInfo.Item5 && otherPlayerInfo.Item1 != localPlayer)
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.2  - onlyForMeAndFriends.True OtherPlayer is not a friend & Not localplayer(me)"); continue; }  LogDebugInt(5, ConsoleColor.DarkRed, $"00.4");

                    if (!NDBConfig.interactSelf && (otherPlayerInfo.Item1 == player.Item1) && (otherPlayerInfo.Item1 == localPlayer))
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.2 - interactSelf.False OtherPlayer is the same as Player && is localplayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"00.5");
                    if (!NDBConfig.othersInteractSelf && (otherPlayerInfo.Item1 == player.Item1) && !(otherPlayerInfo.Item1 == localPlayer))
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.2 - othersInteractSelf.False OtherPlayer is the same as Player && Not localplayer(me)"); continue; } LogDebugInt(5, ConsoleColor.DarkRed, $"00.6");

                    if ((NDBConfig.onlyForMyBones && !includeAvatarOther && otherPlayerInfo.Item1 != localPlayer))
                    { LogDebugInt(3, ConsoleColor.Red, $"Filtered c.2  - onlyForMyBones.True OtherPlayer is not localPlayer(me)"); continue; }  LogDebugInt(5, ConsoleColor.DarkRed, $"00.7");

                    foreach (DynamicBoneCollider otherCollider in otherPlayerInfo.Item4)
                    {
                        LogDebugInt(5, ConsoleColor.DarkRed, $"B1.2");
                        bool includeCollider = CheckIfColliderIncluded(otherPlayerInfo.Item6.Item2, otherCollider);
                        LogDebugInt(5, ConsoleColor.DarkRed, $"B11.2");
                        if (otherCollider != null && !otherCollider.Equals(null) && otherCollider.gameObject != null && !otherCollider.gameObject.Equals(null)) 
                            LogDebugInt(4, ConsoleColor.Yellow, "OtherPlayer Collider - " + otherCollider.name);
                        try
                        {
                            LogDebugInt(5, ConsoleColor.DarkRed, $"1.2");
                            //if (NDBConfig.onlyHandColliders && !includeCollider && (otherPlayerInfo?.Item1?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? false) && !otherCollider.Equals(null) && !otherCollider.gameObject.Equals(null) && !(otherCollider.transform.IsChildOf(otherPlayerInfo?.Item1?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.GetBoneTransform(HumanBodyBones.LeftHand) ?? otherCollider.transform) || otherCollider.transform.IsChildOf(otherPlayerInfo?.Item1?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.GetBoneTransform(HumanBodyBones.RightHand) ?? otherCollider.transform)))
                            if (NDBConfig.onlyHandColliders && !includeCollider && otherPlayerInfo.Item1 != null && otherPlayerInfo?.Item1?.transform?.root?.GetComponentInChildren<VRCPlayer>() != null && otherPlayerInfo.Item1.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.isHuman && !otherCollider.Equals(null) && !otherCollider.gameObject.Equals(null) && !(otherCollider.transform.IsChildOf(otherPlayerInfo.Item1.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftHand) ?? otherCollider.transform) || otherCollider.transform.IsChildOf(otherPlayerInfo.Item1.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightHand) ?? otherCollider.transform)))
                            { LogDebugInt(4, ConsoleColor.Red, $"Filtered c.2  - onlyHandColliders.True OtherPlayer's dbc is not on a hand"); continue; }

                            foreach (DynamicBone dynamicBone in player.Item3)
                            {
                                try
                                {
                                    LogDebugInt(5, ConsoleColor.DarkRed, $"2.2");
                                    bool includeBone = CheckIfBoneIncluded(player.Item6.Item2, dynamicBone);
                                    LogDebugInt(5, ConsoleColor.DarkRed, $"22.2");
                                    if (dynamicBone != null && !dynamicBone.Equals(null) && dynamicBone.m_Root != null && !dynamicBone.m_Root.Equals(null)) 
                                        LogDebugInt(4, ConsoleColor.Yellow, "Player Bone - " + dynamicBone.m_Root.name);
                                    LogDebugInt(5, ConsoleColor.DarkRed, $"3.2");
                                    if (NDBConfig.breastsOnly && !includeBone && (dynamicBone?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? false) && !dynamicBone.m_Root.Equals(null) && !dynamicBone.gameObject.Equals(null) && !(
                                    dynamicBone.m_Root.transform.IsChildOf(dynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Chest)) &&
                                    !dynamicBone.m_Root.transform.IsChildOf(dynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Neck)) &&
                                    !dynamicBone.m_Root.transform.IsChildOf(dynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftShoulder)) &&
                                    !dynamicBone.m_Root.transform.IsChildOf(dynamicBone.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightShoulder))))
                                    { LogDebugInt(4, ConsoleColor.Red, $"Filtered c.2  - breastsOnly.True Player's db is not a child of the chest || is not also a child of neck/shoulders"); continue; }

                                    AddColliderToBone(dynamicBone, otherCollider, player.Item6.Item2, otherPlayerInfo.Item6.Item2);
                                }
                                catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in OtherPlayer=>Player - Foreach dynamicBone\n" + ex.ToString()); }
                            }
                        }
                        catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in OtherPlayer=>Player - Foreach otherCollider\n" + ex.ToString()); }
                    }
                }
                catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in OtherPlayer=>Player - Foreach OtherPlayer\n" + ex.ToString()); }
            }


            if (NDBConfig.logLevel >= 3) PrintBonesSpecific();
        }

        private bool CheckIfBoneIncluded(string hashAvatar, DynamicBone bone)
        {
            LogDebugInt(5, ConsoleColor.DarkRed, $"CiBI-1");
            bool includeBone = false;
            if (!NDBConfig.includeSpecificBones) return includeBone;
            LogDebugInt(5, ConsoleColor.DarkRed, $"CiBI-2");
            try
            {
                if (!(bone is null || bone.Equals(null) || bone.m_Root is null || bone.m_Root.Equals(null)))
                {
                    string hashBone = hashAvatar + ":db:" + bone.m_Root.name;
                    if (NDBConfig.bonesToInclude.Contains(hashBone)) includeBone = true;
                    if (!NDB.bonesIncluded.Contains(hashBone) && includeBone) NDB.bonesIncluded.Add(hashBone);
                    LogDebugInt(5, ConsoleColor.DarkRed, $"CiBI-3");
                }
            }
            catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in CheckIfBoneIncluded\n" + ex.ToString()); }
            LogDebugInt(5, ConsoleColor.DarkRed, $"CiBI-4");
            return includeBone;
        }

        private bool CheckIfColliderIncluded(string hashAvatar, DynamicBoneCollider dbc)
        {
            bool includeDBC = false;
            if (!NDBConfig.includeSpecificBones) return includeDBC;
            try
            {
                if (!(dbc is null || dbc.Equals(null) || dbc.gameObject is null || dbc.gameObject.Equals(null)))
                {
                    string hashBone = hashAvatar + ":dbc:" + dbc.gameObject.name;
                    if (NDBConfig.collidersToInclude.Contains(hashBone)) includeDBC = true;
                    if (!NDB.collidersIncluded.Contains(hashBone) && includeDBC) NDB.collidersIncluded.Add(hashBone);
                }
            }
            catch (Exception ex) { LogDebug(ConsoleColor.Red, "Error in CheckIfColliderIncluded\n" + ex.ToString()); }
            return includeDBC;
        }

        private void RemoveBonesOfGameObjectInAllPlayers(DynamicBoneCollider[] colliders)
        {
            foreach (DynamicBone[] dbs in avatarsInScene.Values.Select((x) => x.Item3))
            {
                foreach (DynamicBone db in dbs)
                {
                    foreach (DynamicBoneCollider dbc in colliders)
                    {
                        db.m_Colliders.Remove(dbc);
                    }
                }
            }
        }

        private void AddColliderToDynamicBone(DynamicBone bone, DynamicBoneCollider dbc, string boneOwner, string colliderOwner)
        {
            if (bone is null || bone.Equals(null) || dbc is null || dbc.Equals(null) || boneOwner is null || colliderOwner is null) return;
            if (bone.m_Root is null || bone.m_Root.Equals(null) || dbc.gameObject is null || dbc.gameObject.Equals(null)) return;
            //https://answers.unity.com/questions/1420784/if-something-null-not-good-enough.html

            if (NDBConfig.excludeSpecificBones)
            {
                try
                {
                    string hashBone = boneOwner + ":db:" + bone.m_Root.name;
                    if (NDBConfig.bonesToExclude.Contains(hashBone))
                    {
                        if (!(NDB.bonesExcluded.Contains(hashBone))) NDB.bonesExcluded.Add(hashBone);
                        LogDebugInt(3, ConsoleColor.Red, $"Specific Exclude db {hashBone}");
                        return;
                    }
                    if (NDBConfig.bonesToAlwaysExclude.Contains(bone.m_Root.name))
                    {
                        if (!(NDB.bonesExcluded.Contains(bone.m_Root.name))) NDB.bonesExcluded.Add(bone.m_Root.name);
                        LogDebugInt(3, ConsoleColor.Red, $"Specific Exclude db {hashBone}");
                        return;
                    }
                    string hashCol = colliderOwner + ":dbc:" + dbc.gameObject.name;
                    if (NDBConfig.collidersToExclude.Contains(hashCol))
                    {
                        if (!(NDB.collidersExcluded.Contains(hashCol))) NDB.collidersExcluded.Add(hashCol);
                        LogDebugInt(3, ConsoleColor.Red, $"Specific Exclude dbc {hashCol}");
                        return;
                    }
                }
                catch (System.Exception ex) { LogDebug(ConsoleColor.DarkRed, "Error in AddColliderToDynamicBone Exclude\n" + ex.ToString()); }
            }


            LogDebugInt(4, ConsoleColor.Cyan, $"Adding dbc {dbc.gameObject.name} from {colliderOwner} to db {bone.m_Root.name} from {boneOwner}");
            try
            {
                if (!bone.m_Colliders.Contains(dbc)) bone.m_Colliders.Add(dbc);
            }
            catch (System.Exception ex) { LogDebug(ConsoleColor.DarkRed, "Failed to Add DBC to DB\n" + ex.ToString()); }
            LogDebugInt(5, ConsoleColor.DarkCyan, $"DBC added to DB");
        }

        private void AddColliderToBone(DynamicBone bone, DynamicBoneCollider collider, string boneOwner, string colliderOwner)
        {
            try
            {
                if (NDBConfig.disallowInsideColliders && collider.m_Bound == DynamicBoneCollider.EnumNPublicSealedvaOuIn3vUnique.Inside)
                {
                    LogDebugInt(2, ConsoleColor.Red, $"Collider is an inside collider and will filtered from being multiplayer'd- avatar:{colliderOwner}, collider name:{collider.name}");
                    return;
                }
                if (collider.Equals(null) || collider.transform.Equals(null)) return;
                float localscale = ((collider.transform.lossyScale.x + collider.transform.lossyScale.y + collider.transform.lossyScale.z) / 3);

                if ((collider.m_Radius * localscale) > NDBConfig.colliderSizeLimit || (collider.m_Height * localscale) > NDBConfig.colliderSizeLimit)
                {
                    LogDebugInt(2, ConsoleColor.Red, $"Collider is too big and will be filtered from being multiplayer'd- avatar:{colliderOwner}, collider name:{collider.name}, localscale: {localscale} radius:{collider.m_Radius} - adjusted rad:{(collider.m_Radius * localscale)}, size limit:{NDBConfig.colliderSizeLimit} ");
                    return;
                }
                LogDebugInt(4, ConsoleColor.DarkCyan, $"Collider info- avatar:{colliderOwner}, collider:{collider.name}, localscale: {localscale}, rad:{collider.m_Radius} - adj rad:{(localscale * collider.m_Radius)}, height:{collider.m_Height} - adj height:{(localscale * collider.m_Height)} size limit:{NDBConfig.colliderSizeLimit} ");

                AddColliderToDynamicBone(bone, collider, boneOwner, colliderOwner);
                
            }
            catch (System.Exception ex) { LogDebug(ConsoleColor.DarkRed, "Error in AddColliderToBone\n" + ex.ToString()); }
        }


        private static void AddAutoCollidersToPlayer(GameObject avatar, string avatarHash) //AutoAdd Colliders 
        {
            try
            {
                if (NDBConfig.addAutoCollidersAll == true || (NDBConfig.avatarsToAddColliders.ContainsKey(avatarHash) && NDBConfig.avatarsToAddColliders[avatarHash]))
                {
                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 1");
                    if (!avatar?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? false )//Make sure model has an animator and is human
                    { LogDebugInt(1, ConsoleColor.Yellow, $"Avatar {avatarHash} was tagged to get colliders added to it's hands but aborted: Avatar is not Human rigged"); return; }

                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 2");
                    Transform lefthand = avatar?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.GetBoneTransform(HumanBodyBones.LeftHand);
                    Transform righthand = avatar?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.GetBoneTransform(HumanBodyBones.RightHand);
                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 3");
                    if (righthand is null || lefthand is null)
                    { LogDebugInt(1, ConsoleColor.Yellow, $"Avatar {avatarHash} was tagged to get colliders added to it's hands but aborted: Missing one or more hands"); return; }

                    if (lefthand.childCount == 0 || righthand.childCount == 0)
                    { LogDebugInt(1, ConsoleColor.Yellow, $"Avatar {avatarHash} was tagged to get colliders added to it's hands but aborted: Hands do not have child bones"); return; }
                    if (lefthand.gameObject.GetComponentInChildren<DynamicBoneCollider>() != null || righthand.gameObject.GetComponent<DynamicBoneCollider>() != null)
                    { LogDebugInt(1, ConsoleColor.Yellow, $"Avatar {avatarHash} was tagged to get colliders added to it's hands but aborted: Hands already have DBC(s)"); return; }

                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 4");
                    List<float> leftDistances = new List<float>(); //Find the hand size by checking average distance from hand bone to the first childern that are fingers
                    for (int i = 0; i < lefthand.childCount; i++)
                    {
                        if (lefthand.GetChild(i) == lefthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftThumbProximal) ||
                            lefthand.GetChild(i) == lefthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftIndexProximal) ||
                            lefthand.GetChild(i) == lefthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) ||
                            lefthand.GetChild(i) == lefthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftRingProximal) ||
                            lefthand.GetChild(i) == lefthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftLittleProximal))
                        { leftDistances.Add(Vector3.Distance(lefthand.position, lefthand.GetChild(i).position)); }
                    }
                    List<float> rightDistances = new List<float>();
                    for (int i = 0; i < righthand.childCount; i++)
                    {
                        if (righthand.GetChild(i) == righthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightThumbProximal) ||
                            righthand.GetChild(i) == righthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightIndexProximal) ||
                            righthand.GetChild(i) == righthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ||
                            righthand.GetChild(i) == righthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightRingProximal) ||
                            righthand.GetChild(i) == righthand.transform.root.GetComponentInChildren<VRCPlayer>().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightLittleProximal))
                        { rightDistances.Add(Vector3.Distance(righthand.position, righthand.GetChild(i).position)); }
                    }

                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 5");
                    if (leftDistances.Count == 0 || rightDistances.Count == 0)
                    {
                        LogDebugInt(1, ConsoleColor.Yellow, $"Avatar {avatarHash} was tagged to get colliders added to it's hands but aborted: Hands have no fingers"); return;
                    }
                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 6");
                    float leftlocalscale = ((lefthand.transform.lossyScale.x + lefthand.transform.lossyScale.y + lefthand.transform.lossyScale.z) / 3);
                    float rightlocalscale = ((righthand.transform.lossyScale.x + righthand.transform.lossyScale.y + righthand.transform.lossyScale.z) / 3);

                    LogDebugInt(5, ConsoleColor.DarkRed, "AActP 7");
                    lefthand.gameObject.AddComponent<DynamicBoneCollider>().m_Radius = leftDistances.Average() / leftlocalscale;
                    righthand.gameObject.AddComponent<DynamicBoneCollider>().m_Radius = rightDistances.Average() / rightlocalscale;

                    LogDebugInt(1, ConsoleColor.Yellow, $"Added Hand Collider to avatar {avatarHash}. Left size: {(lefthand.gameObject.GetComponent<DynamicBoneCollider>().m_Radius * leftlocalscale).ToString("F5").TrimEnd('0')}, Right size: {(righthand.gameObject.GetComponent<DynamicBoneCollider>().m_Radius * rightlocalscale).ToString("F5").TrimEnd('0')}");
                }
            }
            catch (System.Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); };
        }


        private void AddDynamicBonesToVisibilityList(string player, DynamicBone[] dynamicBones, Renderer renderer)
        {
            try
            {
                //Removing bones that were not enabled by default from being modified by the Visibility list 
                List<DynamicBone> enabledList = new List<DynamicBone>();
                foreach (DynamicBone b in dynamicBones)
                {
                    if (b.enabled == true) enabledList.Add(b);
                    if(b != null && !b.Equals(null) && b.m_Root != null && !b.m_Root.Equals(null))
                        LogDebugInt(2, ConsoleColor.Cyan, $"Bone {b.m_Root.name} is disabled and will not be added to visibility list");
                }
                DynamicBone[] enabledDBs = enabledList.ToArray();
                avatarRenderers.Add(player, new System.Tuple<Renderer, DynamicBone[]>(renderer, enabledDBs));
            }
            catch (System.Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); };
        }

        private void RemoveDynamicBonesFromVisibilityList(string player)
        {
            avatarRenderers.Remove(player);
        }



        public override void OnUpdate()
        {
            if (avatarRenderers != null)
            {
                if (avatarRenderers.Count != 0 && NDBConfig.enableBoundsCheck) EnableIfVisible();
            }

            if (!NDBConfig.keybindsEnabled) return;

            if (Input.GetKeyDown(KeyCode.F8))
            {
                LogDebug(ConsoleColor.DarkMagenta, "My bones have the following colliders attached:");
                localPlayer.GetComponentsInChildren<DynamicBone>().Do((bone) =>
                {
                    LogDebug(ConsoleColor.DarkMagenta, $"Bone {bone.m_Root.name} has {bone.m_Colliders.Count} colliders attached");
                    bone.m_Colliders._items.Do((dbc) =>
                    {
                        try
                        {
                            LogDebug(ConsoleColor.DarkMagenta, $"Bone {bone?.m_Root.name ?? "null"} has {dbc?.gameObject.name ?? "null"}");
                        }
                        catch (System.Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); };
                    });
                });

                LogDebug(ConsoleColor.DarkMagenta, $"There are {avatarsInScene.Values.Aggregate(0, (acc, tup) => acc += tup.Item3.Length)} Dynamic Bones in scene");
                LogDebug(ConsoleColor.DarkMagenta, $"There are {avatarsInScene.Values.Aggregate(0, (acc, tup) => acc += tup.Item4.Length)} Dynamic Bones Colliders in scene");
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LogDebug(ConsoleColor.DarkMagenta, "Another player bones have the following colliders attached:");
                avatarsInScene.First(i => i.Value.Item1 != localPlayer).Value.Item1.GetComponentsInChildren<DynamicBone>().Do((bone) =>
                {
                    LogDebug(ConsoleColor.DarkMagenta, $"Bone {bone.m_Root.name} has {bone.m_Colliders.Count} colliders attached");
                    bone.m_Colliders._items.Do((dbc) =>
                    {
                        try
                        {
                            LogDebug(ConsoleColor.DarkMagenta, $"Bone {bone?.m_Root.name ?? "null"} has {dbc?.gameObject.name ?? "null"}");
                        }
                        catch (System.Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); };
                    });
                });
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleState();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                LogDebug(ConsoleColor.Red, "List of avatar in dict:");
                foreach (string name in avatarsInScene.Keys)
                {
                    LogDebug(ConsoleColor.DarkGreen, name);
                }
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (NDBConfig.enableEditor) ToggleDynamicBoneEditorGUI();
            }
            if (Input.GetKeyDown(KeyCode.F12))
            {
                SanityCheck();
            }

        }

        private void SanityCheck()
        {
            foreach (System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>> player in avatarsInScene.Values)
            {
                try
                {
                    bool flagAv = false;
                    string debug = "\n";
                    debug += "AvatarName: " + player.Item6.Item1 + "\n";
                    debug += "AvatarName+Hash: " + player.Item6.Item2 + "\n";
                    debug += "ArmatureScale:" + player.Item6.Item3 + "\n";
                    debug += "IsFriends:" + player.Item5 + "\n";
                    debug += "IsInVR:" + player.Item2 + "\n";
                    if (player.Item1 != null && !player.Item1.Equals(null))
                    {
                        debug += "Avatar:Exists\n";
                        debug += "IsHuman: " + player?.Item1?.transform?.root?.GetComponentInChildren<VRCPlayer>()?.field_Internal_Animator_0?.isHuman ?? "Null"; debug += "\n";
                    }
                    else 
                    { 
                        debug += "Avatar: Is Null !-!-!-!-!-!-!-!-!\n";
                        flagAv = true;
                    }
                    foreach (DynamicBone db in player.Item3) 
                    { 
                        if(db !=null && !db.Equals(null) && db.m_Root !=null && !db.m_Root.Equals(null))
                        {
                            debug += "--DB: " + db.m_Root.name + "\n";
                        }
                    }
                    foreach(DynamicBoneCollider dbc in player.Item4)
                    {
                        if (dbc != null && !dbc.Equals(null) && dbc.gameObject != null && !dbc.gameObject.Equals(null))
                        {
                            debug += "--DBC: " + dbc.gameObject.name + "\n";
                        }
                    }
                    debug += "------------------";
                    LogDebug(ConsoleColor.Cyan, debug);
                    if(flagAv) LogDebug(ConsoleColor.Red, "Avatar was Null!");
                }
                catch (Exception ex) { MelonLogger.Msg(ConsoleColor.Red, "Error in SanityCheck\n" + ex.ToString()); }
            }
        }

        private Rect guiRect;
        private bool showEditorGUI = false;

        //private Process editorProccess;
        //private string editorPath;
        private IPCHandler ipcHandler;
        private void ToggleDynamicBoneEditorGUI()
        {
            showEditorGUI = !showEditorGUI;
            new Thread(new ThreadStart(ConnectToExternalInterface));
            //if (editorProccess != null && !editorProccess.HasExited)
            //{
            //    editorProccess.CloseMainWindow();
            //    editorProccess = null;
            //}
            //else
            //{
            //    if (editorPath == null)
            //    {
            //        if (!File.Exists(editorPath))
            //        {
            //            editorPath = Path.Combine(Assembly.Location, "ExternalDynamicBoneEditor.exe");
            //            //using (ResourceReader resourceReader = new ResourceReader(Assembly.GetManifestResourceStream(Assembly.GetManifestResourceNames()[0])))
            //            //{
            //            //    editorPath = Path.Combine(Assembly.Location, "ExternalDynamicBoneEditor.exe");
            //            //    resourceReader.GetResourceData("ExternalDynamicBoneEditor", out string _, out byte[] bytecode);
            //            //    File.WriteAllBytes(editorPath, bytecode);
            //            //}
            //        }
            //    }
            //    editorProccess = Process.Start(editorPath);
            //    new Thread(new ParameterizedThreadStart(ConnectToExternalInterface)).Start(new Tuple<string, DynamicBone[]>(localPlayer.transform.root.GetComponentInChildren<VRCPlayer>().prop_ApiAvatar_0.name.Take(127).Aggregate("", (acc, c) => acc += c), (DynamicBone[])localPlayer.GetComponentsInChildren<DynamicBone>()));
            //}
        }

        private void ConnectToExternalInterface()
        {
            try
            {
                AvatarBones avatar = new AvatarBones();
                avatar.name = localPlayer.transform.root.GetComponentInChildren<VRCPlayer>().prop_ApiAvatar_0.name.Take(127).Aggregate("", (acc, c) => acc += c);
                LogDebug(ConsoleColor.Blue, "1");
                foreach (DynamicBone db in localPlayer.GetComponentsInChildren<DynamicBone>())
                {
                    avatar.bones[avatar.boneCount++] = MakeBoneStruct(db);
                }
                LogDebug(ConsoleColor.Blue, "2");
                ipcHandler = new IPCHandler(new NamedPipeServerStream("vrchatmdb", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message));
                while (!ipcHandler.IsConnected) Thread.Sleep(100);
                HandleIPCMessages(localPlayer.GetComponentsInChildren<DynamicBone>());
            }
            catch (Exception e) { LogDebug(ConsoleColor.Red ,e.ToString()); }
        }

        private void HandleIPCMessages(DynamicBone[] dynamicBones)
        {
            Message kind = ipcHandler.Receive(out byte[] msg);
            using (MemoryStream ms = new MemoryStream(msg))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    switch (kind)
                    {
                        case Message.SetBoneDamping:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Damping = br.ReadSingle();
                                break;
                            }
                        case Message.SetBoneElasticity:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Elasticity = br.ReadSingle();
                                break;
                            }
                        case Message.SetBoneStiffness:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Stiffness = br.ReadSingle();
                                break;
                            }
                        case Message.SetBoneInert:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Inert = br.ReadSingle();
                                break;
                            }
                        case Message.SetBoneRadius:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Radius = br.ReadSingle();
                                break;
                            }
                        case Message.SetBoneEndLength:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_EndLength = br.ReadSingle();
                                break;
                            }
                        case Message.SetBoneEndOffset:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_EndOffset = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                break;
                            }
                        case Message.SetBoneGravity:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Gravity = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                break;
                            }
                        case Message.SetBoneForce:
                            {
                                GetDynamicBoneByName(dynamicBones, br.ReadString()).m_Force = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                break;
                            }
                    }
                }
            }

        }

        private DynamicBone GetDynamicBoneByName(DynamicBone[] bones, string name)
        {
            return bones.Single(b => b.m_Root.name == name);
        }

        private SerializedBoneData MakeBoneStruct(DynamicBone db)
        {
            SerializedBoneData serializedBoneData = new SerializedBoneData();
            serializedBoneData.name = db.m_Root?.name ?? "undefined";
            serializedBoneData.damping = db.m_Damping;
            serializedBoneData.elasticity = db.m_Elasticity;
            serializedBoneData.stiffness = db.m_Stiffness;
            serializedBoneData.inert = db.m_Inert;
            serializedBoneData.radius = db.m_Radius;
            serializedBoneData.endLength = db.m_EndLength;
            serializedBoneData.endOffset = new float3(db.m_EndOffset.x, db.m_EndOffset.y, db.m_EndOffset.z);
            serializedBoneData.gravity = new float3(db.m_Gravity.x, db.m_Gravity.y, db.m_Gravity.z);
            serializedBoneData.force = new float3(db.m_Force.x, db.m_Force.y, db.m_Force.z);
            return serializedBoneData;
        }

        public override void OnGUI()
        {
            if (showEditorGUI) guiRect = GUILayout.Window(0, new Rect(5, 5, 80, 80), (GUI.WindowFunction)DrawWindowContents, "Dynamic Bones Editor Interface", new Il2CppReferenceArray<GUILayoutOption>(0));
            GUI.FocusWindow(0);
        }

        private Vector2 scrollPosition;
        private void DrawWindowContents(int id)
        {
            try
            {
                //MelonLogger.Msg(ConsoleColor.DarkBlue, "Started drawing editor UI");
                GUILayout.Label($"Avatar: {localPlayer.GetComponentInChildren<VRCPlayer>()?.prop_VRCAvatarManager_0?.prop_ApiAvatar_0?.name ?? ("error fetching avatar name")}", new GUIStyle() { fontSize = 18 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(400), GUILayout.Height(600) });
                foreach (DynamicBone db in localPlayer.GetComponentsInChildren<DynamicBone>(true))
                {
                    //MelonLogger.Msg(ConsoleColor.DarkBlue, $"Started drawing bone {db.m_Root.name}");
                    GUILayout.Label(db.m_Root.name, new GUIStyle() { fontSize = 18 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    GUILayout.Label("Update rate", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    db.m_UpdateRate = (int)GUILayout.HorizontalSlider(db.m_UpdateRate, 1f, 60f, new Il2CppReferenceArray<GUILayoutOption>(0));
                    //if (int.TryParse(GUILayout.TextField(((int)db.m_UpdateRate).ToString(), new GUIStyle() { margin = new RectOffset(10, 0, 0, 0) }, new Il2CppReferenceArray<GUILayoutOption>(0)), out int updateRatevalue))
                    //{
                    //    db.m_UpdateRate = updateRatevalue;
                    //}

                    GUILayout.Label("Damping", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    db.m_Damping = GUILayout.HorizontalSlider(db.m_Damping, 0f, 1f, new Il2CppReferenceArray<GUILayoutOption>(0));
                    GUILayout.Label("Elasticity", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    db.m_Elasticity = GUILayout.HorizontalSlider(db.m_Elasticity, 0f, 1f, new Il2CppReferenceArray<GUILayoutOption>(0));
                    GUILayout.Label("Stiffness", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    db.m_Stiffness = GUILayout.HorizontalSlider(db.m_Stiffness, 0f, 1f, new Il2CppReferenceArray<GUILayoutOption>(0));
                    GUILayout.Label("Inert", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    db.m_Inert = GUILayout.HorizontalSlider(db.m_Inert, 0f, 1f, new Il2CppReferenceArray<GUILayoutOption>(0));

                    GUILayout.Label("Radius", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    if (float.TryParse(GUILayout.TextField((db.m_Radius).ToString(), new Il2CppReferenceArray<GUILayoutOption>(0)), out float radiusValue))
                    {
                        db.m_Radius = radiusValue;
                    }

                    GUILayout.Label("End length", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    if (float.TryParse(GUILayout.TextField((db.m_EndLength).ToString(), new Il2CppReferenceArray<GUILayoutOption>(0)), out float endLengthValue))
                    {
                        db.m_Radius = endLengthValue;
                    }
                    GUILayout.Label("End offset", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    GUILayout.Label("X", new GUIStyle() { fontSize = 14 }, Array.Empty<GUILayoutOption>());
                    //if (float.TryParse(GUILayout.TextField((db.m_EndOffset.x).ToString(), new GUIStyle() { margin = new RectOffset((int)GUILayoutUtility.GetLastRect().xMax, 0, 0, 0) }, new Il2CppReferenceArray<GUILayoutOption>(0)), out float xvalue))
                    if (float.TryParse(GUILayout.TextField((db.m_EndOffset.x).ToString(), new Il2CppReferenceArray<GUILayoutOption>(0)), out float xvalue))
                    {
                        db.m_EndOffset.Set(xvalue, db.m_EndOffset.y, db.m_EndOffset.z);
                    }
                    GUILayout.Label("Y", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    if (float.TryParse(GUILayout.TextField((db.m_EndOffset.y).ToString(), new Il2CppReferenceArray<GUILayoutOption>(0)), out float yvalue))
                    {
                        db.m_EndOffset.Set(db.m_EndOffset.x, yvalue, db.m_EndOffset.z);
                    }
                    GUILayout.Label("Z", new GUIStyle() { fontSize = 14 }, new Il2CppReferenceArray<GUILayoutOption>(0));
                    if (float.TryParse(GUILayout.TextField((db.m_EndOffset.z).ToString(), new Il2CppReferenceArray<GUILayoutOption>(0)), out float zvalue))
                    {
                        db.m_EndOffset.Set(db.m_EndOffset.x, db.m_EndOffset.y, zvalue);
                    }
                }

                GUILayout.EndScrollView();
                if (GUI.changed && nextEditorUpdate < Time.time)
                {
                    foreach (DynamicBone db in localPlayer.GetComponentsInChildren<DynamicBone>(true))
                    {
                        db.m_Radius = Mathf.Max(db.m_Radius, 0f);
                        reloadDynamicBoneParamInternalFuncs.Item1.Invoke(db, null);
                        reloadDynamicBoneParamInternalFuncs.Item2.Invoke(db, null);

                        ApplyBoneChanges(db); //Needed for the changes to be applied into the active bones 

                        LogDebug(ConsoleColor.DarkGreen, $"Updated setting for bone {db.m_Root.name}");
                    }
                    nextEditorUpdate = Time.time + 1f;
                }


                //MelonLogger.Msg(ConsoleColor.DarkBlue, "Finished drawing editor UI");
            }
            catch (Exception ex)
            {
                LogDebugError(ex.ToString());
            }
        }

        private void EnableIfVisible()
        {
            if (nextUpdateVisibility < Time.time && !NDBConfig.disableAllBones)
            {
                foreach (System.Tuple<Renderer, DynamicBone[]> go in avatarRenderers.Values)
                {
                    if (go.Item1 == null) continue;
                    bool visible = go.Item1.isVisible;
                    foreach (DynamicBone db in go.Item2)
                    {
                        if (db == null || db.Equals(null)) continue;
                        if (NDBConfig.logLevel >= 3) if (db.enabled != visible) LogDebug(ConsoleColor.DarkBlue, $"{db.gameObject.name} is now {((visible) ? "enabled" : "disabled")}");
                        db.enabled = visible;
                    }
                }
                nextUpdateVisibility = Time.time + NDBConfig.visiblityUpdateRate;
            }
        }

        private void DisableAllBones(DynamicBone[] dbs)
        {
            foreach (DynamicBone db in dbs)
            {
                LogDebugInt(3, ConsoleColor.DarkBlue, $"{db.gameObject.name} is now disabled - DisableAllBones is Set");
                db.enabled = false;
            }
        }

        private void ToggleState()
        {
            enabled = !enabled;
            LogDebug(ConsoleColor.Green, $"NDBMod is now {((enabled == true) ? "enabled" : "disabled")}");
            try
            {
                if (!enabled)
                {
                    RestoreOriginalColliderList();
                }
                else AddAllCollidersToAllPlayers();
            }
            catch (Exception ex) { LogDebug(ConsoleColor.Red, ex.ToString()); }

            try
            {
                if (toggleButton != null) toggleButton.GetComponentInChildren<Text>().text = $"Press to {((enabled) ? "disable" : "enable")} Dynamic Bones mod";
            }
            catch { }
            //if (NDBConfig.enableFallbackModUi) toggleButton.GetComponentInChildren<Text>().text = $"Press to {((enabled) ? "disable" : "enable")} Dynamic Bones mod"; This does the exact same thing as the line above?
        }

        private void RemovePlayerFromDict(string name)
        {
            avatarsInScene.Remove(name);
        }

        private void AddToPlayerDict(string name, System.Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>> value)
        {
            avatarsInScene.Add(name, value);
        }

        private void DeleteOriginalColliders(string name)
        {
            originalSettings.Remove(name);
        }

        private void SaveOriginalColliderList(string name, DynamicBone[] bones)
        {
            if (originalSettings.ContainsKey(name)) originalSettings.Remove(name);
            List<OriginalBoneInformation> ogInfo = new List<OriginalBoneInformation>(bones.Length);
            foreach (DynamicBone b in bones)
            {
                bones.Select((bone) =>
                {
                    return new OriginalBoneInformation() { distanceToDisable = bone.m_DistanceToObject, updateRate = bone.m_UpdateRate, distantDisable = bone.m_DistantDisable, colliders = new List<DynamicBoneCollider>(bone.m_Colliders.ToArrayExtension()), referenceToOriginal = bone, Elasticity = bone.m_Elasticity, Stiffness = bone.m_Stiffness, Damping = bone.m_Damping, Inert = bone.m_Inert, Radius = bone.m_Radius, Enabled = bone.enabled };
                }).Do((info) => ogInfo.Add(info));
            }
            originalSettings.Add(name, ogInfo);
            LogDebugInt(0, ConsoleColor.DarkGreen, $"Saved original dynamic bone info of player {name}");
        }

        private void RestoreOriginalColliderList()
        {
            foreach (KeyValuePair<string, Tuple<GameObject, bool, DynamicBone[], DynamicBoneCollider[], bool, System.Tuple<string, string, float>>> player in avatarsInScene) //Foreach player
            {
                LogDebugInt(0, ConsoleColor.DarkBlue, $"Restoring original settings for player {player.Key}");
                foreach (DynamicBone db in player.Value.Item3) //Foreach bone in player avatar
                {
                    if (originalSettings.TryGetValue(player.Key, out List<OriginalBoneInformation> origList)) //Gets previous info for player
                    {
                        try
                        {
                            origList.DoIf((x) => ReferenceEquals(x.referenceToOriginal, db), (origData) =>
                            {
                                db.m_Colliders.Clear();
                                origData.colliders.ForEach((dbc) => db.m_Colliders.Add(dbc));
                                db.m_DistanceToObject = origData.distanceToDisable;
                                db.field_Private_Single_4 = origData.updateRate;
                                db.m_UpdateRate = origData.updateRate;
                                db.m_DistantDisable = origData.distantDisable;
                                db.m_Elasticity = origData.Elasticity;
                                db.m_Stiffness = origData.Stiffness;
                                db.m_Damping = origData.Damping;
                                db.m_Inert = origData.Inert;
                                db.m_Radius = origData.Radius;
                                db.enabled = origData.Enabled;
                                ApplyBoneChanges(db);
                            });
                        }
                        catch (Exception e)
                        {
                            LogDebug(ConsoleColor.Red, e.ToString());
                        }
                    }
                    else
                    {
                        LogDebug(ConsoleColor.DarkYellow, $"Warning: could not find original dynamic bone info for {player.Key}'s bone {db.gameObject.name} . This means his bones won't be disabled!");
                    }
                }
            }
            NDB.bonesExcluded.Clear();
            NDB.collidersExcluded.Clear();
            NDB.bonesIncluded.Clear();
            NDB.collidersIncluded.Clear();
        }


        public static StringBuilder sb;

        public static void LogDebug(ConsoleColor color, string text)
        {
            MelonLogger.Msg(color, text);
            //if (NDBConfig.debugLog == true) File.AppendAllText(ExtraLogPath, DateTime.Now.ToString("'['HH':'mm':'ss'] '") + text + Environment.NewLine);//Log to .log file 
            //if (NDBConfig.debugLog > 0) WriteToFile(DateTime.Now.ToString("'['HH':'mm':'ss.fff'] '") + text + Environment.NewLine);
            if (NDBConfig.debugLog > 0) sb.Append(DateTime.Now.ToString("'['HH':'mm':'ss.fff'] '") + text + Environment.NewLine);
        }
        public static void LogDebugInt(int lvl, ConsoleColor color, string text)
        {
            if (NDBConfig.logLevel >= lvl) MelonLogger.Msg(color, text);
            ///if (NDBConfig.debugLog == true) File.AppendAllText(ExtraLogPath, DateTime.Now.ToString("'['HH':'mm':'ss'] '") + text + Environment.NewLine);//Log to .log file 
            //if (NDBConfig.debugLog > 0 && NDBConfig.debugLog >= lvl) WriteToFile(DateTime.Now.ToString("'['HH':'mm':'ss.fff'] '") + text + Environment.NewLine);
            if (NDBConfig.debugLog > 0 && NDBConfig.debugLog >= lvl) sb.Append(DateTime.Now.ToString("'['HH':'mm':'ss.fff'] '") + text + Environment.NewLine);
        }
        public static void LogDebugError(string text)
        {
            MelonLogger.Error(text);
            //if (NDBConfig.debugLog == true) File.AppendAllText(ExtraLogPath, DateTime.Now.ToString("'['HH':'mm':'ss'] '") + text + Environment.NewLine);//Log to .log file 
            //if (NDBConfig.debugLog > 0) WriteToFile(DateTime.Now.ToString("'['HH':'mm':'ss.fff'] '") + text + Environment.NewLine);
            if (NDBConfig.debugLog > 0) sb.Append(DateTime.Now.ToString("'['HH':'mm':'ss.fff'] '") + text + Environment.NewLine);

        }

        public static void InitDebugLog()
        {
            if (ExtraLogPath is null) {
                ExtraLogPath = "UserData/MDB/Log/" + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss") + ".log";
                MelonLogger.Msg(ConsoleColor.Yellow, "DebugLog is enabled - This will write a seperate log file to 'UserData\\MDB\\Log'\n This log file may be large depending on the DebugLog Setting");
            }
            if (!Directory.Exists("UserData/MDB/Log")) Directory.CreateDirectory("UserData/MDB/Log");
            if (!File.Exists(ExtraLogPath))
            {
                File.AppendAllText(ExtraLogPath, "MDB " + VERSION_STR + " Extra Log file - " + DateTime.Now.ToString() + Environment.NewLine);
                //ToDo: Dump config
            }
            sb = new StringBuilder(100000);
            

            MelonCoroutines.Start(ManageDebugBuffer());
        }

        static System.Collections.IEnumerator ManageDebugBuffer()
        {
            var nextUpdate = Time.time;
            while (NDBConfig.debugLog > 0)
            {
                if ((sb.Length > 100000 || nextUpdate < Time.time) && sb.Length > 0)
                {
                    if (NDBConfig.debugLog >= 5) MelonLogger.Msg(ConsoleColor.Gray, "sb length: " + sb.Length);
                    WriteToFile(sb.ToString());
                    sb.Clear();
                    nextUpdate = Time.time + 10f;
                }
                yield return null;
            }
            ExtraLogPath = null;
            MelonLogger.Msg(ConsoleColor.Gray, "End Debug");
        }

        public async static void WriteToFile(string text)
        {
            using var stream = new FileStream(ExtraLogPath, FileMode.Append, FileAccess.Write, FileShare.Write, 4096, useAsync: true);
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

    }


    public static class ListExtensions
    {
        public static T[] ToArrayExtension<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            T[] arr = new T[list.Count];
            for (int x = 0; x < list.Count; x++)
            {
                arr[x] = list[x];
            }
            return arr;
        }
    }
}


namespace UIExpansionKit.API
{

    public struct LayoutDescriptionCustom
    {
        public static LayoutDescription QuickMenu1Column = new LayoutDescription { NumColumns = 1, RowHeight = 40, NumRows = 11 };
        public static LayoutDescription QuickMenu3Column = new LayoutDescription { NumColumns = 3, RowHeight = 115, NumRows = 4 }; //Default height is 95
        public static LayoutDescription QuickMenu1Column11Row = new LayoutDescription { NumColumns = 1, RowHeight = 444 / 12, NumRows = 12 };
    }
}
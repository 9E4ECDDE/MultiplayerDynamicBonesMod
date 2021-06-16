using System;
using System.IO;
using System.Reflection;
using Harmony;
using MelonLoader;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

//https://github.com/knah/VRCMods/tree/master/Common

namespace DBMod
{
    [HarmonyShield]
    internal static class LoaderIntegrityCheck
    {
        public static void CheckIntegrity()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("MultiplayerDynamicBonesMod.ILC._dummy_.dll");
                using var memStream = new MemoryStream((int)stream.Length);
                stream.CopyTo(memStream);

                Assembly.Load(memStream.ToArray());

                PrintWarningMessage();

                while (Console.In.Peek() != '\n') Console.In.Read();
            }
            catch (BadImageFormatException)
            {
            }

            try
            {
                using var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("MultiplayerDynamicBonesMod.ILC._dummy2_.dll");
                using var memStream = new MemoryStream((int)stream.Length);
                stream.CopyTo(memStream);

                Assembly.Load(memStream.ToArray());
            }
            catch (BadImageFormatException ex)
            {
                MelonLogger.Error(ex.ToString());

                PrintWarningMessage();

                while (Console.In.Peek() != '\n') Console.In.Read();
            }

            try
            {
                var harmony = HarmonyInstance.Create(Guid.NewGuid().ToString());
                harmony.Patch(AccessTools.Method(typeof(LoaderIntegrityCheck), nameof(PatchTest)),
                    new HarmonyMethod(typeof(LoaderIntegrityCheck), nameof(ReturnFalse)));

                PatchTest();

                PrintWarningMessage();

                while (Console.In.Peek() != '\n') Console.In.Read();
            }
            catch (BadImageFormatException)
            {
            }
        }

        private static bool ReturnFalse() => false;

        public static void PatchTest()
        {
            throw new BadImageFormatException();
        }

        private static void PrintWarningMessage()
        {
            MelonLogger.Error("===================================================================");
            MelonLogger.Error("You're using MelonLoader with important security features missing.");
            MelonLogger.Error("This exposes you to additional risks from certain malicious actors,");
            MelonLogger.Error("including account theft, account bans, and other unwanted consequences");
            MelonLogger.Error("If this is not what you want, download the official installer from");
            MelonLogger.Error("https://github.com/LavaGang/MelonLoader/releases");
            MelonLogger.Error("then close this console, and reinstall MelonLoader using it.");
            MelonLogger.Error("If you want to accept those risks, press Enter to continue");
            MelonLogger.Error("===================================================================");
            //Modified below this point
            new Thread(new ThreadStart(PopupMsg)).Start();
        }

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        static extern int MessageBox(IntPtr nWnd, string text, string title, uint type);
        private static void PopupMsg()
        {
            try
            {
                if (MessageBox(IntPtr.Zero, "You're using MelonLoader with important security features missing. Do you want to download the official installer?", "Multiplayer Dynamic Bones Mod", 0x04 | 0x40 | 0x1000) == 6)
                {
                    Process.Start("https://github.com/LavaGang/MelonLoader/releases");
                    MessageBox(IntPtr.Zero, "Please close the game, and reinstall MelonLoader using the link on the page that just loaded.", "Multiplayer Dynamic Bones Mod", 0x40 | 0x1000);
                }
            }
            catch (Exception ex) { MelonLogger.Error(ex.ToString()); return; }
        }
    }
} 
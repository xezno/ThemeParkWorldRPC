using System;
using System.IO;
using System.Threading;

namespace ThemeParkWorldRPC
{
    public class TpwRpc
    {
        private Discord.Discord discord;
        private long startTime;

        public TpwRpc()
        {
            discord = new Discord.Discord(784571542221750283, (ulong)Discord.CreateFlags.Default);
        }

        public long GetUnixTime()
        {
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public void Run()
        {
            /*
             * Addresses:
             * +007CB2DC: Int32 - Cash
             * +00871F74: String - level name (jungle, hallow, fantasy, space)
             * +0087505C: Ditto
             * +00876668: Ditto
             * +00FAA40D: Ditto
             * +00768F3D / 05C4D745?: "lobby" or "level", depending on whether the player is in lobby or not
             * +00FAA40D: Previous load (if in lobby, this will be different to level name at 00871F74).
             * +007CB360: Visitor count (only updates when info screen is shown)
             * +00752EC4: Golden ticket count
             * +007CC4B8: Golden key count
             * +05E6DAE0*: User index (saves folder prefixes, add one)
             */

            Console.WriteLine($"Looking for {GlobalSettings.Default.ProcessName}...");
            var found = false;
            var shouldExit = false;
            while (!found)
            {
                found = Memory.Attach(GlobalSettings.Default.ProcessName);
                Thread.Sleep(GlobalSettings.Default.UpdateDelay);
            }
            Console.WriteLine($"Found {GlobalSettings.Default.ProcessName}.");

            while (!shouldExit)
            {
                var rpcData = new TpwRpcData();

                rpcData.Cash = Memory.ReadMemory<int>(0x007CB2DC);
                rpcData.Level = Memory.ReadMemoryString(0x00871F74, 6);
                rpcData.LastLoad = Memory.ReadMemoryString(0x00FAA40D, 6);
                rpcData.GoldenTicketCount = Memory.ReadMemory<int>(0x00752EC4);
                rpcData.GoldenKeyCount = Memory.ReadMemory<int>(0x007CC4B8);

                var savesIndex = Memory.ReadMemory<int>(0x05E6DAE0);
                var savesContents = Directory.GetDirectories(@"C:\Program Files (x86)\Bullfrog\Theme Park World\save\users");
                rpcData.SaveName = "";

                if (savesIndex >= 0 && savesIndex < 5)
                {
                    foreach (var save in savesContents)
                    {
                        var saveName = Path.GetFileName(save);
                        var relSavesIndex = savesIndex + 1;
                        if (saveName.StartsWith(relSavesIndex.ToString()))
                        {
                            rpcData.SaveName = saveName.Substring(1);
                        }
                    }
                }

                rpcData.InLobby = rpcData.Cash == 0 || rpcData.Level != rpcData.LastLoad || savesIndex < 0;

                UpdateActivity(rpcData);
                discord.RunCallbacks();
                Thread.Sleep(GlobalSettings.Default.UpdateDelay);
            }

            startTime = GetUnixTime();
        }

        private string PrettifyLevelName(string levelName)
        {
            switch (levelName)
            {
                case "jungle":
                    return "Lost Kingdom";
                case "space\\":
                    return "Space Zone";
                case "hallow":
                    return "Halloween World";
                case "fantas":
                    return "Wonder Land";
                default:
                    return levelName;
            }
        }

        private void UpdateActivity(TpwRpcData rpcData)
        {
            discord.GetActivityManager().UpdateActivity(new Discord.Activity()
            {
                Details = rpcData.InLobby ? "In lobby" : $"In game",
                State = rpcData.InLobby ? null : $"${rpcData.Cash} | {rpcData.GoldenTicketCount} gt | {rpcData.GoldenKeyCount} gk",
                Assets = new Discord.ActivityAssets()
                {
                    LargeImage = rpcData.InLobby ? "tpw-box" : rpcData.Level.Replace("\\", ""),
                    LargeText = rpcData.InLobby ? "In lobby" : PrettifyLevelName(rpcData.Level),
                    SmallImage = "tpw",
                    SmallText = string.IsNullOrEmpty(rpcData.SaveName) ? null : $"Playing as {rpcData.SaveName}"
                },
                Timestamps = new Discord.ActivityTimestamps()
                {
                    Start = startTime
                }
            }, (res) =>
            {
                if (res == Discord.Result.Ok)
                    Console.WriteLine("Set activity.");
                else
                    Console.WriteLine($"Set activity failed: {res}");
            });
        }
    }
}

using System;
using System.Threading;

namespace ThemeParkRPC
{
    class Program
    {
        static Discord.Discord discord;
        static long startTime;

        static void Main(string[] args)
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
             */

            discord = new Discord.Discord(784571542221750283, (ulong)Discord.CreateFlags.Default);

            Console.WriteLine("Looking for tp.exe..."); 

            var found = false;
            while (!found)
            {
                Thread.Sleep(250);
                found = Memory.Attach("tp_patched");
            }

            startTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            while (true)
            {
                var cash = Memory.ReadMemory<int>(0x007CB2DC);
                var level = Memory.ReadMemoryString(0x00871F74, 6);
                var lastLoad = Memory.ReadMemoryString(0x00FAA40D, 6);
                // var visitorCount = Memory.ReadMemory<int>(0x007CB360);

                var inLobby = cash == 0 || level != lastLoad;

                UpdateActivity(cash, inLobby, level);
                discord.RunCallbacks();
                Thread.Sleep(1000);
            }
        }

        private static string PrettifyLevelName(string levelName)
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

        private static void UpdateActivity(int cash, bool inLobby, string level)
        {
            discord.GetActivityManager().UpdateActivity(new Discord.Activity()
            {
                State = (inLobby) ? "In lobby" : PrettifyLevelName(level),
                Details = inLobby ? null : $"${cash}",
                Assets = new Discord.ActivityAssets()
                {
                    LargeImage = "tpw"
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

using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Libraries;
using System;

namespace Oxide.Plugins
{
    [Info("ChatSuite", "Arood", "1.0.0")]
    [Description("A suite of useful utilities for your chat")]

    class ChatSuite : RustPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        private static bool Dev = false;
        private static Dictionary<string, object> Commands;
        private static List<string> OnlinePlayers = new List<string>();
        private int LastAdvert = 0;

        /**
         * Configuration
         */

        protected override void LoadDefaultConfig() { }
        
        private void LoadConfig()
        {
            if (Dev)
                Config.Clear();
            
            string g = "Across-the-board"; // Calling it Across-the-board so it shows up earlier in the config file

            SetConfig(g, "Enable Admin Say", true);
            SetConfig(g, "Admin Name", "SERVER");
            SetConfig(g, "Icon Profile", "0");
            SetConfig(g, "Enable Airdrop Notification", true);
            SetConfig(g, "Enable Helicopter Notification", true);
            SetConfig(g, "Enable Welcome Message", true);
            SetConfig(g, "Enable Join Notification", true);
            SetConfig(g, "Enable Leave Notification", true);
            SetConfig(g, "Enable Advertisements", true);
            SetConfig(g, "Advertisement Interval", 10);
            SetConfig(g, "Welcome Message", "<size=20>Welcome <color=#a2ff79>{player}</color>!</size>\nType <color=#ffd479>/help</color> in chat to see all available commands on this server. Please make sure that you read our <color=#ffd479>/rules</color> as well.\n\nEnjoy your stay!");

            SetConfig("Advertisements", new List<object> {
                "Type /help to learn more about the server",
                "Make sure to read our server /rules"
            });

            SetConfig("Commands", new Dictionary<string, object>
            {
                {
                    "/help", new Dictionary<string, object>
                    {
                        {
                            "broadcast", false
                        },
                        {
                            "text", new List<object> {
                                "Available commands:"
                            }
                        }
                    }
                },
                {
                    "/rules", new Dictionary<string, object>
                    {
                        {
                            "broadcast", false
                        },
                        {
                            "text", new List<object> {
                                "1. No cheating, hacking or glitching",
                                "2. No racism or discrimination",
                                "3. Respect all players",
                                "4. Do not impersonate or \"pretend to be someone\"",
                                "5. Do not spam the chat"
                            }
                        }
                    }
                },
                {
                    "!rules", new Dictionary<string, object>
                    {
                        {
                            "broadcast", true
                        },
                        {
                            "text", new List<object> {
                                "1. No cheating, hacking or glitching",
                                "2. No racism or discrimination",
                                "3. Respect all players",
                                "4. Do not impersonate or \"pretend to be someone\"",
                                "5. Do not spam the chat"
                            }
                        }
                    }
                }
            });

            SaveConfig();

            Commands = (Dictionary<string, object>) Config.Get("Commands");

            lang.RegisterMessages(new Dictionary<string, string> {
                { "Incoming Airdrop", "<color=#a2ff79>Airdrop</color> incoming, drop coordinates are: <color=#a2ff79>{x}, {y}, {z}</color>."},
                { "Incoming Patrol Helicopter", "<color=#ff674e>Patrol Helicopter</color> incoming!" },
                { "Player joined", "<color=#a2ff79>{player}</color> joined from <color=#a2ff79>{country}</color>" },
                { "Player left", "<color=#74c6ff>{player}</color> disconnected (reason: {reason})" }
            }, this);

            permission.RegisterPermission("chatsuite.adminsay", this);
        }

        private T GetConfig<T>(params object[] args)
        {
            string[] stringArgs = GetConfigPath(args);
            if (Config.Get(stringArgs) == null)
                Config.Set(args);
            return (T)Convert.ChangeType(Config.Get(stringArgs), typeof(T));
        }

        private void SetConfig(params object[] args)
        {
            string[] stringArgs = GetConfigPath(args);
            if (Config.Get(stringArgs) == null)
                Config.Set(args);
        }
        
        private string[] GetConfigPath(params object[] args)
        {
            string[] stringArgs = new string[args.Length - 1];
            for (var i = 0; i < args.Length - 1; i++)
                stringArgs[i] = args[i].ToString();
            return stringArgs;
        }

        /**
         * Utilities
         */

        private void Broadcast(string msg = null, object uid = null)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                Say(player, msg, uid);
            }
        }

        private void Say(BasePlayer player, string msg = null, object uid = null)
        {
            rust.SendChatMessage(player, null, msg, uid?.ToString() ?? GetConfig<string>("Across-the-board","Icon Profile","0"));
        }

        /**
         * Startup
         */
        
        void Loaded() {
            LoadConfig();

            if (GetConfig<bool>("Across-the-board","Enable Advertisements",true)) {
                timer.Repeat(GetConfig<float>("Across-the-board","Advertisement Interval",10) * 60, 0, () => AdvertsLoop());
            }

            var c = Interface.Oxide.GetLibrary<Command>();
            if (GetConfig<bool>("Across-the-board","Enable Admin Say",true)) {
                c.AddChatCommand("say", this, "AdminSay");
            }
            
            c.AddChatCommand("players", this, "ListPlayers");
            c.AddChatCommand("welcome", this, "SendWelcome");

            foreach (string Key in Commands.Keys) {
                if (Key.Substring(0,1) == "/") {
                    c.AddChatCommand(Key.Substring(1), this, "HandleCommands");
                }
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnlinePlayers.Add(player.UserIDString);
        }

        /**
         * Advertisements
         */
        private void AdvertsLoop()
        {
            List<object> Adverts = GetConfig<List<object>>("Advertisements", null);

            Broadcast((string) Adverts[LastAdvert]);
            if (Adverts.Count > 1)
            {
                LastAdvert = LastAdvert + 1;
                if (LastAdvert == Adverts.Count) {
                    LastAdvert = 0;
                }
            }
        }

        /**
         * Chat Commands
         */
        void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null) return;

            var msg = arg.GetString(0, "text");
            if (msg.Substring(0,1) == "!") {
                HandleCommands(player, msg, null);
            }
        }

        private void HandleCommands(BasePlayer player, string command, string[] args) {
            if (command.Substring(0,1) != "!") command = "/"+command;
            if (Commands.ContainsKey(command)) {
                var cmd = (Dictionary<string,object>) Commands[command];
                foreach (string t in (List<object>) cmd["text"]) {
                    if (cmd.ContainsKey("broadcast") && (bool) cmd["broadcast"]) {
                        Broadcast(t);
                    } else {
                        Say(player, t);
                    }
                }
            }
        }

        private void ListPlayers(BasePlayer player, string command, string[] args) {
            string msg = "Players online: ";
            List<string> playerNames = new List<string>();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                playerNames.Add(p.displayName);
            msg = msg + string.Join(", ", playerNames.ToArray());
            Say(player, msg);
        }

        /**
         * Admin Say
         */

        private void AdminSay(BasePlayer player, string command, string[] args) {
            if (permission.UserHasPermission(player.UserIDString, "chatsuite.adminsay")) {
                string AdminName = GetConfig<string>("Across-the-board", "Admin Name", "");
                string Message = string.Join(" ", args);

                if (AdminName != "") {
                    Message = AdminName + ": " + Message;
                }

                Broadcast(Message);
            } else {
                Say(player, "You don't have permission to use this command");
            }
        }

        /**
         * Notifications when players connect or disconnect
         */

        void OnPlayerSleepEnded(BasePlayer player)
        {
            string uid = player.UserIDString;

            // TODO: killed while sleeping = error, do actual online check first
            bool isOnline = false;
            foreach (BasePlayer p in BasePlayer.activePlayerList) {
                if (p.UserIDString == uid) isOnline = true;
            }
            
            if (!OnlinePlayers.Contains(uid) && isOnline) {
                OnlinePlayers.Add(uid);

                SendWelcome(player, "", null);

                var country = "Unknown";
                if (PlayerDatabase) {
                    country = (string)PlayerDatabase.Call("GetPlayerData", uid, "country") ?? "Unknown";
                }

                if (country == "Unknown") {
                    var ip = player.net.connection.ipaddress.Split(':')[0];
                    webrequest.EnqueueGet("http://ip-api.com/json/" + ip + "?fields=3", (code, response) => WebRequestFilter(code, response, player), this);
                } else {
                    RealJoinMessage(player, country);
                }
            }
        }

        private void SendWelcome(BasePlayer player, string command, string[] args) {
            if (GetConfig<bool>("Across-the-board","Enable Welcome Message",true)) {
                string msg = GetConfig<string>("Across-the-board","Welcome Message","");
                Say(player, msg.Replace("{player}", player.displayName));
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            string uid = player.UserIDString;
            if (OnlinePlayers.Contains(uid))
            {   
                OnlinePlayers.Remove(uid);
                string msg = lang.GetMessage("Player left", this, null);
                if (GetConfig<bool>("Across-the-board","Enable Leave Notification",true))
                    Broadcast(msg.Replace("{player}", player.displayName).Replace("{reason}", reason));
            }
        }

        private void WebRequestFilter(int code, string response, BasePlayer player)
        {
            string uid = player.UserIDString;
            string country = "Unknown";

            if (response != null || code == 200)
            {
                try {
                    var json = JObject.Parse(response);
                    string _country = json["country"].ToString();
                    if (!String.IsNullOrEmpty(_country))
                    {
                        country = _country;
                        if (PlayerDatabase)
                            PlayerDatabase.Call("SetPlayerData", uid, "country", country);
                    }
                }catch  
                {  
                }  
            }

            RealJoinMessage(player, country);
        }

        private void RealJoinMessage(BasePlayer player, string country) {
            if (!GetConfig<bool>("Across-the-board","Enable Join Notification",true)) return;
            string uid = player.UserIDString;
            Broadcast(lang.GetMessage("Player joined", this, null).Replace("{player}",player.displayName).Replace("{country}",country));
        }

        /**
         * Spawns
         */
        void OnAirdrop(CargoPlane plane, Vector3 location)
        {
            if (GetConfig<bool>("Across-the-board","Enable Airdrop Notification",true)) {
                string Message = lang.GetMessage("Incoming Airdrop", this, null);
                string x = location.x.ToString();
                string y = location.y.ToString();
                string z = location.z.ToString();
                string loc = x + ", " + y + ", " + z;
                Broadcast(Message.Replace("{location}", loc).Replace("{x}", x).Replace("{y}", y).Replace("{z}", z));
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.ShortPrefabName == "patrolhelicopter") {
                if (GetConfig<bool>("Across-the-board","Enable Helicopter Notification",true)) 
                    Broadcast(lang.GetMessage("Incoming Patrol Helicopter", this, null));
            }
        }
    }
}
using System;
using Rainmeter;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by ORelio (c) 2012-2013.
    /// Allows to connect to any Minecraft server, send and receive text, automated scripts.
    /// This source code is released under the CDDL 1.0 License.
    /// </summary>
    internal class MClient
    {
        private static McTcpClient Client;
        public const string Version = "1.7.0";

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>
        public static void Initialize(string username, string password, string serverip)
        {
            Settings.Login = username;
            Settings.Password = password;
            Settings.ServerIP = serverip;

            InitializeClient();
        }

        /// <summary>
        /// Start a new Client
        /// </summary>
        private static void InitializeClient()
        {
            MinecraftCom.LoginResult result;
            Settings.Username = Settings.Login;
            string sessionID = "";
            string UUID = "";

            // Online or offline mode.
            if (Settings.Password == "-")
            {
                API.Log(API.LogType.Warning, "You chose to run in offline mode.");
                result = MinecraftCom.LoginResult.Success;
                sessionID = "0";
            }
            else
            {
                API.Log(API.LogType.Warning, "Connecting to Minecraft.net...");//
                result = MinecraftCom.GetLogin(ref Settings.Username, Settings.Password, ref sessionID, ref UUID);
            }

            #region After connection

            if (result == MinecraftCom.LoginResult.Success)
            {
                API.Log(API.LogType.Warning, "Success. (session ID: " + sessionID + ')'); //

                if (Settings.ServerIP == "")
                {
                    API.Log(API.LogType.Warning, "Server IP not written.");
                    Settings.ServerIP = "localhost";
                }

                //Get server version
                API.Log(API.LogType.Warning, "Retrieving Server Info..."); //

                int protocolversion = 0;
                string version = "";
                if (MinecraftCom.GetServerInfo(Settings.ServerIP, ref protocolversion, ref version))
                {
                    //Supported protocol version ?
                    int[] supportedVersions = {4};
                    if (Array.IndexOf(supportedVersions, protocolversion) > -1)
                    {
                        //Load translations (Minecraft 1.6+)
                        ChatParser.InitTranslations();

                        //Will handle the connection for this client
                        API.Log(API.LogType.Warning, "Version is supported.");
                        MinecraftCom handler = new MinecraftCom();
                        ConsoleIO.SetAutoCompleteEngine(handler);
                        handler.setVersion(protocolversion);

                        #region Bots

                        //Load & initialize bots if needed
                        if (Settings.AntiAFK_Enabled)
                        {
                            handler.BotLoad(new Bots.AntiAFK(Settings.AntiAFK_Delay));
                        }
                        if (Settings.Hangman_Enabled)
                        {
                            handler.BotLoad(new Bots.Pendu(Settings.Hangman_English));
                        }
                        if (Settings.Alerts_Enabled)
                        {
                            handler.BotLoad(new Bots.Alerts());
                        }
                        if (Settings.ChatLog_Enabled)
                        {
                            handler.BotLoad(new Bots.ChatLog(Settings.ChatLog_File, Settings.ChatLog_Filter,
                                Settings.ChatLog_DateTime));
                        }
                        if (Settings.PlayerLog_Enabled)
                        {
                            handler.BotLoad(new Bots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.PlayerLog_File));
                        }
                        if (Settings.AutoRelog_Enabled)
                        {
                            handler.BotLoad(new Bots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries));
                        }
                        if (Settings.xAuth_Enabled)
                        {
                            handler.BotLoad(new Bots.xAuth(Settings.xAuth_Password));
                        }
                        if (Settings.Scripting_Enabled)
                        {
                            handler.BotLoad(new Bots.Scripting(Settings.Scripting_ScriptFile));
                        }

                        #endregion Bots

                        //Start the main TCP client
                        Client = new McTcpClient(Settings.Username, UUID, sessionID, Settings.ServerIP, handler);
                    }
                    else
                    {
                        API.Log(API.LogType.Warning, "Cannot connect to the server : This version is not supported !");
                        ReadLineReconnect();
                    }
                }
                else
                {
                    API.Log(API.LogType.Warning, "Failed to ping this IP.");
                    ReadLineReconnect();
                }
            }
            else
            {
                API.Log(API.LogType.Warning, "Connection failed : ");

                switch (result)
                {
                    case MinecraftCom.LoginResult.AccountMigrated:
                        API.Log(API.LogType.Warning, "Account migrated, use e-mail as username.");
                        break;

                    case MinecraftCom.LoginResult.Blocked:
                        API.Log(API.LogType.Warning, "Too many failed logins. Please try again later.");
                        break;

                    case MinecraftCom.LoginResult.WrongPassword:
                        API.Log(API.LogType.Warning, "Incorrect password.");
                        break;

                    case MinecraftCom.LoginResult.NotPremium:
                        API.Log(API.LogType.Warning, "User not premium.");
                        break;

                    case MinecraftCom.LoginResult.Error:
                        API.Log(API.LogType.Warning, "Network error.");
                        break;
                }

                ReadLineReconnect();
            }

            #endregion After connection
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>
        public static void Restart()
        {
            new System.Threading.Thread(t_restart).Start();
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>
        public static void Exit()
        {
            new System.Threading.Thread(t_exit).Start();
        }

        /// <summary>
        /// Pause the program, usually when an error or a kick occured, letting the user press Enter to quit OR type /reconnect
        /// </summary>
        /// <returns>Return True if the user typed "/reconnect"</returns>
        public static bool ReadLineReconnect()
        {
            string text = ConsoleIO.ReadLine();

            if (text == "reco" || text == "reconnect" || text == "/reco" || text == "/reconnect")
            {
                MClient.Restart();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Private thread for restarting the program. Called through Restart()
        /// </summary>
        private static void t_restart()
        {
            if (Client != null)
            {
                Client.Disconnect(); 
            }

            API.Log(API.LogType.Warning, "Restarting Minecraft Console Client...");
            InitializeClient();
        }

        /// <summary>
        /// Private thread for exiting the program. Called through Exit()
        /// </summary>
        private static void t_exit()
        {
            if (Client != null)
            {
                Client.Disconnect();
            }
            //Environment.Exit(0);
        }
    }
}
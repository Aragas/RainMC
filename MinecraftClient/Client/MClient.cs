using System;

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

        private static void Main(string[] args)
        {
            Initialize("Aragasas", "753951", "localhost");
        }

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

            if (Settings.Password == "-")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("You chose to run in offline mode.");
                Console.ForegroundColor = ConsoleColor.Gray;
                result = MinecraftCom.LoginResult.Success;
                sessionID = "0";
            }
            else
            {
                Console.WriteLine("Connecting to Minecraft.net...");
                result = MinecraftCom.GetLogin(ref Settings.Username, Settings.Password, ref sessionID, ref UUID);
            }
            if (result == MinecraftCom.LoginResult.Success)
            {
                Console.WriteLine("Success. (session ID: " + sessionID + ')');
                if (Settings.ServerIP == "")
                {
                    Console.Write("Server IP : ");
                    Settings.ServerIP = Console.ReadLine();
                }

                //Get server version
                Console.WriteLine("Retrieving Server Info...");
                int protocolversion = 0; string version = "";
                if (MinecraftCom.GetServerInfo(Settings.ServerIP, ref protocolversion, ref version))
                {
                    //Supported protocol version ?
                    int[] supportedVersions = { 4 };
                    if (Array.IndexOf(supportedVersions, protocolversion) > -1)
                    {
                        //Load translations (Minecraft 1.6+)
                        ChatParser.InitTranslations();

                        //Will handle the connection for this client
                        Console.WriteLine("Version is supported.");
                        MinecraftCom handler = new MinecraftCom();
                        ConsoleIO.SetAutoCompleteEngine(handler);
                        handler.setVersion(protocolversion);

                        //Load & initialize bots if needed
                        if (Settings.AntiAFK_Enabled) { handler.BotLoad(new Bots.AntiAFK(Settings.AntiAFK_Delay)); }
                        if (Settings.Hangman_Enabled) { handler.BotLoad(new Bots.Pendu(Settings.Hangman_English)); }
                        if (Settings.Alerts_Enabled) { handler.BotLoad(new Bots.Alerts()); }
                        if (Settings.ChatLog_Enabled) { handler.BotLoad(new Bots.ChatLog(Settings.ChatLog_File, Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                        if (Settings.PlayerLog_Enabled) { handler.BotLoad(new Bots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.PlayerLog_File)); }
                        if (Settings.AutoRelog_Enabled) { handler.BotLoad(new Bots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries)); }
                        if (Settings.xAuth_Enabled) { handler.BotLoad(new Bots.xAuth(Settings.xAuth_Password)); }
                        if (Settings.Scripting_Enabled) { handler.BotLoad(new Bots.Scripting(Settings.Scripting_ScriptFile)); }

                        //Start the main TCP client
                        if (Settings.SingleCommand != "")
                        {
                            Client = new McTcpClient(Settings.Username, UUID, sessionID, Settings.ServerIP, handler, Settings.SingleCommand);
                        }
                        else Client = new McTcpClient(Settings.Username, UUID, sessionID, Settings.ServerIP, handler);
                    }
                    else
                    {
                        Console.WriteLine("Cannot connect to the server : This version is not supported !");
                        ReadLineReconnect();
                    }
                }
                else
                {
                    Console.WriteLine("Failed to ping this IP.");
                    ReadLineReconnect();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("Connection failed : ");
                switch (result)
                {
                    case MinecraftCom.LoginResult.AccountMigrated: Console.WriteLine("Account migrated, use e-mail as username."); break;
                    case MinecraftCom.LoginResult.Blocked: Console.WriteLine("Too many failed logins. Please try again later."); break;
                    case MinecraftCom.LoginResult.WrongPassword: Console.WriteLine("Incorrect password."); break;
                    case MinecraftCom.LoginResult.NotPremium: Console.WriteLine("User not premium."); break;
                    case MinecraftCom.LoginResult.Error: Console.WriteLine("Network error."); break;
                }
                while (Console.KeyAvailable) { Console.ReadKey(false); }
                if (Settings.SingleCommand == "") { ReadLineReconnect(); }
            }
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>
        public static void Restart()
        {
            new System.Threading.Thread(new System.Threading.ThreadStart(t_restart)).Start();
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>
        public static void Exit()
        {
            new System.Threading.Thread(new System.Threading.ThreadStart(t_exit)).Start();
        }

        /// <summary>
        /// Pause the program, usually when an error or a kick occured, letting the user press Enter to quit OR type /reconnect
        /// </summary>
        /// <returns>Return True if the user typed "/reconnect"</returns>
        public static bool ReadLineReconnect()
        {
            string text = Console.ReadLine();
            if (text == "reco" || text == "reconnect" || text == "/reco" || text == "/reconnect")
            {
                MClient.Restart();
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Private thread for restarting the program. Called through Restart()
        /// </summary>
        private static void t_restart()
        {
            if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
            Console.WriteLine("Restarting Minecraft Console Client...");
            InitializeClient();
        }

        /// <summary>
        /// Private thread for exiting the program. Called through Exit()
        /// </summary>
        private static void t_exit()
        {
            if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
            Environment.Exit(0);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using Rainmeter;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// It allows message sending and text receiving.
    /// </summary>
    class McTcpClient
    {
        public static int AttemptsLeft = 0;

        string host;
        int port;
        string username;
        string text;
        Thread t_updater;
        Thread t_sender;
        TcpClient client;
        MinecraftCom handler;

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_port">The server IP (serveradress or serveradress:port)</param>
        public McTcpClient(string username, string uuid, string sessionID, string server_port, MinecraftCom handler)
        {
            StartClient(username, uuid, sessionID, server_port, handler);
        }

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="user">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_port">The server IP (serveradress or serveradress:port)/param>
        /// <param name="singlecommand">If set to true, the client will send a single command and then disconnect from the server</param>
        /// <param name="command">The text or command to send. Will only be sent if singlecommand is set to true.</param>
        private void StartClient(string user, string uuid, string sessionID, string server_port, MinecraftCom handler)
        {
            this.handler = handler;
            username = user;
            string[] sip = server_port.Split(':');
            host = sip[0];
            if (sip.Length == 1)
            {
                port = 25565;
            }
            else
            {
                try
                {
                    port = Convert.ToInt32(sip[1]);
                }
                catch (FormatException) { port = 25565; }
            }

            try
            {
                API.Log(API.LogType.Warning, "Logging in...");
                client = new TcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                handler.setClient(client);
                if (handler.Login(user, uuid, sessionID, host, port))
                {
                    //Single command sending

                    API.Log(API.LogType.Warning, "Server was successfuly joined.");
                    API.Log(API.LogType.Warning, "Type '/quit' to leave the server.");

                    //Command sending thread, allowing user input
                    t_sender = new Thread(new ThreadStart(StartTalk));
                    t_sender.Name = "CommandSender";
                    t_sender.Start();

                    //Data receiving thread, allowing text receiving
                    t_updater = new Thread(new ThreadStart(Updater));
                    t_updater.Name = "PacketHandler";
                    t_updater.Start();

                }
                else
                {
                    API.Log(API.LogType.Warning, "Login failed.");
                    MClient.ReadLineReconnect();
                }
            }
            catch (SocketException)
            {
                API.Log(API.LogType.Warning, "Failed to connect to this IP.");
                if (AttemptsLeft > 0)
                {
                    ChatBot.LogToConsole("Waiting 5 seconds (" + AttemptsLeft + " attempts left)...");
                    Thread.Sleep(5000); AttemptsLeft--; MClient.Restart();
                }
            }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and to leave the server.
        /// Will be automatically called on a separate Thread by StartClient()
        /// </summary>
        private void StartTalk()
        {
            try
            {
                while (client.Client.Connected)
                {
                    text = ConsoleIO.ReadLine();
                    if (ConsoleIO.BasicIO && text.Length > 0 && text[0] == (char)0x00)
                    {
                        //Process a request from the GUI
                        string[] command = text.Substring(1).Split((char)0x00);
                        switch (command[0].ToLower())
                        {
                            case "autocomplete":
                                if (command.Length > 1)
                                {
                                    ConsoleIO.Write((char)0x00 + "autocomplete" + (char)0x00 + handler.AutoComplete(command[1]));
                                }
                                else Console.Write((char)0x00 + "autocomplete" + (char)0x00);
                                break;
                        }
                    }
                    else
                    {
                        if (text.ToLower() == "/quit" || text.ToLower().StartsWith("/exec ") ||
                            text.ToLower() == "/reco" || text.ToLower() == "/reconnect")
                        {
                            break;
                        }
                        while (text.Length > 0 && text[0] == ' ')
                        {
                            text = text.Substring(1);
                        }

                        if (text != "")
                        {
                            //Message is too long
                            if (text.Length > 100)
                            {
                                if (text[0] == '/')
                                {
                                    //Send the first 100 chars of the command
                                    text = text.Substring(0, 100);
                                    handler.SendChatMessage(text);
                                }
                                else
                                {
                                    //Send the message splitted in sereval messages
                                    while (text.Length > 100)
                                    {
                                        handler.SendChatMessage(text.Substring(0, 100));
                                        text = text.Substring(100, text.Length - 100);
                                    }
                                    handler.SendChatMessage(text);
                                }
                            }
                            else handler.SendChatMessage(text);
                        }
                    }
                }

                if (text.ToLower() == "/quit")
                {
                    ConsoleIO.Write("You have left the server.");
                    Disconnect();
                }

                else if (text.ToLower().StartsWith("/exec ")) {
                    handler.BotLoad(new Bots.Scripting("config/" + text.Split()[1]));
                }


                else if (text.ToLower() == "/reco" || text.ToLower() == "/reconnect")
                {
                    ConsoleIO.Write("You have left the server.");
                    handler.SendRespawnPacket();
                    MClient.Restart();
                }
            }
            catch (IOException) { }
        }

        /// <summary>
        /// Receive the data (including chat messages) from the server, and keep the connection alive.
        /// Will be automatically called on a separate Thread by StartClient()
        /// </summary>
        private void Updater()
        {
            try
            {
                //handler.DebugDump();
                do
                {
                    Thread.Sleep(100);
                } while (handler.Update());
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (!handler.HasBeenKicked)
            {
                ConsoleIO.Write("Connection has been lost.");
                if (!handler.OnConnectionLost() && !MClient.ReadLineReconnect()) { t_sender.Abort(); }
            }
            else if (MClient.ReadLineReconnect()) { t_sender.Abort(); }
        }

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>
        public void Disconnect()
        {
            handler.Disconnect("disconnect.quitting");
            Thread.Sleep(1000);
            if (t_updater != null) { t_updater.Abort(); }
            if (t_sender != null) { t_sender.Abort(); }
            if (client != null) { client.Close(); }
        }
    }
}

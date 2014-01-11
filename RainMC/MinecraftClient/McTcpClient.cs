using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// It allows message sending and text receiving.
    /// </summary>
    class McTcpClient
    {
        public static int AttemptsLeft;

        string _host;
        int _port;
        string _username;
        string _text;
        Thread _tUpdater;
        Thread _tSender;
        TcpClient _client;
        MinecraftCom _handler;

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="serverPort">The server IP (serveradress or serveradress:port)</param>
        public McTcpClient(string username, string uuid, string sessionID, string serverPort, MinecraftCom handler)
        {
            StartClient(username, uuid, sessionID, serverPort, handler);
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
            _handler = handler;
            _username = user;
            string[] sip = server_port.Split(':');
            _host = sip[0];
            if (sip.Length == 1)
            {
                _port = 25565;
            }
            else
            {
                try
                {
                    _port = Convert.ToInt32(sip[1]);
                }
                catch (FormatException)
                {
                    _port = 25565;
                }
            }

            try
            {
                ConsoleIO.Write("Logging in...");

                _client = new TcpClient(_host, _port) {ReceiveBufferSize = 1024*1024};
                handler.SetClient(_client);

                if (handler.Login(user, uuid, sessionID, _host, _port))
                {
                    //Single command sending

                    ConsoleIO.Write("Server was successfuly joined.");
                    ConsoleIO.Write("Type '/quit' to leave the server.");

                    //Command sending thread, allowing user input
                    _tSender = new Thread(StartTalk) {Name = "CommandSender"};
                    _tSender.Start();

                    //Data receiving thread, allowing text receiving
                    _tUpdater = new Thread(Updater) {Name = "PacketHandler"};
                    _tUpdater.Start();

                }
                else
                {
                    ConsoleIO.Write("Login failed.");
                    MClient.ReadLineReconnect();
                }
            }
            catch (SocketException)
            {
                ConsoleIO.Write("Failed to connect to this IP.");

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
                while (_client.Client.Connected)
                {
                    _text = ConsoleIO.ReadLine();

                    if (_text.Length > 0 && _text[0] == (char)0x00)
                    {
                        //Process a request from the GUI
                        string[] command = _text.Substring(1).Split((char)0x00);
                        switch (command[0].ToLower())
                        {
                            case "autocomplete":
                                if (command.Length > 1)
                                    ConsoleIO.Write((char)0x00 + "autocomplete" + (char)0x00 + _handler.AutoComplete(command[1]));
                                
                                else 
                                    Console.Write((char)0x00 + "autocomplete" + (char)0x00);
                                break;
                        }
                    }
                    else
                    {
                        if (_text.ToLower() == "/quit" || _text.ToLower().StartsWith("/exec ") ||
                            _text.ToLower() == "/reco" || _text.ToLower() == "/reconnect")
                            break;
                        
                        while (_text.Length > 0 && _text[0] == ' ')
                        {
                            _text = _text.Substring(1);
                        }

                        if (_text != "")
                        {
                            //Message is too long
                            if (_text.Length > 100)
                            {
                                if (_text[0] == '/')
                                {
                                    //Send the first 100 chars of the command
                                    _text = _text.Substring(0, 100);
                                    _handler.SendChatMessage(_text);
                                }
                                else
                                {
                                    //Send the message splitted in sereval messages
                                    while (_text.Length > 100)
                                    {
                                        _handler.SendChatMessage(_text.Substring(0, 100));
                                        _text = _text.Substring(100, _text.Length - 100);
                                    }
                                    _handler.SendChatMessage(_text);
                                }
                            }
                            else _handler.SendChatMessage(_text);
                        }
                    }
                }

                if (_text.ToLower() == "/quit")
                {
                    ConsoleIO.Write("You have left the server.");
                    Disconnect();
                }

                else if (_text.ToLower().StartsWith("/exec ")) 
                    _handler.BotLoad(new Bots.Scripting("config/" + _text.Split()[1]));
                


                else if (_text.ToLower() == "/reco" || _text.ToLower() == "/reconnect")
                {
                    ConsoleIO.Write("You have left the server.");
                    _handler.SendRespawnPacket();
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
                do
                {
                    Thread.Sleep(100);
                } while (_handler.Update());
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (!_handler.HasBeenKicked)
            {
                ConsoleIO.Write("Connection has been lost.");

                if (!_handler.OnConnectionLost() && !MClient.ReadLineReconnect()) 
                    _tSender.Abort();
            }
            else if (MClient.ReadLineReconnect())
                _tSender.Abort();
           
        }

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>
        public void Disconnect()
        {
            _handler.Disconnect("disconnect.quitting");

            Thread.Sleep(1000);

            if (_tUpdater != null)
                _tUpdater.Abort();
            
            if (_tSender != null)
                _tSender.Abort();
            
            if (_client != null)
                _client.Close();
            
        }
    }
}

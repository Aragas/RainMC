using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace MinecraftClient
{
    /// <summary>
    /// The class containing all the core functions needed to communicate with a Minecraft server.
    /// </summary>

    public class MinecraftCom : IAutoComplete
    {
        #region Login to Minecraft.net and get a new session ID

        public enum LoginResult { Error, Success, WrongPassword, Blocked, AccountMigrated, NotPremium };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="accesstoken">Will contain the access token returned by Minecraft.net, if the login is successful</param>
        /// <param name="uuid">Will contain the player's UUID, needed for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>
        public static LoginResult GetLogin(ref string user, string pass, ref string accesstoken, ref string uuid)
        {
            try
            {
                WebClient wClient = new WebClient();
                wClient.Headers.Add("Content-Type: application/json");
                string json_request = "{\"agent\": { \"name\": \"Minecraft\", \"version\": 1 }, \"username\": \"" + user + "\", \"password\": \"" + pass + "\" }";
                string result = wClient.UploadString("https://authserver.mojang.com/authenticate", json_request);
                if (result.Contains("availableProfiles\":[]}"))
                {
                    return LoginResult.NotPremium;
                }
                else
                {
                    string[] temp = result.Split(new string[] { "accessToken\":\"" }, StringSplitOptions.RemoveEmptyEntries);

                    if (temp.Length >= 2) 
                        accesstoken = temp[1].Split('"')[0];

                    temp = result.Split(new string[] { "name\":\"" }, StringSplitOptions.RemoveEmptyEntries);

                    if (temp.Length >= 2)
                        user = temp[1].Split('"')[0];
                    
                    temp = result.Split(new string[] { "availableProfiles\":[{\"id\":\"" }, StringSplitOptions.RemoveEmptyEntries);

                    if (temp.Length >= 2)
                        uuid = temp[1].Split('"')[0];
                    

                    return LoginResult.Success;
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse response = (HttpWebResponse)e.Response;
                    if ((int)response.StatusCode == 403)
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
                        {
                            string result = sr.ReadToEnd();

                            if (result.Contains("UserMigratedException"))
                                return LoginResult.AccountMigrated;
                            
                            else 
                                return LoginResult.WrongPassword;
                        }
                    }
                    else return LoginResult.Blocked;
                }
                else return LoginResult.Error;
            }
        }

        #endregion

        #region Session checking when joining a server in online mode

        /// <summary>
        /// Check session using the Yggdrasil authentication scheme. Allow to join an online-mode server
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="accesstoken">Session ID</param>
        /// <param name="serverhash">Server ID</param>
        /// <returns>TRUE if session was successfully checked</returns>
        public static bool SessionCheck(string uuid, string accesstoken, string serverhash)
        {
            try
            {
                WebClient wClient = new WebClient();
                wClient.Headers.Add("Content-Type: application/json");
                string jsonRequest = "{\"accessToken\":\"" + accesstoken + "\",\"selectedProfile\":\"" + uuid +
                                     "\",\"serverId\":\"" + serverhash + "\"}";
                return (wClient.UploadString("https://sessionserver.mojang.com/session/minecraft/join", jsonRequest) ==
                        "");
            }
            catch (WebException)
            {
                return false;
            }
        }

        #endregion

        TcpClient _c = new TcpClient();
        Crypto.AesStream _s;

        public bool HasBeenKicked
        {
            get
            {
                return connectionlost;
            }
        }
        bool connectionlost;
        bool encrypted;
        int protocolversion;

        public bool Update()
        {
            for (int i = 0; i < _bots.Count; i++)
            {
                _bots[i].Update();
            }

            if (_c.Client == null || !_c.Connected)
                return false;

            try
            {
                while (_c.Client.Available > 0)
                {
                    int size = ReadNextVarInt();
                    int id = ReadNextVarInt();

                    switch (id)
                    {
                        case 0x00:
                            byte[] keepalive = new byte[4] {0, 0, 0, 0};
                            Receive(keepalive, 0, 4, SocketFlags.None);
                            byte[] keepalive_packet = ConcatBytes(GetVarInt(0x00), keepalive);
                            byte[] keepalive_tosend = ConcatBytes(GetVarInt(keepalive_packet.Length), keepalive_packet);
                            Send(keepalive_tosend);
                            break;

                        case 0x02:
                            string message = ReadNextString();
                            //printstring("§8" + message, false); //Debug : Show the RAW JSON data
                            message = ChatParser.ParseText(message);
                            PrintString(message, false);
                            for (int i = 0; i < _bots.Count; i++)
                            {
                                _bots[i].GetText(message);
                            }
                            break;

                        case 0x37:
                            int stats_count = ReadNextVarInt();
                            for (int i = 0; i < stats_count; i++)
                            {
                                string stat_name = ReadNextString();
                                ReadNextVarInt(); //stat value
                                if (stat_name == "stat.deaths")
                                    PrintString("You are dead. Type /reco to respawn & reconnect.", false);
                            }
                            break;

                        case 0x3A:
                            int autocomplete_count = ReadNextVarInt();
                            string tab_list = "";
                            for (int i = 0; i < autocomplete_count; i++)
                            {
                                _autocompleteResult = ReadNextString();
                                if (_autocompleteResult != "")
                                    tab_list = tab_list + _autocompleteResult + " ";
                            }
                            _autocompleteReceived = true;
                            tab_list = tab_list.Trim();
                            if (tab_list.Length > 0)
                                PrintString("§8" + tab_list, false);
                            break;

                        case 0x40:
                            string reason = ChatParser.ParseText(ReadNextString());
                            ConsoleIO.Write("Disconnected by Server :");
                            PrintString(reason, true);
                            connectionlost = true;
                            for (int i = 0; i < _bots.Count; i++)
                                _bots[i].OnDisconnect(ChatBot.DisconnectReason.InGameKick, reason);
                            return false;

                        default:
                            ReadData(size - GetVarInt(id).Length); //Skip packet
                            break;
                    }
                }
            }
            catch (SocketException)
            {
                return false;
            }
            return true;
        }

        public void DebugDump()
        {
            byte[] cache = new byte[128000];
            Receive(cache, 0, 128000, SocketFlags.None);
            string dump = BitConverter.ToString(cache);
            System.IO.File.WriteAllText("debug.txt", dump);
            System.Diagnostics.Process.Start("debug.txt");
        }
        public bool OnConnectionLost()
        {
            if (!connectionlost)
            {
                connectionlost = true;
                for (int i = 0; i < _bots.Count; i++)
                {
                    if (_bots[i].OnDisconnect(ChatBot.DisconnectReason.ConnectionLost, "Connection has been lost."))
                    {
                        return true; //The client is about to restart
                    }
                }
            }
            return false;
        }

        private void ReadData(int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    Receive(cache, 0, offset, SocketFlags.None);
                }
                catch (OutOfMemoryException) { }
            }
        }
        private string ReadNextString()
        {
            int length = ReadNextVarInt();
            if (length > 0)
            {
                byte[] cache = new byte[length];
                Receive(cache, 0, length, SocketFlags.None);
                string result = Encoding.UTF8.GetString(cache);
                return result;
            }
            else return "";
        }
        private byte[] ReadNextByteArray()
        {
            byte[] tmp = new byte[2];
            Receive(tmp, 0, 2, SocketFlags.None);
            Array.Reverse(tmp);
            short len = BitConverter.ToInt16(tmp, 0);
            byte[] data = new byte[len];
            Receive(data, 0, len, SocketFlags.None);
            return data;
        }
        private int ReadNextVarInt()
        {
            int i = 0;
            int j = 0;
            int k = 0;
            byte[] tmp = new byte[1];
            while (true)
            {
                Receive(tmp, 0, 1, SocketFlags.None);
                k = tmp[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }
        private static byte[] GetVarInt(int paramInt)
        {
            List<byte> bytes = new List<byte>();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte)(paramInt & 127 | 128));
                paramInt = (int)(((uint)paramInt) >> 7);
            }
            bytes.Add((byte)paramInt);
            return bytes.ToArray();
        }
        private static byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }
        private static int Atoi(string str)
        {
            return Int32.Parse(Regex.Match(str, @"\d+").Value);
        }

        private static void SetColor(char c)
        {
            switch (c)
            {
                case '0': Console.ForegroundColor = ConsoleColor.Gray; break; //Should be Black but Black is non-readable on a black background
                case '1': Console.ForegroundColor = ConsoleColor.DarkBlue; break;
                case '2': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case '3': Console.ForegroundColor = ConsoleColor.DarkCyan; break;
                case '4': Console.ForegroundColor = ConsoleColor.DarkRed; break;
                case '5': Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                case '6': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case '7': Console.ForegroundColor = ConsoleColor.Gray; break;
                case '8': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                case '9': Console.ForegroundColor = ConsoleColor.Blue; break;
                case 'a': Console.ForegroundColor = ConsoleColor.Green; break;
                case 'b': Console.ForegroundColor = ConsoleColor.Cyan; break;
                case 'c': Console.ForegroundColor = ConsoleColor.Red; break;
                case 'd': Console.ForegroundColor = ConsoleColor.Magenta; break;
                case 'e': Console.ForegroundColor = ConsoleColor.Yellow; break;
                case 'f': Console.ForegroundColor = ConsoleColor.White; break;
                case 'r': Console.ForegroundColor = ConsoleColor.White; break;
            }
        }
        private static void PrintString(string str, bool acceptnewlines)
        {
            if (!String.IsNullOrEmpty(str))
            {
                if (!acceptnewlines)
                {
                    str = str.Replace('\n', ' ');
                }

                string[] subs = str.Split(new char[] { '§' });
                if (subs[0].Length > 0)
                {
                    ConsoleIO.Write(subs[0]);
                }

                string text = null;
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 0)
                    {
                        SetColor(subs[i][0]);
                        if (subs[i].Length > 1)
                        {
                            text += subs[i].Substring(1, subs[i].Length - 1);
                        }
                    }
                }
                ConsoleIO.Write(text);
            }
        }

        private bool _autocompleteReceived;
        private string _autocompleteResult = "";
        public string AutoComplete(string behindCursor)
        {
            if (String.IsNullOrEmpty(behindCursor))
                return "";

            byte[] packet_id = GetVarInt(0x14);
            byte[] tocomplete_val = Encoding.UTF8.GetBytes(behindCursor);
            byte[] tocomplete_len = GetVarInt(tocomplete_val.Length);
            byte[] tabcomplete_packet = ConcatBytes(packet_id, tocomplete_len, tocomplete_val);
            byte[] tabcomplete_packet_tosend = ConcatBytes(GetVarInt(tabcomplete_packet.Length), tabcomplete_packet);

            _autocompleteReceived = false;
            _autocompleteResult = behindCursor;
            Send(tabcomplete_packet_tosend);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !_autocompleteReceived) { System.Threading.Thread.Sleep(100); wait_left--; }
            return _autocompleteResult;
        }

        public void SetVersion(int ver)
        {
            protocolversion = ver;
        }

        public void SetClient(TcpClient n)
        {
            _c = n;
        }

        private void SetEncryptedClient(Crypto.AesStream n)
        {
            _s = n; 
            encrypted = true;
        }

        private void Receive(byte[] buffer, int start, int offset, SocketFlags f)
        {
            while (_c.Client.Available < start + offset) { }
            if (encrypted)
            {
                _s.Read(buffer, start, offset);
            }
            else _c.Client.Receive(buffer, start, offset, f);
        }

        private void Send(byte[] buffer)
        {
            if (encrypted)
            {
                _s.Write(buffer, 0, buffer.Length);
            }
            else _c.Client.Send(buffer);
        }

        public static bool GetServerInfo(string serverIP, ref int protocolversion, ref string version)
        {
            try
            {
                string host; int port;
                string[] sip = serverIP.Split(':');
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

                TcpClient tcp = new TcpClient(host, port);
                
                byte[] packet_id = GetVarInt(0);
                byte[] protocol_version = GetVarInt(4);
                byte[] server_adress_val = Encoding.UTF8.GetBytes(host);
                byte[] server_adress_len = GetVarInt(server_adress_val.Length);
                byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
                byte[] next_state = GetVarInt(1);
                byte[] packet = ConcatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
                byte[] tosend = ConcatBytes(GetVarInt(packet.Length), packet);

                tcp.Client.Send(tosend, SocketFlags.None);

                byte[] status_request = GetVarInt(0);
                byte[] request_packet = ConcatBytes(GetVarInt(status_request.Length), status_request);

                tcp.Client.Send(request_packet, SocketFlags.None);

                MinecraftCom ComTmp = new MinecraftCom();
                ComTmp.SetClient(tcp);
                if (ComTmp.ReadNextVarInt() > 0) //Read Response length
                {
                    if (ComTmp.ReadNextVarInt() == 0x00) //Read Packet ID
                    {
                        string result = ComTmp.ReadNextString(); //Get the Json data
                        if (result[0] == '{' && result.Contains("protocol\":") && result.Contains("name\":\""))
                        {
                            string[] tmp_ver = result.Split(new string[] { "protocol\":" }, StringSplitOptions.None);
                            string[] tmp_name = result.Split(new string[] { "name\":\"" }, StringSplitOptions.None);
                            if (tmp_ver.Length >= 2 && tmp_name.Length >= 2)
                            {
                                protocolversion = Atoi(tmp_ver[1]);
                                version = tmp_name[1].Split('"')[0];
                                //ConsoleIO.Write(result); //Debug: show the full Json string
                                ConsoleIO.Write("Server version : " + version + " (protocol v" + protocolversion + ").");
                                return true;
                            }
                        }
                    }
                }
                ConsoleIO.Write("Unexpected answer from the server (is that a MC 1.7+ server ?)");
                return false;
            }
            catch
            {
                ConsoleIO.Write("An error occured while attempting to connect to this IP.");
                return false;
            }
        }

        public bool Login(string username, string uuid, string sessionID, string host, int port)
        {
            byte[] packet_id = GetVarInt(0);
            byte[] protocol_version = GetVarInt(4);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(host);
            byte[] server_adress_len = GetVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = GetVarInt(2);
            byte[] handshake_packet = ConcatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
            byte[] handshake_packet_tosend = ConcatBytes(GetVarInt(handshake_packet.Length), handshake_packet);

            Send(handshake_packet_tosend);

            byte[] username_val = Encoding.UTF8.GetBytes(username);
            byte[] username_len = GetVarInt(username_val.Length);
            byte[] login_packet = ConcatBytes(packet_id, username_len, username_val);
            byte[] login_packet_tosend = ConcatBytes(GetVarInt(login_packet.Length), login_packet);

            Send(login_packet_tosend);

            ReadNextVarInt(); //Packet size
            int pid = ReadNextVarInt(); //Packet ID
            if (pid == 0x00) //Login rejected
            {
                ConsoleIO.Write("Login rejected by Server :");
                PrintString(ChatParser.ParseText(ReadNextString()), true);
                return false;
            }
            else if (pid == 0x01) //Encryption request
            {
                string serverID = ReadNextString();
                byte[] Serverkey_RAW = ReadNextByteArray();
                byte[] token = ReadNextByteArray();
                var PublicServerkey = Crypto.GenerateRSAPublicKey(Serverkey_RAW);
                var SecretKey = Crypto.GenerateAESPrivateKey();
                return StartEncryption(uuid, sessionID, token, serverID, PublicServerkey, SecretKey);
            }
            else if (pid == 0x02) //Login successfull
            {
                ConsoleIO.Write("Server is in offline mode.");
                return true; //No need to check session or start encryption
            }
            else return false;
        }

        public bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, java.security.PublicKey serverKey, javax.crypto.SecretKey secretKey)
        {
            ConsoleIO.Write("Crypto keys & hash generated.");

            if (serverIDhash != "-")
            {
                ConsoleIO.Write("Checking Session...");
                if (!SessionCheck(uuid, sessionID, new java.math.BigInteger(Crypto.GetServerHash(serverIDhash, serverKey, secretKey)).toString(16)))
                {
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = Crypto.Encrypt(serverKey, secretKey.getEncoded());
            byte[] token_enc = Crypto.Encrypt(serverKey, token);
            byte[] key_len = BitConverter.GetBytes((short)key_enc.Length); Array.Reverse(key_len);
            byte[] token_len = BitConverter.GetBytes((short)token_enc.Length); Array.Reverse(token_len);

            //Encryption Response packet
            byte[] packet_id = GetVarInt(0x01);
            byte[] encryption_response = ConcatBytes(packet_id, key_len, key_enc, token_len, token_enc);
            byte[] encryption_response_tosend = ConcatBytes(GetVarInt(encryption_response.Length), encryption_response);
            Send(encryption_response_tosend);

            //Start client-side encryption
            SetEncryptedClient(Crypto.SwitchToAesMode(_c.GetStream(), secretKey));

            //Get the next packet
            ReadNextVarInt(); //Skip Packet size (not needed)
            return (ReadNextVarInt() == 0x02); //Packet ID. 0x02 = Login Success
        }

        public bool SendChatMessage(string message)
        {
            if (String.IsNullOrEmpty(message))
                return true;
            try
            {
                byte[] packet_id = GetVarInt(0x01);
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = GetVarInt(message_val.Length);
                byte[] message_packet = ConcatBytes(packet_id, message_len, message_val);
                byte[] message_packet_tosend = ConcatBytes(GetVarInt(message_packet.Length), message_packet);
                Send(message_packet_tosend);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public bool SendRespawnPacket()
        {
            try
            {
                byte[] packet_id = GetVarInt(0x16);
                byte[] action_id = { 0 };
                byte[] respawn_packet = ConcatBytes(GetVarInt(packet_id.Length + 1), packet_id, action_id);
                Send(respawn_packet);
                return true;
            }
            catch (SocketException) { return false; }
        }

        public void Disconnect(string message)
        {
            if (message == null)
                message = "";

            try
            {
                byte[] packet_id = GetVarInt(0x40);
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = GetVarInt(message_val.Length);
                byte[] disconnect_packet = ConcatBytes(packet_id, message_len, message_val);
                byte[] disconnect_packet_tosend = ConcatBytes(GetVarInt(disconnect_packet.Length), disconnect_packet);
                Send(disconnect_packet_tosend);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
        }

        private readonly List<ChatBot> _bots = new List<ChatBot>();
        public void BotLoad(ChatBot b) { b.SetHandler(this); _bots.Add(b); b.Initialize(); }
        public void BotUnLoad(ChatBot b) { _bots.RemoveAll(item => object.ReferenceEquals(item, b)); }
        public void BotClear() { _bots.Clear(); }
    }
}

using System;
using System.Text;
using System.Threading;
using MineLib.Network;
using MineLib.Network.Enums;
using MineLib.Network.Packets;
using MineLib.Network.Packets.Client;
using MineLib.Network.Packets.Client.Login;

namespace Minecraft
{
    /// <summary>
    ///     Wrapper for Network of MineLib.Net.
    /// </summary>
    public partial class Bot : IMinecraft, IDisposable
    {
        #region Variables

        public string AccessToken { get; set; }

        public string ClientName { get; set; }

        public string ClientLogin { get; set; }

        public string ClientToken { get; set; }

        public string SelectedProfile { get; set; }

        public string ClientPassword { get; set; }

        public string ClientBrand { get; set; }

        public string ServerBrand { get; set; }

        public bool VerifyNames { get; set; }

        public string ServerIP { get; set; }

        public short ServerPort { get; set; }

        public ServerState State { get; set; }

        #endregion Variables

        public bool Connected { get { return Handler != null && Handler.Connected; } }

        private Thread _connecter;

        private NetworkHandler Handler;

        /// <summary>
        ///     Create a new Minecraft Instance
        /// </summary>
        /// <param name="username">The username to use when connecting to Minecraft</param>
        /// <param name="password">The password to use when connecting to Minecraft</param>
        public Bot(string login, string password, string username = "")
        {
            ClientLogin = login;

            ClientName = string.IsNullOrEmpty(username) ? ClientLogin : username;

            ClientPassword = password;
            VerifyNames = !string.IsNullOrEmpty(password);
            ClientBrand = "RainMC"; // -- Used in the plugin message reporting the client brand to the server.

            if (VerifyNames)
                Login();
        }

        /// <summary>
        ///     Connects to the Minecraft Server.
        /// </summary>
        /// <param name="ip">The IP of the server to connect to</param>
        /// <param name="port">The port of the server to connect to</param>
        public void Connect(string ip)
        {
            var parts = ip.Split(':');

            ServerIP = parts[0];

            try { ServerPort = Convert.ToInt16(parts[1]); }
            catch (Exception) { ServerPort = 25565; }

            Handler = new NetworkHandler(this);

            // -- Register our event handlers.
            Handler.OnPacketHandled += RaisePacketHandled;

            // -- Connect to the server and begin reading packets.
            if (!Handler.Start())
            {
                Dispose();
                return;
            }

            _connecter = new Thread(SendConnectPackets) { Name = "FirstPacketsSender"};
            _connecter.Start();

        }

        private void SendConnectPackets()
        {
            SendPacket(new HandshakePacket
            {
                ProtocolVersion = 4, ServerAddress = ServerIP, ServerPort = ServerPort, NextState = NextState.Login,
            });

            SendPacket(new LoginStartPacket { Name = ClientName});

            var x = 0;
            while (State == ServerState.Login)
            {
                if (x > 3)
                    Dispose();
                x++;
                Thread.Sleep(500);
            }

            SendPacket(new PluginMessagePacket
            {
                Channel = "MC|Brand", Data = Encoding.UTF8.GetBytes("RainMC")
            });

            SendPacket(new ClientStatusPacket {Status = ClientStatus.Respawn});

            Thread.CurrentThread.Abort();
        }

        /// <summary>
        ///     Send IPacket to the Minecraft Server.
        /// </summary>
        /// <param name="packet">IPacket to sent to server</param>
        public void SendPacket(IPacket packet)
        {
            if (Handler != null && Connected)
                Handler.Send(packet);
        }

        public void SendChatMessage(string message)
        {
            //var encoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
            //var bytes = encoding.GetBytes(message);
            //var converted = Encoding.Convert(encoding, Encoding.UTF8, bytes);
            //message = Encoding.UTF8.GetString(converted);
            if (Handler != null && Connected)
                Handler.Send(new ChatMessagePacket { Message = message });
        }

        /// <summary>
        ///     Disconnects from the Minecraft server.
        /// </summary>
        public void Disconnect()
        {
            // -- Reset all variables to default so we can make a new connection.

            SendPacket(new DisconnectPacket { Reason = "disconnect.quitting" });

            State = ServerState.Login;
        }

        public void Dispose()
        {
            Disconnect();

            if (Handler != null)
                Handler.Dispose();

            if(_connecter != null)
                _connecter.Abort();
        }

    }
}
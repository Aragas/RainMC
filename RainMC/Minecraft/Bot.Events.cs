using System.Text;
using MineLib.Network.Enums;
using MineLib.Network.Packets;
using MineLib.Network.Packets.Server;

namespace Minecraft
{
    public partial class Bot
    {
        public delegate void ChatMessageReceived(string message);
        public event ChatMessageReceived OnChatMessageReceived;

        private void OnKeepAlive(IPacket packet)
        {
            var keepAlive = (KeepAlivePacket) packet;

            SendPacket(keepAlive);
        }

        private void OnJoinGame(IPacket packet)
        {
            var joinGame = (JoinGamePacket) packet;

        }

        private void OnChatMessage(IPacket packet)
        {
            var chatMessage = (ChatMessagePacket) packet;

            OnChatMessageReceived(ChatParser.ParseText(chatMessage.Message));
        }

        private void OnTimeUpdate(IPacket packet)
        {
            var timeUpdate = (TimeUpdatePacket) packet;

        }
       
        private void OnSpawnPosition(IPacket packet)
        {
            var spawnPosition = (SpawnPositionPacket) packet;

        }

        private void OnUpdateHealth(IPacket packet)
        {
            var updateHealth = (UpdateHealthPacket) packet;

        }

        private void OnRespawn(IPacket packet)
        {
            var respawn = (RespawnPacket) packet;

        }

        private void OnPlayerPositionAndLook(IPacket packet)
        {
            var playerPositionAndLook = (PlayerPositionAndLookPacket) packet;

            SendPacket(new MineLib.Network.Packets.Client.ClientStatusPacket { Status = ClientStatus.Respawn });

            SendPacket(new MineLib.Network.Packets.Client.PlayerPositionPacket
            {
                X = playerPositionAndLook.X,
                HeadY = playerPositionAndLook.Y + 1.74,
                FeetY = playerPositionAndLook.Y + 1.74 - 1.62,
                Z = playerPositionAndLook.Z,
                OnGround = playerPositionAndLook.OnGround
            });

            SendPacket(new MineLib.Network.Packets.Client.PlayerLookPacket
            {
                Yaw = playerPositionAndLook.Yaw,
                Pitch = playerPositionAndLook.Pitch,
                OnGround = playerPositionAndLook.OnGround
            });
        }

        private void OnChangeGameState(IPacket packet)
        {
            var changeGameState = (ChangeGameStatePacket) packet;

        }

        private void OnStatistics(IPacket packet)
        {
            var statistics = (StatisticsPacket) packet;

        }

        private void OnPlayerListItem(IPacket packet)
        {
            var playerListItem = (PlayerListItemPacket) packet;
			
        }

        private void OnPlayerAbilities(IPacket packet)
        {
            var playerAbilities = (PlayerAbilitiesPacket) packet;

        }

        private void OnTabComplete(IPacket packet)
        {
            var tabComplete = (TabCompletePacket)packet;

        }

        private void OnScoreboardObjective(IPacket packet)
        {
            var scoreboardObjective = (ScoreboardObjectivePacket)packet;

        }

        private void OnUpdateScore(IPacket packet)
        {
            var updateScore = (UpdateScorePacket)packet;

        }

        private void OnDisplayScoreboard(IPacket packet)
        {
            var displayScoreboard = (DisplayScoreboardPacket)packet;

        }

        private void OnPluginMessage(IPacket packet)
        {
            var pluginMessage = (PluginMessagePacket) packet;

            switch (pluginMessage.Channel)
            {
                case "MC|Brand":
                    ServerBrand = Encoding.UTF8.GetString(pluginMessage.Data);
                    break;

                default:
                    break;
            }
        }

        private void OnDisconnect(IPacket packet)
        {
            var disconnect = (DisconnectPacket)packet;

        }

    }
}
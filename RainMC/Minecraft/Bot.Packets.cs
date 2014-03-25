using MineLib.Network.Enums;
using MineLib.Network.Packets;

namespace Minecraft
{
    public partial class Bot
    {
        private void RaisePacketHandled(IPacket packet, int id, ServerState state)
        {
            switch (state)
            {
                case ServerState.Login:

                    #region Login

                    switch ((PacketsServer)id)
                    {
                        case PacketsServer.LoginDisconnect:
                            // Dispose();
                            break;

                        case PacketsServer.EncryptionRequest:
                            // -- NetworkHandler do all stuff automatic
                            break;

                        case PacketsServer.LoginSuccess:
                            State = ServerState.Play;
                            break;
                    }

                    #endregion Login

                    break;

                case ServerState.Play:

                    #region Play

                    switch ((PacketsServer)id)
                    {
                        case PacketsServer.KeepAlive:
                            OnKeepAlive(packet);
                            break;

                        case PacketsServer.JoinGame:
                            OnJoinGame(packet);
                            break;

                        case PacketsServer.ChatMessage:
                            OnChatMessage(packet);
                            break;

                        case PacketsServer.TimeUpdate:
                            OnTimeUpdate(packet);
                            break;

                        case PacketsServer.EntityEquipment:
                            break;

                        case PacketsServer.SpawnPosition:
                            OnSpawnPosition(packet);
                            break;

                        case PacketsServer.UpdateHealth:
                            OnUpdateHealth(packet);
                            break;

                        case PacketsServer.Respawn:
                            OnRespawn(packet);
                            break;

                        case PacketsServer.PlayerPositionAndLook:
                            OnPlayerPositionAndLook(packet);
                            break;

                        case PacketsServer.HeldItemChange:
                            break;

                        case PacketsServer.UseBed:
                            break;

                        case PacketsServer.Animation:
                            break;

                        case PacketsServer.SpawnPlayer:
                            break;

                        case PacketsServer.CollectItem:
                            break;

                        case PacketsServer.SpawnObject:
                            break;

                        case PacketsServer.SpawnMob:
                            break;

                        case PacketsServer.SpawnPainting:
                            break;

                        case PacketsServer.SpawnExperienceOrb:
                            break;

                        case PacketsServer.EntityVelocity:
                            break;

                        case PacketsServer.DestroyEntities:
                            break;

                        case PacketsServer.Entity:
                            break;

                        case PacketsServer.EntityRelativeMove:
                            break;

                        case PacketsServer.EntityLook:
                            break;

                        case PacketsServer.EntityLookAndRelativeMove:
                            break;

                        case PacketsServer.EntityTeleport:
                            break;

                        case PacketsServer.EntityHeadLook:
                            break;

                        case PacketsServer.EntityStatus:
                            break;

                        case PacketsServer.AttachEntity:
                            break;

                        case PacketsServer.EntityMetadata:
                            break;

                        case PacketsServer.EntityEffect:
                            break;

                        case PacketsServer.RemoveEntityEffect:
                            break;

                        case PacketsServer.SetExperience:
                            break;

                        case PacketsServer.EntityProperties:
                            break;

                        case PacketsServer.ChunkData:
                            break;

                        case PacketsServer.MultiBlockChange:
                            break;

                        case PacketsServer.BlockChange:
                            break;

                        case PacketsServer.BlockAction:
                            break;

                        case PacketsServer.BlockBreakAnimation:
                            break;

                        case PacketsServer.MapChunkBulk:
                            break;

                        case PacketsServer.Explosion:
                            break;

                        case PacketsServer.Effect:
                            break;

                        case PacketsServer.SoundEffect:
                            break;

                        case PacketsServer.Particle:
                            break;

                        case PacketsServer.ChangeGameState:
                            OnChangeGameState(packet);
                            break;

                        case PacketsServer.SpawnGlobalEntity:
                            break;

                        case PacketsServer.OpenWindow:
                            break;

                        case PacketsServer.CloseWindow:
                            break;

                        case PacketsServer.SetSlot:
                            break;

                        case PacketsServer.WindowItems:
                            break;

                        case PacketsServer.WindowProperty:
                            break;

                        case PacketsServer.ConfirmTransaction:
                            break;

                        case PacketsServer.UpdateSign:
                            break;

                        case PacketsServer.Maps:
                            break;

                        case PacketsServer.UpdateBlockEntity:
                            break;

                        case PacketsServer.SignEditorOpen:
                            break;

                        case PacketsServer.Statistics:
                            OnStatistics(packet);
                            break;

                        case PacketsServer.PlayerListItem:
                            OnPlayerListItem(packet);
                            break;

                        case PacketsServer.PlayerAbilities:
                            OnPlayerAbilities(packet);
                            break;

                        case PacketsServer.TabComplete:
                            OnTabComplete(packet);
                            break;

                        case PacketsServer.ScoreboardObjective:
                            OnScoreboardObjective(packet);
                            break;

                        case PacketsServer.UpdateScore:
                            OnUpdateScore(packet);
                            break;

                        case PacketsServer.DisplayScoreboard:
                            OnDisplayScoreboard(packet);
                            break;

                        case PacketsServer.Teams:
                            break;

                        case PacketsServer.PluginMessage:
                            OnPluginMessage(packet);
                            break;

                        case PacketsServer.Disconnect:
                            OnDisconnect(packet);
                            break;
                    }

                    #endregion

                    break;
            }
        }
    }
}
using MineLib.Network;

namespace Minecraft
{
    public partial class Bot
    {
        /// <summary>
        ///     Login to Minecraft.net and store credentials
        /// </summary>
        private void Login()
        {
            if (VerifyNames)
            {
                var result = Yggdrasil.Login(ClientName, ClientPassword);

                switch (result.Status)
                {
                    case YggdrasilStatus.Success:
                        AccessToken = result.Response.AccessToken;
                        ClientToken = result.Response.ClientToken;
                        SelectedProfile = result.Response.Profile.ID;
                        break;

                    default:
                        VerifyNames = false; // -- Fall back to no auth.
                        break;
                }
            }
            else
            {
                AccessToken = "None";
                SelectedProfile = "None";
            }
        }

        /// <summary>
        ///     Uses a client's stored credentials to verify with Minecraft.net
        /// </summary>
        public bool RefreshSession()
        {
            if (!VerifyNames)
                return false;

            var result = Yggdrasil.RefreshSession(AccessToken, ClientToken);

            switch (result.Status)
            {
                case YggdrasilStatus.Success:
                    AccessToken = result.Response.AccessToken;
                    ClientToken = result.Response.ClientToken;
                    return true;

                default:
                    return false;
            }
        }

        public bool VerifySession()
        {
            return VerifyNames && Yggdrasil.VerifySession(AccessToken);
        }

        public bool Invalidate()
        {
            return VerifyNames && Yggdrasil.Invalidate(AccessToken, ClientToken);
        }

        public bool Logout()
        {
            return VerifyNames && Yggdrasil.Logout(ClientName, ClientPassword);
        }
    }
}

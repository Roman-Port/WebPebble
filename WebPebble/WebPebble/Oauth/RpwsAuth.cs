using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble.Oauth
{
    public static class RpwsAuth
    {
        public static E_RPWS_User AuthenticateUser(string token)
        {
            //Request the API.
            try
            {
                return LibRpws.LibRpwsCore.GetObjectHttp<RpwsMeReply>("https://blue.api.get-rpws.com/v1/rpws_me/", token).user;
            } catch
            {
                return null;
            }
        }

        class RpwsMeReply
        {
            public E_RPWS_User user;
        }
    }
}

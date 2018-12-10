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
                var u = LibRpws.LibRpwsCore.GetObjectHttp<RpwsMeReply>("https://blue.api.get-rpws.com/v1/rpws_me/", token).user;
                if (u != null)
                    u.token = token;
                return u;
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

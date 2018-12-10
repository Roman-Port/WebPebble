using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebPebble.Entities;
using WebPebble.Oauth;

namespace WebPebble.Services.Me
{
    public class OneTimeRequestTokens
    {
        /// <summary>
        /// Key: Token; Value: User token
        /// </summary>
        public static Dictionary<string, string> oneTimeRequestTokens = new Dictionary<string, string>();

        /// <summary>
        /// Generates a one-time request token for assets. This will grant access to any service one time and is used mostly for requesting assets. It can only be sent via query.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="user"></param>
        /// <param name="proj"></param>
        /// <returns></returns>
        public static async Task CreateOneTimeRequestToken(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject proj)
        {
            //Generate a token
            string token = LibRpws.LibRpwsCore.GenerateRandomString(32);
            while (oneTimeRequestTokens.ContainsKey(token))
                token = LibRpws.LibRpwsCore.GenerateRandomString(32);

            //Insert
            oneTimeRequestTokens.Add(token, user.token);

            //Respond
            await Program.QuickWriteJsonToDoc(e, new Dictionary<string, string>
            {
                {"token", token }
            });
        }
    }
}

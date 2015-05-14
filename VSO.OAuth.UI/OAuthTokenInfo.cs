using System.Collections.Generic;

namespace VSO.OAuth.UI
{
    public class OAuthOptions
    {
        public string AuthUrl { get; set; }
        public string TokenUrl { get; set; }
        public string AppId { get; set; }
        public string ClientSecret { get; set; }
        public string CallbackUrl { get; set; }
        public string[] Scopes { get; set; }
    }

    public class OAuthTokenInfo
    {
        public OAuthTokenInfo()
        {
        }

        public OAuthTokenInfo(IReadOnlyDictionary<string, string> parameters)
        {
            //ugly :(
            string accessToken, refreshToken, expiresIn;

            parameters.TryGetValue("access_token", out accessToken);
            parameters.TryGetValue("refresh_token", out refreshToken);
            parameters.TryGetValue("expires_in", out expiresIn);

            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
        }

        public string AccessToken { get; set; }

        public string ExpiresIn { get; set; }

        public string RefreshToken { get; set; }
    }
}
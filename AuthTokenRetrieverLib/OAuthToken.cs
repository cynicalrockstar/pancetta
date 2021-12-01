using Newtonsoft.Json;
using System;

namespace Reddit.AuthTokenRetriever
{
    [Serializable]
    public class OAuthToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken;

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType;

        [JsonProperty(PropertyName = "expires_in")]
        public string ExpiresIn;

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken;

        [JsonProperty(PropertyName = "ExpiresAt")]
        public DateTime ExpiresAt;

        //public OAuthToken(string accessToken = null, string refreshToken = null)
        //{
        //    AccessToken = accessToken;
        //    RefreshToken = refreshToken;
        //}
    }
}

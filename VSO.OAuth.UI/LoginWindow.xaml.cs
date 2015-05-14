using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Newtonsoft.Json;

namespace VSO.OAuth.UI
{
    public partial class LoginWindow
    {
        private OAuthOptions _options;

        public event EventHandler<OAuthTokenInfo> LoginCompleted;
        
        public LoginWindow()
        {
            InitializeComponent();
            Browser.Navigating += BrowserOnNavigating;
            Browser.Navigated += BrowserOnNavigated;
        }

        public void Login(OAuthOptions options)
        {
            _options = options;
            var authUrl = GetAuthUrl();
            Browser.Navigate(authUrl);
        }

        private void BrowserOnNavigating(object sender, NavigatingCancelEventArgs navigatingCancelEventArgs)
        {
            Url.Text = navigatingCancelEventArgs.Uri.ToString();
        }

        private string GetAuthUrl()
        {
            return string.Format("{0}?client_id={1}" +
                                 "&response_type=Assertion" +
                                 "&scope={2}" +
                                 "&redirect_uri={3}",
                                    _options.AuthUrl,
                                    _options.AppId,
                                    string.Join("%20", _options.Scopes),
                                    _options.CallbackUrl);
        }


        private async void BrowserOnNavigated(object sender, NavigationEventArgs navigationEventArgs)
        {
            var uri = navigationEventArgs.Uri;
            var uriText = uri.ToString();
            Url.Text = uriText;

            if (!uriText.StartsWith(_options.CallbackUrl)) 
                return;

            var queryString = !string.IsNullOrWhiteSpace(uri.Query) && uri.Query.StartsWith("?") 
                ? uri.Query.Substring(1) 
                : string.Empty;
            var parameters =
                queryString
                    .Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(parameter => parameter.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(k => k.ElementAt(0), v => v.ElementAt(1));

            //got the auth code
            if (parameters.ContainsKey("code"))
            {
                string authCode;
                parameters.TryGetValue("code", out authCode);

                //now get the access token
                await GetAccessToken(authCode);
            }

        }

        private async Task GetAccessToken(string authCode)
        {
            using (var httpClient = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
                    { "client_assertion", _options.ClientSecret },
                    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                    { "assertion", authCode },
                    { "redirect_uri", _options.CallbackUrl }
                });

                var response = await httpClient.PostAsync(_options.TokenUrl, content);
                var json = await response.Content.ReadAsStringAsync();
                var parameters = 
                    JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json)
                               .ToDictionary(k => k.Key, v => (string)v.Value.ToString());

                if (LoginCompleted != null)
                    LoginCompleted(this, new OAuthTokenInfo(parameters));
            }
        }
    }
}

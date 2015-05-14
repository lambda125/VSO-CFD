module Auth

open Newtonsoft.Json
open System.IO
open VSO.OAuth.UI
open System.Configuration
open VisualStudioOnline.Api.Rest.V1.Client
open System

let login () =

    let readToken filename =
        if File.Exists(filename) then 
            let t = JsonConvert.DeserializeObject<OAuthTokenInfo>(File.ReadAllText filename) 
            Some t
        else
            None

    let persistToken token =
        let filename = token.GetType().Name
        JsonConvert.SerializeObject(token)
        |> fun s -> System.IO.File.WriteAllText (filename, s)

    let config = ConfigurationManager.AppSettings

    let isValid (token:OAuthTokenInfo option) = 
        async {
            if token.IsNone
            then
                return false
            else 
            
                //make sure existing token is valid, if not login in again
                //TODO: we could try refreshing the token here etc.

                //just check with a call to VSO get projects
                let accountName = config.["AccountName"]
                let vso = VsoClient(accountName, token.Value.AccessToken)

                try
                    let wit = vso.GetService<IVsoWit>()
                    let! result = wit.GetFields() |> Async.AwaitTask
                    return true
                with
                    | ex -> 
                    //assuming this is most likely a HTTP 203 type thing here
                    //TODO check what actually went wrong 
                    File.AppendAllText("AuthErrors.txt", ex.ToString())
                    return false
        }

    let existingToken = readToken (typedefof<OAuthTokenInfo>.Name)

    async {

        let! isTokenValid = existingToken |> isValid
        if isTokenValid
        then
            return existingToken.Value
        else

            let options = OAuthOptions(
                                AuthUrl = config.["AuthUrl"],
                                TokenUrl = config.["TokenUrl"],
                                AppId = config.["AppId"],
                                ClientSecret = config.["ClientSecret"],
                                Scopes = config.["Scopes"].Split(' '),
                                CallbackUrl = config.["CallbackUrl"]
                            )

            let! tokenFromLogin = Login.Start(options) |> Async.AwaitTask
            persistToken tokenFromLogin
            return tokenFromLogin
            
    }

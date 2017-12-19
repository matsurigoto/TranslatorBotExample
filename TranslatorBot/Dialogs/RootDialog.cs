using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Net;
using System.Web;
using RestSharp;

namespace TranslatorBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private string APIPassword = "837xxxxxxx";

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            string response = string.Empty;

            if (activity.Text.StartsWith("tran") || activity.Text.StartsWith("翻譯"))
            {
                string authToken = string.Empty;
                authToken = await AuthoizeService(APIPassword);
                response = await TranslateString(authToken, activity.Text.Replace("tran", "").Replace("翻譯",""));
            }

            // return our reply to the user
            await context.PostAsync($"{response}");

            context.Wait(MessageReceivedAsync);
        }

        private async Task<string> AuthoizeService(string key)
        {
            var authTokenSource = new AzureAuthToken(key.Trim());
            string authToken;
            try
            {
                authToken = await authTokenSource.GetAccessTokenAsync();
            }
            catch (HttpRequestException)
            {
                if (authTokenSource.RequestStatusCode == HttpStatusCode.Unauthorized)
                {
                    return "Request to token service is not authorized (401). Check that the Azure subscription key is valid.";
                }
                if (authTokenSource.RequestStatusCode == HttpStatusCode.Forbidden)
                {
                    return "Request to token service is not authorized (403). For accounts in the free-tier, check that the account quota is not exceeded.";
                }
                throw;
            }

            return authToken;
        }

        private async Task<string> TranslateString(string authToken, string text)
        {
            string from = "en";
            string to = "zh";
            string uri = "/v2/Http.svc/Translate?text=" + HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to;

            string result = string.Empty;
            var client = new RestClient("https://api.microsofttranslator.com");
            var request = new RestRequest(uri, Method.GET);
            request.AddHeader("Authorization", authToken);
            var response = await client.ExecuteTaskAsync(request);

            return response.Content.Replace("<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">", "").Replace("</string>", "");
        }
    }
}
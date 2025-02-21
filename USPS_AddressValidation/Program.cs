using System;
using System.Security.Principal;
using Newtonsoft.Json;

class Program
{
    private static readonly string baseUrl = "https://apis.usps.com/addresses/v3/address";

    //Token Auhtorization
    private static readonly string tokenEndpoint = "https://apis.usps.com/oauth2/v3/token";

    //Replace clientID and clientSecrect with yours own from your registered account with USPS
    private static readonly string clientId = "xxxxxxxxxxxxx"; 
    private static readonly string clientSecret = "xxxxxxxxx";

    static async Task Main(string[] args)
    {
        string uspsToken = await GetOAuthTokenAsync();

        string requestUrl = baseUrl + "?";

        string Address1 = "9150 IMPERIAL HWY";
        string Address2 = "";
        string City = "Downey";
        string State = "CA";
        string Zip = "";

        Address1 = System.Web.HttpUtility.UrlEncode(Address1);
        Address2 = System.Web.HttpUtility.UrlEncode(Address2);
        City = System.Web.HttpUtility.UrlEncode(City);

        string requestAddress = "streetAddress=" + Address1 + "&secondaryAddress=" + Address2 + "&city=" + City + "&state=" + State + "&ZIPCode=" + Zip;

        //Remove Zip parameter when it's not provided
        if (Zip == "") { requestAddress = requestAddress.Replace("&ZIPCode=", ""); }

        requestUrl = $"{requestUrl + requestAddress}";

        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {uspsToken}");

        try {
            HttpResponseMessage response = await client.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var reponse = JsonConvert.DeserializeObject<Root>(responseBody);

            Console.WriteLine("Response from USPS API:");
            Console.WriteLine("Address line 1: " + reponse?.Address.StreetAddress);
            Console.WriteLine("Address line 2: " + reponse?.Address.SecondaryAddress);
            Console.WriteLine("City: " + reponse?.Address.City);
            Console.WriteLine("State: " + reponse?.Address.State);
            Console.WriteLine("Zip: " + reponse?.Address.ZIPCode);
            Console.WriteLine("Zip+4: " + reponse?.Address.ZIPPlus4);
        }
        catch (HttpRequestException e) {
            Console.WriteLine($"Request error: {e.Message}");
        }

        return;
    }

    public class Root
    {
        public required string Firm { get; set; }
        public required Address Address { get; set; }
        public required List<object> Corrections { get; set; }
        public required List<object> Matches { get; set; }
        public required List<string> Warnings { get; set; }
    }

    public class Address
    {
        public required string StreetAddress { get; set; }
        public required string SecondaryAddress { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string ZIPCode { get; set; }
        public required string ZIPPlus4 { get; set; }
    }

    private static Dictionary<string, object> _cache = new Dictionary<string, object>();

    private static async Task<string> GetOAuthTokenAsync()
    {
        using (var client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            // Prepare the form data
            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "addresses")
            });

            request.Content = content;

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode) {
                string responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

                //USPS does not provide token expire time
                //_cache["ExpireIn"] = tokenResponse.ExpireIn;

                return tokenResponse.AccessToken;
            }
            else {
                throw new Exception($"Error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; set; }

        [JsonProperty("expire_in")]
        public required string ExpireIn { get; set; }
    }
}
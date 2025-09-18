using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace burbodek.Services
{
    public class Paymongo : IPaymongo, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public Paymongo(IConfiguration config)
        {
            _secretKey = config["PayMongo:SecretKey"];
            _httpClient = new HttpClient();
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_secretKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        // Create Checkout Session
        public async Task<string> CreateCheckoutSession(
         decimal amount,
         string currency,
         string name,
         string email,
         string contact,
         string productNames)
        {
            var payload = new
            {
                data = new
                {
                    attributes = new
                    {
                        line_items = new[]
                        {
                    new {
                        name = productNames,
                        amount = (int)(amount * 100), // in cents
                        currency = currency,
                        quantity = 1
                    }
                },
                        payment_method_types = new[] { "gcash" },
                        success_url = "https://localhost:7280/Payments/Success",
                        cancel_url = "https://localhost:7280/Payments/Cancel",
                        billing = new
                        {
                            name = name,
                            email = email,
                            phone = contact
                        },
                        metadata = new
                        {
                            customer_name = name,
                            customer_email = email,
                            customer_contact = contact
                        }
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.paymongo.com/v1/checkout_sessions", content);

            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error creating checkout session: {response.StatusCode} - {responseString}");
            }

            return responseString;
        }


        // Fetch checkout session details
        public async Task<string> GetCheckoutSession(string sessionId)
        {
            var response = await _httpClient.GetAsync($"https://api.paymongo.com/v1/checkout_sessions/{sessionId}");
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error getting checkout session: {response.StatusCode} - {responseString}");
            }
            return responseString;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

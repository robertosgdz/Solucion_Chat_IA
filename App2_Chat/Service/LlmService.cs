using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

//en App2, cambia el namespace a: namespace App2_Chat.Services
namespace App2_Chat.Services
{
    public class LlmService
    {
        private readonly HttpClient _httpClient;

        public LlmService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
        }

        public async Task<string> GetResponseFromLlmAsync(string prompt, string systemPrompt, string model, string apiUrl, double temp, int maxTokens)
        {
            try
            {
                // Math.Min elige el número más pequeño de los dos.
                double calculatedTopP = Math.Min(temp, 1.0);
                int calculatedTopK = temp > 0.7 ? 100 : 40;

                var requestData = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = prompt }
                    },
                    temperature = temp,
                    top_p = calculatedTopP,
                    top_k = calculatedTopK,
                    max_tokens = maxTokens
                };

                string jsonContent = JsonConvert.SerializeObject(requestData);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error HTTP: {response.StatusCode}";
                }

                string responseString = await response.Content.ReadAsStringAsync();

                // Parseo seguro del JSON
                JObject jsonResponse = JObject.Parse(responseString);
                string cleanResponse = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

                return cleanResponse ?? "Error: Respuesta vacía del LLM";
            }
            catch (Exception ex)
            {
                return $"Excepción LLM: {ex.Message}";
            }
        }
    }
}

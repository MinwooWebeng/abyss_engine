namespace AbyssCLI.Aml.API
{
    public class WebFetchApi
    {
        private static readonly HttpClient client = new HttpClient();

        public WebFetchApi()
        {
            client.Timeout = TimeSpan.FromSeconds(30); // Set a timeout of 30 seconds
        }

        // Fetch the content from a URL
        public async Task<string> FetchAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();  // Throws an exception if the HTTP response status is an error
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                // Log or handle error as needed
                return $"Error: {ex.Message}";
            }
        }

        // POST data to the given URL
        public async Task<string> PostAsync(string url, string jsonData)
        {
            try
            {
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                // Log or handle error as needed
                return $"Error: {ex.Message}";
            }
        }
    }
}

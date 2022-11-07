using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleFunctionApp.SmokeTests
{
    public class HttpJsonWrapper
    {
        private Uri _baseURL;

        public HttpJsonWrapper(Uri baseURL)
        {
            _baseURL = baseURL;
        }

        public async Task<T> GetAsync<T>(string path, string authenticationToken = null)
        {
            string content = await GetAsyncWorker<T>(path, authenticationToken);

            if (content != null)
                return JsonSerializer.Deserialize<T>(content);
            return default(T);
        }

        private async Task<string> GetAsyncWorker<T>(string path, string authenticationToken)
        {
            HttpResponseMessage response = await CreateClient(authenticationToken).GetAsync(BuildURL(path));

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            HandleStatusCodes(response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task PutAsync<T>(string path, T value, string authenticationToken = null)
        {
            HttpResponseMessage response = await CreateClient(authenticationToken).PutAsync(BuildURL(path), CreateContent(value));

            HandleStatusCodes(response.StatusCode);
        }

        public async Task<T> PostAsync<T>(string path, string authenticationToken = null)
        {
            HttpResponseMessage response;
            string content;

            response = await CreateClient(authenticationToken).PostAsync(BuildURL(path), null);
            HandleStatusCodes(response.StatusCode);
            content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(content);
        }

        public async Task PostAsync<T>(string path, T value, string authenticationToken = null)
        {
            HttpResponseMessage response = await CreateClient(authenticationToken).PostAsync(BuildURL(path), CreateContent(value));

            HandleStatusCodes(response.StatusCode);
        }

        public async Task<T1> PostAsync<T1, T2>(string path, T2 value, string authenticationToken = null)
        {
            HttpResponseMessage response;
            string content;

            response = await CreateClient(authenticationToken).PostAsync(BuildURL(path), CreateContent(value));
            HandleStatusCodes(response.StatusCode);
            content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T1>(content);
        }

        public async Task DeleteAsync(string path, string authenticationToken = null)
        {
            HttpResponseMessage response = await CreateClient(authenticationToken).DeleteAsync(BuildURL(path));

            HandleStatusCodes(response.StatusCode);
        }

        #region Helpers

        private HttpClient CreateClient(string authenticationToken)
        {
            HttpClient client;

            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(authenticationToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationToken);

            return client;
        }

        private Uri BuildURL(string path)
        {
            return new Uri(_baseURL, path);
        }

        private StringContent CreateContent<T>(T value)
        {
            return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
        }

        private void HandleStatusCodes(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NoContent:
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new SecurityException();
                default:
                    throw new WebException("Unsuccessful attempt.  HTTP status code: " + statusCode.ToString(), WebExceptionStatus.UnknownError);
            }
        }

        #endregion
    }
}
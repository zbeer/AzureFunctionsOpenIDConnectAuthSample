using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SampleFunctionApp.SmokeTests
{
    public class HttpFormEncodeWrapper
    {
        private Uri _baseURL;

        public HttpFormEncodeWrapper(Uri baseURL)
        {
            _baseURL = baseURL;
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

        #region Helpers

        private HttpClient CreateClient(string authenticationToken)
        {
            HttpClient client;

            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            if (!string.IsNullOrWhiteSpace(authenticationToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationToken);

            return client;
        }

        private Uri BuildURL(string path)
        {
            return new Uri(_baseURL, path);
        }

        private FormUrlEncodedContent CreateContent<T>(T value)
        {
            Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(value));

            return new FormUrlEncodedContent(data);
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
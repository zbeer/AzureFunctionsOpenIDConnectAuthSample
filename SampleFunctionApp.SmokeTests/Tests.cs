using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace SampleFunctionApp.SmokeTests
{
    public class AuthInput
    {
        public AuthInput()
        {
            grant_type = "client_credentials";
        }

        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string audience { get; set; }
    }

    public class AuthOutput
    {
        public string access_token { get; set; }
    }

    public class HelloFunctionInput
    {
        public string name { get; set; }
    }

    [Parallelizable(ParallelScope.Self)]
    public class Tests
    {
        public Tests()
        {
            IConfigurationBuilder builder;
            IConfigurationRoot configuration;

            builder = new ConfigurationBuilder().SetBasePath(System.AppContext.BaseDirectory)
                                                .AddJsonFile("appsettings.json");
            configuration = builder.Build();

            _apiWrapper = new HttpJsonWrapper(new Uri(configuration["API_URL"]));
            _authWrappter = new HttpFormEncodeWrapper(new Uri(configuration["AUTH_URL"]));
            _clientId = configuration["CLIENT_ID"];
            _clientSecret = configuration["CLIENT_SECRET"];
            _audience = configuration["AUDIENCE"];
        }

        private readonly HttpJsonWrapper _apiWrapper;
        private readonly HttpFormEncodeWrapper _authWrappter; 
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _audience;

        [Category("Smoke")]
        [Test]
        public async Task HelloPost_ShouldNotFail_WhenCorrectlyAccessed()
        {
            AuthInput authInput;
            AuthOutput authOutput;
            string result;

            authInput = new AuthInput()
            {
                audience = _audience,
                client_id = _clientId,
                client_secret = _clientSecret,
            };
            authOutput = await _authWrappter.PostAsync<AuthOutput, AuthInput>("/oauth/token", authInput);

            result = await _apiWrapper.PostAsync<string, HelloFunctionInput>("/api/HelloFunction", 
                                                                             new HelloFunctionInput() { name = "world" }, 
                                                                             authOutput.access_token);

            Assert.AreEqual("Hello, world", result);
        }
    }
}
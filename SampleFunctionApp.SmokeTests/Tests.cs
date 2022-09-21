using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;

namespace SampleFunctionApp.SmokeTests
{
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

            _wrapper = new HttpJsonWrapper(new Uri(configuration["API_URL"]));
        }

        private readonly HttpJsonWrapper _wrapper;

        [Category("Smoke")]
        [Test]
        public void HelloPost_ShouldNotFail_WhenCorrectlyAccessed()
        {
            _wrapper.PostAsync<String>("/api/HelloFunction",
                                       null,
                                       null).Wait();
        }
    }
}
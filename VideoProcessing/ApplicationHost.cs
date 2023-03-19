using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace test3
{
    public class ApplicationHost<TStartup>
        where TStartup : class
    {
        private readonly string _launchDirectory;
        private IWebHost _webHost;
        private bool _stopRequested;

        public ApplicationHost()
        {
            _launchDirectory = Directory.GetCurrentDirectory();
        }

        public void Start(bool launchedFromConsole)
        {
            var contentRootPath = launchedFromConsole ? _launchDirectory : Directory.GetCurrentDirectory();

            // set up configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(contentRootPath)
                .AddEnvironmentVariables()
                .Build();

            // set up web host
            IWebHostBuilder webHostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config)
                .UseContentRoot(contentRootPath)
                .UseUrls("http://*:5050")
                .UseStartup<TStartup>();

            // create and run host
            _webHost = webHostBuilder.Build();
            _webHost.Services.GetRequiredService<IApplicationLifetime>()
                .ApplicationStopped.Register(() =>
                {
                    if (!_stopRequested)
                        Stop();
                });

            _webHost.Start();

            // print information to console
            if (launchedFromConsole)
            {
                var hostingEnvironment = _webHost.Services.GetService<IHostingEnvironment>();
                Console.WriteLine($"Hosting environment: {hostingEnvironment.EnvironmentName}");
                Console.WriteLine($"Content root path: {hostingEnvironment.ContentRootPath}");

                var serverAddresses = _webHost.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
                foreach (var address in serverAddresses ?? Array.Empty<string>())
                {
                    Console.WriteLine($"Listening on: {address}");
                }
            }
        }

        public void Stop()
        {
            _stopRequested = true;
            _webHost?.Dispose();
        }
    }
}

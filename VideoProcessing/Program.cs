using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using test3.Models;
using test3.Services;
using Topshelf;
using Topshelf.Hosts;
using Host = Microsoft.Extensions.Hosting.Host;

namespace test3
{
    public class Program
    {
        public static Configuration Configuration;
        public static Stopwatch CurrentJobTimer;

        public static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ApplicationHost<Startup>>(s =>
                {
                    s.ConstructUsing(name => new ApplicationHost<Startup>());
                    s.WhenStarted((svc, control) =>
                    {
                        svc.Start(control is ConsoleRunHost);
                        return true;
                    });
                    s.WhenStopped(svc => svc.Stop());
                    s.WhenShutdown(svc => svc.Stop());
                    
                });

                x.SetDisplayName("VideoProcessorApi");
                x.SetServiceName("VideoProcessorApi");
                x.SetDescription("VideoProcessorApi");
                x.StartAutomatically();
                x.RunAsLocalService();
                
               
            });
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

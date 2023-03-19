using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace test3.Services
{
    public class LongRunningService : BackgroundService
    {
        private readonly BackgroundWorkerQueue queue;

        public LongRunningService(BackgroundWorkerQueue queue)
        {
            this.queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await queue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception e)
                {
                    ConsoleManager.AddText(e.Message, false);
                    //throw;
                }
                
            }
        }
    }
}

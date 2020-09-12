using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sequel.Core;
using static System.Runtime.InteropServices.OSPlatform;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace Sequel
{
    public class SequelConfigurationHostedService : IHostedService
    {
        public SequelConfigurationHostedService(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IHostEnvironment Env { get; }
        public IConfiguration Configuration { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await QueryManager.History.ConfigureAsync();

            if (Env.IsProduction())
            {
                OpenDefaultBrowser();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void OpenDefaultBrowser()
        {
            try
            {
                string port = Configuration["urls"].Substring(Configuration["urls"].Length - 4);
                string url = $"http://localhost:{port}";

                if (IsOSPlatform(Windows))
                {
                    var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
                    Process.Start(psi);
                }
                else if (IsOSPlatform(Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (IsOSPlatform(OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch { }
        }
    }
}

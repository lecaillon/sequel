using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sequel.Core;
using Sequel.Models;
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
            Directory.CreateDirectory(Program.RootDirectory);

            await QueryHistoryManager.Optimize();
            await QueryHistoryManager.Configure();
            await TreeViewMenuItem.ConfigureAsync();
            await Snippet.ConfigureAsync();

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
                string port = Configuration["urls"][^4..];
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

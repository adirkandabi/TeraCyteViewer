using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace TeraCyteViewer
{
    public partial class App : Application
    {
        public static IHost? HostApp { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var basePath = AppContext.BaseDirectory;

            // Load base configuration (appsettings.json drives Serilog + app options)
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Bootstrap Serilog early so startup failures are captured
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.WithProperty("App", "TeraCyteViewer")
                .CreateLogger();

            // Catch-all exception hooks (last line of defense)
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                Log.Fatal(ex.ExceptionObject as Exception, "AppDomain unhandled exception");

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                Log.Error(ex.Exception, "Unobserved task exception");
                ex.SetObserved(); // avoid process termination on finalizer thread
            };

            this.DispatcherUnhandledException += (s, ex) =>
            {
                Log.Error(ex.Exception, "Dispatcher unhandled exception");
                ex.Handled = true; // keep the app running, UI will remain responsive
            };

            // Generic Host for DI, config, logging – keeps things testable and modular
            HostApp = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(basePath);
                    c.AddConfiguration(config); // reuse already-built config so Serilog aligns
                })
                .UseSerilog() // plug Serilog into Microsoft.Extensions.Logging
                .ConfigureServices((ctx, services) =>
                {
                    // HttpClient configured with API base URL from config
                    var baseUrl = ctx.Configuration["TeraCyte:BaseUrl"]!;
                    services.AddHttpClient("TeraCyte", c => c.BaseAddress = new Uri(baseUrl));

                    // Services (singletons because they hold app-wide state and caches)
                    services.AddSingleton<Services.AuthService>();
                    services.AddSingleton<Services.ApiClient>();
                    services.AddSingleton<Services.PollingService>();

                    // ViewModels (singletons – one shell / one live dashboard per app)
                    services.AddSingleton<ViewModels.MainViewModel>();
                    services.AddSingleton<ViewModels.LoginViewModel>();
                    services.AddSingleton<ViewModels.LiveViewModel>();

                    // Views
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // Bring up the shell
            HostApp.Start();
            HostApp.Services.GetRequiredService<MainWindow>().Show();

            Log.Information("Application started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Flush logs and shut down host gracefully
            Log.Information("Application exiting");
            HostApp?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}

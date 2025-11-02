using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
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

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.WithProperty("App", "TeraCyteViewer")
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                Log.Fatal(ex.ExceptionObject as Exception, "AppDomain unhandled exception");

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                Log.Error(ex.Exception, "Unobserved task exception");
                ex.SetObserved();
            };

            this.DispatcherUnhandledException += (s, ex) =>
            {
                Log.Error(ex.Exception, "Dispatcher unhandled exception");
                ex.Handled = true;
            };

            HostApp = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(basePath);
                    c.AddConfiguration(config);
                })
                .UseSerilog() 
                .ConfigureServices((ctx, services) =>
                {
                    var baseUrl = ctx.Configuration["TeraCyte:BaseUrl"]!;
                    services.AddHttpClient("TeraCyte", c => c.BaseAddress = new Uri(baseUrl));
                    // Services
                    services.AddSingleton<Services.AuthService>();
                    services.AddSingleton<Services.ApiClient>();
                    services.AddSingleton<Services.PollingService>();
                    

                    // ViewModels
                    services.AddSingleton<ViewModels.MainViewModel>();
                    services.AddSingleton<ViewModels.LoginViewModel>();
                    services.AddSingleton<ViewModels.LiveViewModel>();

                    // Views
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            HostApp.Start();
            HostApp.Services.GetRequiredService<MainWindow>().Show();

            Log.Information("Application started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting");
            HostApp?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}

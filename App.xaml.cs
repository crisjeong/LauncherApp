using LauncherApp.Services;
using LauncherApp.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static IConfiguration Configuration { get; private set; }


        private IHost _host;


        public App()
        {
            ConfigureAppSettings();
            ConfigureLogging();

            _host = new HostBuilder()                    
                    .ConfigureServices((context, services) =>
                    {

                        // Serilog를 Microsoft.Extensions.Logging에 통합
                        //services.AddLogging(loggingBuilder =>
                        //{
                        //    loggingBuilder.ClearProviders(); // 기본 로그 제공자를 제거
                        //    loggingBuilder.AddSerilog();     // Serilog 추가
                        //});

                        // IConfiguration 등록
                        services.AddSingleton(Configuration);

                        // AppSettings 섹션을 객체로 바인딩하여 DI로 주입
                        services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));


                        // 서비스 등록 (여기에 서비스나 뷰모델, 리포지토리 등을 등록)            
                        services.AddSingleton<MainWindow>();
                        services.AddSingleton<IProgramManager, ProgramManager>();
                        services.AddSingleton<IWMIService, WMIService>();
                        services.AddHostedService<WMIWorker>();
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders(); // 기본 로그 제공자를 제거
                        logging.AddSerilog(); // Serilog 추가
                    })
                    .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            //ConfigureServices();
            await _host.StartAsync();

            //var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            //mainWindow?.Show();
            var mainWindow = _host.Services.GetService<MainWindow>();
            mainWindow?.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exited");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
        
        private void ConfigureAppSettings()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  // 현재 디렉터리를 기준으로 설정
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                //.MinimumLevel.Debug() // 로그의 최소 수준을 설정 (Debug, Information, Warning 등)
                //.WriteTo.Console()    // 콘솔에 로그 출력
                //.WriteTo.File("logs/log-.txt",
                //    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                //    rollingInterval: RollingInterval.Day,
                //    rollOnFileSizeLimit: true,
                //    retainedFileCountLimit: 30)
                .Destructure.ToMaximumCollectionCount(30)
                .CreateLogger();

            Log.Information("Application started");
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Serilog를 Microsoft.Extensions.Logging에 통합
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders(); // 기본 로그 제공자를 제거
                loggingBuilder.AddSerilog();     // Serilog 추가
            });

            // IConfiguration 등록
            services.AddSingleton(Configuration);

            // AppSettings 섹션을 객체로 바인딩하여 DI로 주입
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));


            // 서비스 등록 (여기에 서비스나 뷰모델, 리포지토리 등을 등록)            
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IProgramManager, ProgramManager>();
            services.AddSingleton<IWMIService, WMIService>();
            services.AddHostedService<WMIWorker>();

            // DI 컨테이너 구성
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}

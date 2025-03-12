using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using szakdolgozat.Components;
using szakdolgozat.Services;
using szakdolgozat.Stores;
using szakdolgozat.ViewModels;
using szakdolgozat.Views;
using DotNetEnv;

namespace szakdolgozat
{
    public partial class App : Application
    {
        private string _connectionString;

        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            Env.TraversePath().Load(".env");

            _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextFactory<AssetDbContext>(options =>
            {
                var sqlConnection = new SqlConnection(_connectionString);
                sqlConnection.AccessToken = AuthenticationService.Instance.SqlAccessToken;
                options.UseSqlServer(sqlConnection);
            });

            services.AddSingleton<NavigationStore>();
            services.AddSingleton<INavigationService>(x => CreateLoginNavigationService(x));

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<AssetListViewModel>();
            services.AddSingleton<AssetAssignmentListViewModel>();
            services.AddSingleton<AssetLogListViewModel>();
            services.AddSingleton<AssetFilterViewModel>();
            services.AddSingleton<AssetAssignmentFilterViewModel>();
            services.AddSingleton<AssetLogFilterViewModel>();
            services.AddTransient<ManageUsersViewModel>();
            services.AddSingleton<SubtypeListViewModel>();
            services.AddSingleton<AssetVulnerabilityViewModel>();
            services.AddSingleton<ChartExportService>();

            services.AddSingleton<MainWindow>(x => new MainWindow()
            {
                DataContext = x.GetRequiredService<MainWindowViewModel>(),
                WindowState = WindowState.Maximized
            });
            services.AddSingleton<LoginView>();
            services.AddSingleton<AssetListView>();
            services.AddSingleton<AssetAssignmentListView>();
            services.AddSingleton<AssetLogListView>();
            services.AddSingleton<AssetFilter>();
            services.AddSingleton<AssetAssignmentFilter>();
            services.AddSingleton<AssetLogFilter>();
            services.AddTransient<ManageUsersView>();
            services.AddSingleton<SubtypeListView>();
            services.AddSingleton<AssetVulnerabilityView>();
        }

        private INavigationService CreateLoginNavigationService(IServiceProvider serviceProvider)
        {
            return new NavigationService<LoginViewModel>(() => serviceProvider.GetRequiredService<LoginViewModel>());
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            INavigationService initialNavigationService = ServiceProvider.GetRequiredService<INavigationService>();
            if (AuthenticationService.Instance.TryAutoLoginAsync().Result)
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    bool canConnect = false;
                    while (!canConnect)
                    {
                        canConnect = scope.ServiceProvider.GetRequiredService<AssetDbContext>().Database.CanConnect();
                        if (!canConnect)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                initialNavigationService = new NavigationService<AssetListViewModel>(() => ServiceProvider.GetRequiredService<AssetListViewModel>());
            }
            initialNavigationService.Navigate();
            MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}

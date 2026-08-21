using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using TikTokGPMTool.Data;
using TikTokGPMTool.ViewModels;
using TikTokGPMTool.Services;

namespace TikTokGPMTool
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private IHost _host;

		public App()
		{
			_host = CreateHostBuilder().Build();
			_host.Start();
		}

		public static IHostBuilder CreateHostBuilder(string[]? args = null)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureServices((context, services) =>
				{
					var connectionString = $"Data Source={Path.GetFullPath("TeleDB.db")}";

					services.AddDbContext<TeleDataContext>(options =>
					{
						options.UseSqlite(connectionString);
						options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
					}, ServiceLifetime.Scoped, ServiceLifetime.Singleton);
					services.AddDbContextFactory<TeleDataContext>(options => options.UseSqlite(connectionString));

					// Đăng ký MainViewModel
					services.AddScoped<MainViewModel>();  // Thêm dòng này
					services.AddSingleton<IGpmProfileService, GpmProfileService>();
					services.AddSingleton<IActionHistoryService, ActionHistoryService>();
					services.AddSingleton<ITikTokAutomationService, TikTokAutomationService>();
					services.AddSingleton<ICampaignRunner, CampaignRunner>();
				});
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Lấy MainViewModel từ DI container
			var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
			using (var scope = _host.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<TeleDataContext>();
				db.Database.Migrate();
			}

			// Tạo đối tượng MainWindow và truyền IAccountDataService vào
			var mainWindow = new MainWindow();

			// Thiết lập DataContext cho MainWindow
			mainWindow.DataContext = mainViewModel;

			// Hiển thị cửa sổ chính
			mainWindow.Show();
		}
	}

}

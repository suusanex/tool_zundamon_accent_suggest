using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.FileIO;
using VoicevoxHelper.Infrastructure.LlmService;
using VoicevoxHelper.Infrastructure.Masking;
using VoicevoxHelper.Infrastructure.Services;
using VoicevoxHelper.Infrastructure.VoicevoxApi;
using VoicevoxHelper.App.Views;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private IHost? _host;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		try
		{
			_host = CreateHostBuilder().Build();
			_host.Start();

			var mainWindow = _host.Services.GetRequiredService<MainWindow>();
			MainWindow = mainWindow;
			mainWindow.Show();

			var navigation = _host.Services.GetRequiredService<INavigationService>();
			navigation.Navigate<ModeSelectionPage>();
		}
		catch (Exception ex)
		{
			Log.Fatal("Host startup failed: {Exception}", ex.ToString());
			Log.CloseAndFlush();
			throw;
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		try
		{
			_host?.Dispose();
		}
		catch (Exception ex)
		{
			Log.Error("Host dispose failed: {Exception}", ex.ToString());
		}
		finally
		{
			Log.CloseAndFlush();
		}

		base.OnExit(e);
	}

	private static IHostBuilder CreateHostBuilder()
	{
		return Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((_, config) =>
			{
				config.SetBasePath(AppContext.BaseDirectory);
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
			})
			.UseSerilog((context, services, loggerConfiguration) =>
			{
				loggerConfiguration
					.MinimumLevel.Information()
					.WriteTo.Console()
					.WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day);
			})
			.ConfigureServices((context, services) =>
			{
				services.Configure<ApplicationSettings>(context.Configuration);
				services.Configure<LlmSettings>(context.Configuration.GetSection("Llm"));
				services.Configure<VoicevoxSettings>(context.Configuration.GetSection("Voicevox"));
				services.Configure<DictionarySettings>(context.Configuration.GetSection("Dictionary"));

				services.AddSingleton<INavigationService, NavigationService>();
				services.AddSingleton<IClipboardService, ClipboardService>();
				services.AddSingleton<WorkflowState>();
				services.AddSingleton<IMaskingService, PersonalInfoMaskingService>();
				services.AddSingleton<ICsvParser, CsvDictionaryParser>();
				services.AddSingleton<IJsonParser, JsonDictionaryParser>();
				services.AddSingleton<IChatClient, OpenAiChatClient>();
				services.AddSingleton<ILlmService, AzureOpenAIService>();
				services.AddSingleton<IDictionaryRegistrationService, DictionaryRegistrationService>();

				services.AddHttpClient<IVoicevoxApiClient, VoicevoxApiClient>(client =>
				{
					var voicevox = context.Configuration.GetSection("Voicevox").Get<VoicevoxSettings>() ?? new VoicevoxSettings();
					client.Timeout = TimeSpan.FromSeconds(voicevox.TimeoutSeconds);
				});

				services.AddSingleton<ModeSelectionViewModel>();
				services.AddSingleton<DictionaryExtractionInputViewModel>();
				services.AddSingleton<DictionaryExtractionPreviewViewModel>();
				services.AddSingleton<DictionaryExtractionOutputViewModel>();
				services.AddSingleton<DictionaryRegistrationFileSelectionViewModel>();
				services.AddSingleton<DictionaryRegistrationValidationViewModel>();
				services.AddSingleton<DictionaryRegistrationExecutionViewModel>();
				services.AddSingleton<ScriptRewriteInputViewModel>();
				services.AddSingleton<ScriptRewriteResultViewModel>();

				services.AddSingleton<ModeSelectionPage>();
				services.AddSingleton<DictionaryExtractionInputPage>();
				services.AddSingleton<DictionaryExtractionPreviewPage>();
				services.AddSingleton<DictionaryExtractionOutputPage>();
				services.AddSingleton<DictionaryRegistrationFileSelectionPage>();
				services.AddSingleton<DictionaryRegistrationValidationPage>();
				services.AddSingleton<DictionaryRegistrationExecutionPage>();
				services.AddSingleton<ScriptRewriteInputPage>();
				services.AddSingleton<ScriptRewriteResultPage>();

				services.AddSingleton<MainWindow>();
			});
	}
}


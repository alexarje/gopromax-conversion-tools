using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Services;
using VideoConversionApp.ViewModels;

namespace VideoConversionApp;

public partial class App : Application
{
    private static string _configFilePath = null!;

    public static string ConfigFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_configFilePath))
                _configFilePath = Path.Combine(
                    Path.GetDirectoryName(typeof(App).Assembly.Location)!,
                    "config.json");
            return _configFilePath;
        }
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    
    
    public override void OnFrameworkInitializationCompleted()
    {
        // If you use CommunityToolkit, line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        
        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();
        
        // Load settings at the beginning.
        var configManager = services.GetRequiredService<IConfigManager>();
        if(!configManager.LoadConfigurations(ConfigFilePath))
            configManager.SaveConfigurations(ConfigFilePath);
        
        var vm = services.GetRequiredService<MainWindowViewModel>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = vm
            };
            ((StorageServiceProvider)services.GetRequiredService<IStorageServiceProvider>())
                .UseProviderWindow(desktop.MainWindow);

            desktop.MainWindow.Closing += (sender, args) =>
            {
                configManager.SaveConfigurations(ConfigFilePath);
            };
        }
        
        base.OnFrameworkInitializationCompleted();
    }

}
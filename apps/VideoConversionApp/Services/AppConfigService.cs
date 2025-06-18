using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VideoConversionApp.Services;

public class AppConfigService : IAppConfigService
{
    private AppConfig? _appConfiguration;

    [field: AllowNull, MaybeNull]
    private string ConfigFilePath
    {
        get
        {
            if (field == null)
            {
                var executableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configFile = Path.Combine(executableDir!, "config.yaml");
                field = configFile;
            }
            return field;
        }
    }

    public void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                var yamlConfig = new AppConfigYamlModel();
                _appConfiguration = new AppConfig(yamlConfig);
                SaveConfig();
                return;
            }

            var configYaml = File.ReadAllText(ConfigFilePath);

            var yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeMapping<IConfigPaths, ConfigPathsYamlModel>()
                .WithTypeMapping<IConfigConversion, ConfigConversionYamlModel>()
                .WithTypeMapping<IConfigPreviews, ConfigPreviewsYamlModel>()
                .Build();

            var yamlModel = yamlDeserializer.Deserialize<AppConfigYamlModel>(configYaml);
            _appConfiguration = new AppConfig(yamlModel);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to load configuration from config.yaml", ex);
        }
    }

    public void SaveConfig()
    {
        try
        {
            var yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = yamlSerializer.Serialize(_appConfiguration!.GetYamlModel());
            File.WriteAllText(ConfigFilePath, yaml);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to save configuration to config.yaml", ex);
        }
    }
    
    public AppConfig GetConfig()
    {
        if (_appConfiguration == null)
            throw new Exception("No configuration loaded");
        
        return _appConfiguration;
    }
}
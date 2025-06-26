namespace VideoConversionApp.Abstractions;

public interface IConfigManager
{
    bool LoadConfigurations(string filename);
    void SaveConfigurations(string filename);
    T? GetConfig<T>() where T: ISerializableConfiguration;
}

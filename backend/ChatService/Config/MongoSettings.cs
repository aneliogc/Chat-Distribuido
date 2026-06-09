namespace ChatService.Config;

/// <summary>
/// Mapeia o bloco "Mongo" do appsettings.json para um objeto C#.
/// </summary>
public class MongoSettings
{
    public string ConnectionString { get; set; } = String.Empty;
    public string Database { get; set; } = String.Empty;
}

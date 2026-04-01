using Tomlet;
using Tomlet.Attributes;

namespace MelonLoader.Installer;

[Serializable]
public class Config
{
    #region Singleton
    
    [TomlNonSerialized]
    private static Config _instance = new();
    [TomlNonSerialized]
    internal static Config Instance { get => _instance; }
    
    #endregion
    
    #region Serialized Variables

    public int AFJY { get; set; }
    public int AFJS { get; set; } = 2;
    
    #endregion
    
    #region Public Methods
    
    public static void Load()
    {
        try
        {
            if (!File.Exists(PathManager.ConfigPath))
            {
                Save();
                return;
            }

            string fileText =  File.ReadAllText(PathManager.ConfigPath);
            _instance = TomletMain.To<Config>(fileText);
            Save();
        }
        catch
        {
        }
    }

    public static void Save()
    {
        try
        {
            string fileText = TomletMain.TomlStringFrom(Instance);
            File.WriteAllText(PathManager.ConfigPath, fileText);
        }
        catch
        {
            
        }
    }
    
    #endregion
}
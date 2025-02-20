using System.Reflection;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;

namespace SimpleGlamourSwitcher.Configuration;

public abstract class ConfigFile {
    public uint Version { get; protected set; }
    
    [JsonIgnore]
    public Guid Guid { get; protected set; }
    
    protected virtual void Setup() {
        Version = 1;
    }
}




public abstract class ConfigFile<T, TParent> : ConfigFile where T : ConfigFile<T, TParent>, new() where TParent : ConfigFile, IParentConfig<TParent> {
    [JsonIgnore]
    private bool dirty;

    [JsonIgnore]
    public bool Dirty {
        get => dirty;
        set => dirty = dirty || value;
    }

    
    private static readonly T Default = new();

    [JsonIgnore] private TParent? parent;
    
    public static void VerifyParent(ref TParent? parent) {
        if (typeof(TParent) == typeof(RootConfig)) parent = RootConfig.Instance as TParent;
    }
    protected TParent? GetParent() {
        VerifyParent(ref parent);
        
        return parent;
    }
    
    public static T Create(TParent? parent = null) {
        VerifyParent(ref parent);
        var instance = new T();
        instance.Initialize(parent, Guid.NewGuid());
        return instance;
    }
    
    
    public static T? Load(Guid guid, TParent? parent = null) {
        VerifyParent(ref parent);
        
        PluginLog.Debug($"Load {typeof(T).Name} - {guid} - File: {GetConfigPath(parent, guid)}");

        var configFile = GetConfigPath(parent, guid);
        if (!configFile.FullName.StartsWith(PluginInterface.GetPluginConfigDirectory())) throw new FileLoadException("Config file cannot load from outside the plugin config directory. Something went wrong");
        
        
        if (!configFile.Exists) return null;

        var json = File.ReadAllText(configFile.FullName);
        var instance = JsonConvert.DeserializeObject<T>(json);
        if (instance == null) return null;
        instance.Initialize(parent, guid);
        PluginLog.Debug($"Loaded {typeof(T).Name} - {guid}");

        return instance;
        
    }
    
    


    public void Initialize(TParent? parent, Guid guid) {
        VerifyParent(ref parent);
        Setup();

        this.parent = parent;
        Guid = guid;
    }
    
    public void Save(bool force = false) {
        if (force) Dirty = true;
        PluginLog.Debug($"Save Request {typeof(T).Name} - {GetConfigPath(parent, Guid)}");
        if (!Dirty) return;

        dirty = false;
        
        PluginLog.Debug($"Save {typeof(T).Name} - {GetConfigPath(parent, Guid)}");

        var file = GetConfigPath(parent, Guid);
        if (file.Directory == null) throw new Exception("Invalid File");
        if (!file.FullName.StartsWith(PluginInterface.GetPluginConfigDirectory())) throw new Exception("ConfigFile cannot save outside the plugin config directory. Something went wrong");
        try {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            if (!file.Directory.Exists) file.Directory.Create();
            File.WriteAllText(file.FullName, json);
        } catch (Exception ex) {
            PluginLog.Error(ex, $"Error saving {typeof(T).Name} - {Guid}");
        }
    }
    
    public static FileInfo GetConfigPath(TParent? parent, Guid guid) {
        VerifyParent(ref parent);

        var fileName = $"{guid}.json";
        if (typeof(T).IsAssignableTo(typeof(INamedConfigFile))) {
            fileName = typeof(T).GetMethod("GetFileName", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, [guid]) as string;
        }
        
        if (fileName == null) throw new Exception("Filename cannot be null.");
        
        
        var filePath = Path.Join(TParent.GetChildDirectory(parent).FullName, fileName);
        
        PluginLog.Debug($"File Path for [{typeof(T).Name}]{guid} - {filePath}");
        
        
        return new FileInfo(filePath);
    }
}
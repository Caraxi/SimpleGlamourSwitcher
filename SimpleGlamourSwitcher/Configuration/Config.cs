global using static SimpleGlamourSwitcher.Configuration.Config.I;
using SimpleGlamourSwitcher.Configuration.Files;

namespace SimpleGlamourSwitcher.Configuration;

public static class Config {

   public static class I {
      public static PluginConfigFile PluginConfig => _pluginConfig;
      public static CharacterConfigFile? ActiveCharacter => _activeCharacter;

      public static CharacterConfigFile? SharedCharacter => _sharedCharacter;

   }
   
   static Config() {
      _pluginConfig = ReloadPluginConfig();
   }

   public static void Shutdown() {
      _pluginConfig.Save();
      _activeCharacter?.Save();
   }
   
   private static PluginConfigFile _pluginConfig;
   private static CharacterConfigFile? _activeCharacter;
   private static CharacterConfigFile? _sharedCharacter;

   public static PluginConfigFile ReloadPluginConfig() {
      _pluginConfig = PluginConfigFile.Load(Guid.Empty) ?? PluginConfigFile.Create();
      LoadSharedCharacter();
      return _pluginConfig;
   }
   
   
   public static bool SwitchCharacter(Guid? guid, bool setSaved = true, bool resetState = false) {
      _activeCharacter?.Save();
      _activeCharacter = null;
      
      if (guid == null || guid == Guid.Empty) {
         if (setSaved) {
            _pluginConfig.SelectedCharacter.Remove(PlayerStateService.ContentId);
            _pluginConfig.Dirty = true;
            _pluginConfig.Save();
         }
      } else {
         if (setSaved) {
            _pluginConfig.SelectedCharacter[PlayerStateService.ContentId] = guid.Value;
            _pluginConfig.Dirty = true;
            _pluginConfig.Save();
         }
         _activeCharacter = CharacterConfigFile.Load(guid.Value, _pluginConfig);
      }
      
      return _activeCharacter != null;
   }

   public static void LoadSharedCharacter() {
      _sharedCharacter = CharacterConfigFile.Load(CharacterConfigFile.SharedDataGuid, _pluginConfig) ?? CharacterConfigFile.Create(CharacterConfigFile.SharedDataGuid, _pluginConfig);
      _sharedCharacter.Name = "Shared";
      _sharedCharacter.Save(true);
   }
}

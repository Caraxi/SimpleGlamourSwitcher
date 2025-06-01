namespace SimpleGlamourSwitcher.Configuration.Parts;

public class AutomationConfig {
    public Guid? Login = null;
    public Guid? CharacterSwitch = null;
    public Guid? DefaultGearset = null;
    public Dictionary<byte, Guid?> Gearsets = new();
}

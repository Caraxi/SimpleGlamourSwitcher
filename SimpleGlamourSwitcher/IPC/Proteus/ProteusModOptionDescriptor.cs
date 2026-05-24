using System.ComponentModel;
using Newtonsoft.Json;

namespace SimpleGlamourSwitcher.IPC.Proteus;

public record ProteusOptionDescriptor(string? GroupName, string? OptionName) {
    public ProteusModOptionDescriptor WithMod(string modDirectory) => new(modDirectory, GroupName, OptionName);
}

public record ProteusModOptionDescriptor(string ModDirectory, string? GroupName, string? OptionName) : ProteusOptionDescriptor(GroupName, OptionName) {
    [JsonIgnore] public bool IsOptionless => GroupName == null || OptionName == null;
    [JsonIgnore] public ProteusColorTable? ColorTable => ProteusIpc.GetColourTable(this);



    public static List<ProteusModOptionDescriptor>? ListFromIpc(ProteusIpcOverlayDetail ipcOverlayDetail) {
        if (ipcOverlayDetail.Options == null) return null;
        var l = new List<ProteusModOptionDescriptor>();

        foreach (var (group, optionList) in ipcOverlayDetail.Options) {
            l.AddRange(optionList.Select(option => new ProteusModOptionDescriptor(ipcOverlayDetail.ModDirectory, group, option)));
        }

        return l;
    }

    public virtual bool Equals(ProteusModOptionDescriptor? other) =>
        other != null && IsOptionless == other.IsOptionless &&
        string.Equals(ModDirectory, other.ModDirectory, StringComparison.InvariantCultureIgnoreCase) &&
        (
            IsOptionless || string.Equals(GroupName, other.GroupName, StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(OptionName, other.OptionName, StringComparison.InvariantCultureIgnoreCase)
        );

    public override int GetHashCode() => HashCode.Combine(ModDirectory, GroupName, OptionName);

    public override string ToString() => IsOptionless ? ModDirectory : $"{ModDirectory}:{GroupName}/{OptionName}";


}

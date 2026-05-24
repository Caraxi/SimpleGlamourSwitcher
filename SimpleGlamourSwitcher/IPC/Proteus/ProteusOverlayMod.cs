namespace SimpleGlamourSwitcher.IPC.Proteus;

public record ProteusOverlayMod(string ModDirectory, string Name, int Priority, List<ProteusModOptionDescriptor>? Options) {
    public static implicit operator ProteusOverlayMod(ProteusIpcOverlayDetail a) => new(a.ModDirectory, a.Name, a.Priority, ProteusModOptionDescriptor.ListFromIpc(a));

    public static implicit operator ProteusModOptionDescriptor(ProteusOverlayMod mod) => new(mod.ModDirectory, null, null);


    public ProteusColorTable? ColorTable => ProteusIpc.GetColourTable(this);
}

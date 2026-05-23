using Newtonsoft.Json;

namespace SimpleGlamourSwitcher.IPC.Proteus;

public class ProteusColorTable(List<ProteusColorRow> rows) {
    public List<ProteusColorRow> Rows { get; set; } = rows;
    public ProteusColorTable() : this([]) { }
    public ProteusColorTable(string json) : this(ProteusColorRow.ListFromJson(json)) { }
}

using Newtonsoft.Json;

namespace SimpleGlamourSwitcher.IPC.Proteus;

public record ProteusColorRow(int Row, ProteusColorSubRow? SubRowA, ProteusColorSubRow? SubRowB) {
    public static List<ProteusColorRow> ListFromJson(string json) {
        try {
            var colors = JsonConvert.DeserializeObject<List<ProteusColorRow>>(json, new ProteusHexConverter());
            return colors ?? [];
        } catch {
            return [];
        }
    }
}

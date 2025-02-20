using Newtonsoft.Json.Linq;

namespace SimpleGlamourSwitcher.IPC.Glamourer;


public class GlamourerState {
    public GlamourerEquipment Equipment = new();
    public GlamourerBonuses Bonus = new();
    public GlamourerCustomize Customize = new();
    public GlamourerParameters Parameters = new();
    public Dictionary<MaterialValueIndex, GlamourerMaterial> Materials = new();

    public static implicit operator GlamourerState?(JObject? jObject) {
        return jObject == null ? new GlamourerState() : jObject.ToObject<GlamourerState>();
    }
}

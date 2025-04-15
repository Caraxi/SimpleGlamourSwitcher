namespace SimpleGlamourSwitcher.IPC.Glamourer;

public class GlamourerParameterFloat : GlamourerToggleable {
    public float? Value;
}

public class GlamourerParameterPercentage : GlamourerToggleable {
    public float? Percentage;
}

public class GlamourerParameterColor : GlamourerToggleable {
    public float? Red;
    public float? Green;
    public float? Blue;
    public float? Alpha;
}

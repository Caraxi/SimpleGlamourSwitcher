namespace SimpleGlamourSwitcher.Configuration.Parts;

public abstract record Applicable<T> : Applicable {
    public abstract void ApplyToCharacter(T slot, ref bool requestRedraw);

    public sealed override void ApplyToCharacter(ref bool requestRedraw) {
        throw new Exception("Not implemented for Applicable<T>");
    }
}

public abstract record Applicable<T, T2> : Applicable {
    public abstract void ApplyToCharacter(T t, T2 t2, ref bool requestRedraw);

    public sealed override void ApplyToCharacter(ref bool requestRedraw) {
        throw new Exception("Not implemented for Applicable<T, T2>");
    }
}


public abstract record Applicable {
    public abstract void ApplyToCharacter(ref bool requestRedraw);
    public bool Apply;
}

namespace OpenPetsWorld;

public enum Stage
{
    Infancy,
    Growth,
    Adult,
    Full,
    Extreme
}

public static class StageConverter
{
    public static string ToStr(this Stage stage) => stage switch
    {
        Stage.Infancy => "幼年期",
        Stage.Growth => "成长期",
        Stage.Adult => "成年期",
        Stage.Full => "完全体",
        Stage.Extreme => "究极体",
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
}
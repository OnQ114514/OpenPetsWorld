namespace OpenPetsWorld;

public enum Stage
{
    Infancy,
    Growth,
    Adulthood,
    Full,
    Extreme
}

public static class Converter
{
    public static string ToStr(this Stage stage)
    {
        return stage switch
        {
            Stage.Infancy => "幼年期",
            Stage.Growth => "成长期",
            Stage.Adulthood => "成年期",
            Stage.Full => "完全体",
            Stage.Extreme => "究极体",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
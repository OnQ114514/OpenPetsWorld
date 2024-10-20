namespace OpenPetsWorld.PetTool;

public enum State
{
    Poisoned,
}

public static class StateConverter
{
    public static string ToStr(this List<State> states)
    {
        foreach (var state in states)
        {
            return state switch
            {
                State.Poisoned => "中毒",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return "正常";
    }
}
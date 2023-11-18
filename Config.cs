namespace OpenPetsWorld;

public class Config
{
    public string Address = "";
    public string VerifyKey = "";
    public string QNumber = "";
    public List<string> GroupList = new();
    public HashSet<string> NotRunningGroup = new();
    public bool BlackListMode = false;
    public string MasterId = "";
    public List<string> Admins = new();
    public string BootText = "TAKE OFF TOWARD THE DREAM";
}
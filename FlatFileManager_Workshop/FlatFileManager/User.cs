namespace FlatFileManager;

public class User
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public bool IsActive { get; set; }
    public int Attempts { get; set; } = 0;
}
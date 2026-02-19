namespace AccountManagement.Options;

public class CookieOptionsConfig {
    public string Domain { get; set; } = string.Empty;
    public bool Secure { get; set; } = true;
    public bool HttpOnly { get; set; } = true;
    public string SameSite { get; set; } = "None";
    public string Path { get; set; } = "/";
}
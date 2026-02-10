namespace AccountManagement.Options;

public class DbConnectionOptions {
    public string HostName { get; set; }
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string ConnectionString =>
        $"Host={HostName};Port={Port};Database={Database ?? DbName};Username={Username};Password={Password};";
}
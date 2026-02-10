namespace AccountManagement.Options;

public class DbConnectionOptions {
    public string HostName { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string DbName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public string ConnectionString =>
        $"Host={HostName};Port={Port};Database={Database ?? DbName};Username={Username};Password={Password};";
}
namespace AccountManagement.Options;

public class RabbitMqOptions {
    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
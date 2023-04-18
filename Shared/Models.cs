namespace Shared;

public class Config
{
    public bool IsProductionEnvironment { get; set; }
    public required Ftp FtpKunde { get; set; }
    public required Ftp FtpSix { get; set; }
    public required string Invoices { get; set; }
}

public class Ftp
{
    public required string Host { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Out { get; set; }
    public required string In { get; set; }
}
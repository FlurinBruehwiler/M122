using System.IO.Compression;
using System.Net;
using System.Text.Json;
using FluentFTP;
using FluentFTP.Helpers;
using MailKit.Net.Smtp;
using MimeKit;

var configTxt = File.ReadAllText("appsettings.json");
var config = JsonSerializer.Deserialize<Config>(configTxt);

if (config is null)
    return;

using var ftpKundensystem = new FtpClient
{
    Host = config.FtpKunde.Host,
    Credentials = new NetworkCredential(config.FtpKunde.Username, config.FtpKunde.Password)
};

ftpKundensystem.Connect();

Console.WriteLine("Connected to Kundensystem FTP");

using var ftpSix = new FtpClient
{
    Host = config.FtpSix.Host,
    Credentials = new NetworkCredential(config.FtpSix.Username, config.FtpSix.Password)
};

ftpSix.Connect();

Console.WriteLine("Connected to Six FTP");

var quittungsfiles = ftpSix.GetListing(config.FtpSix.Out).Where(x => x.Name.StartsWith("quittungsfile")).ToList();

if (quittungsfiles.Count == 0)
{
    Console.WriteLine("Keine Quittungen gefunden");
    return;
}

Console.WriteLine($"{quittungsfiles.Count} Quittungen gefunden!!!");

foreach (var quittungsfile in quittungsfiles)
{
    try
    {
        Console.WriteLine("---------------------------------------");
        Console.WriteLine($"Gestarted mit der Verarbeitung von {quittungsfile.Name}");
    
        var tempPath = Path.Combine(config.Invoices, "temp");
        var tempFile = Path.Combine(tempPath, quittungsfile.Name);
        ftpSix.DownloadFile(tempFile, quittungsfile.FullName);

        var quittungsText = File.ReadAllLines(tempFile);
        Console.WriteLine("Quittung erfolgreich heruntergeladen");

        var invoiceName = quittungsText.First().Split(" ").First(x => x.Contains("_invoice.xml")).Split("_")[1];
    
        var timestamp = quittungsfile.Name.RemovePrefix("quittungsfile").RemovePostfix(".txt");
    
        var invoiceFolder = Path.Combine(config.Invoices, invoiceName);
        var folderToZip = Path.Combine(invoiceFolder, "FolderToZip");
    
        Directory.CreateDirectory(folderToZip);
    
        var targetQuittungsFile = Path.Combine(folderToZip, quittungsfile.Name);
        if (File.Exists(targetQuittungsFile))
        {
            File.Delete(tempFile);
        }
        else
        {
            File.Move(tempFile, Path.Combine(folderToZip, quittungsfile.Name));
        }
    
        ftpSix.DownloadDirectory(folderToZip, $"{config.FtpSix.In}/archive/{timestamp}");
        File.Delete(Path.Combine(folderToZip, "index.php"));
    
        var zipFile = Path.Combine(invoiceFolder, invoiceName + ".zip");
    
        if (!File.Exists(zipFile))
        {
            ZipFile.CreateFromDirectory(folderToZip, zipFile);
            Console.WriteLine("Zip Date wurde erstellt");
        }
    
        var inputDataText = File.ReadAllLines(Path.Combine(invoiceFolder, "input.data"));
        var email = inputDataText[1].Split(";").Last();
    
        const string myKoohlEmail = "bruhwiler.flurin@gmail.com";
    
        var message = new MimeMessage
        {
            From = { new MailboxAddress(myKoohlEmail, myKoohlEmail) },
            To = { new MailboxAddress(email, email) },
            Subject = "Handwerkerrechnung Quittung"
        };
    
        var builder = new BodyBuilder();
        builder.Attachments.Add(zipFile);
        message.Body = builder.ToMessageBody();
    
        if (config.IsProductionEnvironment)
        {
            using var client = new SmtpClient();
            client.Connect("smtp.gmail.com", 465, true);
            client.Authenticate(myKoohlEmail, "jnzbhaymanqwsqxy");
            client.Send(message);
            client.Disconnect(true);
            
            Console.WriteLine("Mail wurde erfolgreich versendet");
        }
    
        ftpKundensystem.UploadFile(zipFile, $"{config.FtpKunde.In}/{invoiceName}.zip");
    }
    catch(Exception e)
    {
        Console.WriteLine(
            "Es ist ein Fehler aufgetreten beim auslesen der Rechnungsdatei. Vermutlich ist die Quittung nicht korrekt");
        Console.WriteLine(e);
    }
}
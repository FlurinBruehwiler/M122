using System.IO.Compression;
using System.Net;
using System.Text.Json;
using FluentFTP;
using FluentFTP.Helpers;
using MailKit.Net.Smtp;
using MimeKit;
using Shared;

Logger.Info("-----------------------------------");

var configTxt = File.ReadAllText(@"C:\Users\FBR\RiderProjects\Handwerkerrechnungen\Handwerkerrechnung2\bin\Debug\net7.0\appsettings.json");
var config = JsonSerializer.Deserialize<Config>(configTxt);

if (config is null)
    return;

using var ftpKundensystem = new FtpClient
{
    Host = config.FtpKunde.Host,
    Credentials = new NetworkCredential(config.FtpKunde.Username, config.FtpKunde.Password)
};

ftpKundensystem.Connect();

Logger.Info("Connected to Kundensystem FTP");

using var ftpSix = new FtpClient
{
    Host = config.FtpSix.Host,
    Credentials = new NetworkCredential(config.FtpSix.Username, config.FtpSix.Password)
};

ftpSix.Connect();

Logger.Info("Connected to Six FTP");

var quittungsfiles = ftpSix.GetListing(config.FtpSix.Out).Where(x => x.Name.StartsWith("quittungsfile")).ToList();

if (quittungsfiles.Count == 0)
{
    Logger.Info("Keine Quittungen gefunden");
    return;
}

Logger.Info($"{quittungsfiles.Count} Quittungen gefunden!!!");

foreach (var quittungsfile in quittungsfiles)
{
    try
    {
        Logger.Info("---------------------------------------");
        Logger.Info($"Gestarted mit der Verarbeitung von {quittungsfile.Name}");
    
        var tempPath = Path.Combine(config.Invoices, "temp");
        var tempFile = Path.Combine(tempPath, quittungsfile.Name);
        ftpSix.DownloadFile(tempFile, quittungsfile.FullName);

        var quittungsText = File.ReadAllLines(tempFile);
        Logger.Info("Quittung erfolgreich heruntergeladen");

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
            Logger.Info("Zip Date wurde erstellt");
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
        builder.TextBody = "Viel Glück mit Ihrer Rechnung :)";
        message.Body = builder.ToMessageBody();
    
        if (config.IsProductionEnvironment)
        {
            using var client = new SmtpClient();
            client.Connect("smtp.gmail.com", 465, true);
            client.Authenticate(myKoohlEmail, "jnzbhaymanqwsqxy");
            client.Send(message);
            client.Disconnect(true);
            
            Logger.Info("Mail wurde erfolgreich versendet");
        }
    
        ftpKundensystem.UploadFile(zipFile, $"{config.FtpKunde.In}/{invoiceName}.zip");

        if (config.IsProductionEnvironment)
        {
            ftpSix.DeleteFile(quittungsfile.FullName);
            Logger.Info("Quittung wurde auf dem FTP gelöscht");            
        }
    }
    catch(Exception e)
    {
        Logger.Info(
            "Es ist ein Fehler aufgetreten beim auslesen der Rechnungsdatei. Vermutlich ist die Quittung nicht korrekt");
        Logger.Info(e.ToString());
        
        if (config.IsProductionEnvironment)
        {
            var errorDirectory = $"{config.FtpSix.Out}/Error";
            if (!ftpKundensystem.DirectoryExists(errorDirectory))
            {
                ftpKundensystem.CreateDirectory(errorDirectory);
            }

            var errorFile = Path.Combine(errorDirectory, quittungsfile.Name);
            ftpKundensystem.MoveFile(quittungsfile.FullName, errorFile);
            Logger.Info("Quittung wurde in de Error Ordner verschoben");
        }
    }
}
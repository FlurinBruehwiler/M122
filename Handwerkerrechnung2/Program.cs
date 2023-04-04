using System.IO.Compression;
using System.Net;
using FluentFTP;
using FluentFTP.Helpers;
using Handwerkerrechnung;
using MailKit.Net.Smtp;
using MimeKit;

AppDomain.CurrentDomain.UnhandledException += (_, _) =>
{
    Logger.Error("There was an error, maybe the file is not very good");
};

const string invoicesPath = @"C:\Users\FBR\RiderProjects\Handwerkerrechnungen\invoices";

using var ftpKundensystem = new FtpClient
{
    Host = "ftp.haraldmueller.ch",
    Credentials = new NetworkCredential("schoolerinvoices", "Berufsschule8005!")
};

ftpKundensystem.Connect();

using var ftpSix = new FtpClient
{
    Host = "ftp.coinditorei.com",
    Credentials = new NetworkCredential("zahlungssystem", "Berufsschule8005!")
};

ftpSix.Connect();

var quittungsfile = ftpSix.GetListing("out/AP20bBruehwiler").FirstOrDefault(x => x.Name.StartsWith("quittungsfile"));

if (quittungsfile is null)
    return;

var tempPath = Path.Combine(invoicesPath, "temp");
var tempFile = Path.Combine(tempPath, quittungsfile.Name);
ftpSix.DownloadFile(tempFile, quittungsfile.FullName);

var quittungsText = File.ReadAllLines(tempFile);
var invoiceName = quittungsText.First().Split(" ").First(x => x.Contains("_invoice.xml")).Split("_")[1];

var timestamp = quittungsfile.Name.RemovePrefix("quittungsfile").RemovePostfix(".txt");

var invoiceFolder = Path.Combine(invoicesPath, invoiceName);
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

ftpSix.DownloadDirectory(folderToZip,$"/in/AP20bBruehwiler/archive/{timestamp}");
File.Delete(Path.Combine(folderToZip, "index.php"));

var zipFile = Path.Combine(invoiceFolder, invoiceName + ".zip");

if (!File.Exists(zipFile))
{
    ZipFile.CreateFromDirectory(folderToZip, zipFile);
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

using var client = new SmtpClient();
client.Connect("smtp.gmail.com", 465, true);
client.Authenticate(myKoohlEmail, "jnzbhaymanqwsqxy");
client.Send(message);
client.Disconnect(true);

ftpKundensystem.UploadFile(zipFile, $"in/AP20b/Bruehwiler/{invoiceName}.zip");
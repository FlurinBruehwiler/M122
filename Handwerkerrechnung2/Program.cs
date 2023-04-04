using System.IO.Compression;
using System.Net;
using FluentFTP;
using FluentFTP.Helpers;
using MailKit.Net.Smtp;
using MimeKit;

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
var invoiceName = quittungsText.First().Split(" ").First(x => x.Contains("_invoice.xml")).RemovePostfix("_invoice.xml");

var timestamp = quittungsfile.Name.RemovePrefix("quittungsfile");

var invoiceFolder = Path.Combine(invoicesPath, invoiceName);
var folderToZip = Path.Combine(invoiceFolder, "FolderToZip");

File.Move(tempFile, Path.Combine(folderToZip, quittungsfile.Name));

ftpSix.DownloadDirectory(folderToZip,$"/in/AP20bBruehwiler/archive/{timestamp}");
File.Delete(Path.Combine(folderToZip, "index.php"));

var zipFile = Path.Combine(invoiceFolder, invoiceName + ".zip");

ZipFile.CreateFromDirectory(folderToZip, zipFile);

var inputDataText = File.ReadAllLines(Path.Combine(invoiceFolder, "input.data"));
var email = inputDataText[1].Split(";").Last();

var message = new MimeMessage
{
    From = { new MailboxAddress("Flurin", "bruhwiler.flurin@gmail.com") },
    To = { new MailboxAddress("Geschätzter aber nicht geliebter Kunde", email) },
    Subject = "Handwerkerrechnung Quittung"
};

var builder = new BodyBuilder();
builder.Attachments.Add(zipFile);
message.Body = builder.ToMessageBody();

using var client = new SmtpClient();
client.Connect("smtp.gmail.com", 465, true);
client.Authenticate("bruhwiler.flurin@gmail.com", "jnzbhaymanqwsqxy");
client.Send(message);
client.Disconnect(true);

ftpKundensystem.UploadFile(zipFile, $"int/AP20b/Bruehwiler/{invoiceName}.zip");

//invoices
    //temp
    //23003
        //input.data
        //invoice.xml
        //invoice.txt
        //folderToZip
            //quittungsfile
            //invoice.xml
            //invoice.txt
        //zipFile.zip
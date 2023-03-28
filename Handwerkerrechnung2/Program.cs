using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using FluentFTP;
using FluentFTP.Helpers;

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

var smtpClient = new SmtpClient("smtp.gmail.com")
{
    Port = 587,
    Credentials = new NetworkCredential("username", "password"),
    EnableSsl = true
};

var message = new MailMessage
{
    Attachments = { new Attachment(zipFile, MediaTypeNames.Application.Zip) },
    From = new MailAddress("bruhwiler.flurin@gmail.com"),
    To = { email },
    Subject = "Quittung"
};

smtpClient.Send(message);

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
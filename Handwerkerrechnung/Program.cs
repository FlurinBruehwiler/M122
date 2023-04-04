using System.Globalization;
using System.Net;
using FluentFTP;
using FluentFTP.Helpers;
using Handwerkerrechnung;

using var ftpKundensystem = new FtpClient
{
    Host = "ftp.haraldmueller.ch",
    Credentials = new NetworkCredential("schoolerinvoices", "Berufsschule8005!")
};

ftpKundensystem.Connect();

Logger.Info("Connected to Kundensystem FTP");

using var ftpSix = new FtpClient
{
    Host = "ftp.coinditorei.com",
    Credentials = new NetworkCredential("zahlungssystem", "Berufsschule8005!")
};

ftpSix.Connect();

Logger.Info("Connected to Six FTP");

var file = ftpKundensystem.GetListing("out/AP20b/Bruehwiler").FirstOrDefault();

if (file is null)
    return;

Logger.Info($"Neue Rechnung gefunden!!!: {file.Name}");

const string invoicesPath = @"C:\Users\FBR\RiderProjects\Handwerkerrechnungen\invoices";

var dataTempFile = Path.Combine(invoicesPath, "temp", $"{Guid.NewGuid()}.data");
ftpKundensystem.DownloadFile(dataTempFile, file.FullName);

var text = File.ReadAllText(dataTempFile);

Logger.Info("Rechnung erfolgreich heruntergeladen");

var fileContnet = text.Split("\n").Select(x => x.Split(";")).ToArray();

var header = fileContnet.First();
var herkunft = fileContnet.First(x => x.First() == "Herkunft");
var endkunde = fileContnet.First(x => x.First() == "Endkunde");
var content = fileContnet.Where(x => x.First() == "RechnPos").ToArray();

var rechnungsnummer = header[0].RemovePrefix("Rechnung_"); //23003
var auftragsnummer = header[1].RemovePrefix("Auftrag_"); //A003

var datum = header[3]; //21.07.2023

var herkunftId = herkunft[1];
var herkunftName = herkunft[3]; //Adam Adler
var herkunftAdresse1 = herkunft[4]; //Bahnhofstrasse 1
var herkunftAdresse2 = herkunft[5].Trim(); //8000 Zuerich

var kundennummer = herkunft[2]; //K821
var rechnungsname = herkunft[6]; //CHE-111.222.333 MWST

var endkundId = endkunde[1];
var endkundeName = endkunde[2]; //Autoleasing AG
var endkundeAdresse1 = endkunde[3]; //Gewerbestrasse 100
var endkundeAdresse2 = endkunde[4].Trim(); //5000 Aarau

Logger.Info("Parsing der Rechnung erfolgreich");

var myKoohlDirectory = Path.Combine(invoicesPath, rechnungsnummer);

Directory.CreateDirectory(myKoohlDirectory);

var finalDataFile = Path.Combine(myKoohlDirectory, "input.data");

File.Move(dataTempFile, finalDataFile);

Logger.Info("Beginne Generierung von txt Datei");

var stringItems = string.Empty;

var totalAmount = 0;

foreach (var row in content)
{
    var nr = row[1];
    var name = row[2];
    var count = row[3];
    var amount1 = row[4];
    var amount2 = row[5];
    // var mwst = row[6];

    stringItems += nr;
    stringItems += new string(' ', 4 - nr.Length);

    stringItems += name;
    stringItems += new string(' ', 38 - name.Length);
        
    stringItems += count;
    stringItems += new string(' ', 12 - count.Length - amount1.Length);

    stringItems += $"{amount1}  CHF";
    stringItems += new string(' ', 12 - amount2.Length);
    stringItems += amount2;

    stringItems += Environment.NewLine;
        
    totalAmount += int.Parse(amount2.Replace(".", ""));
}

stringItems = stringItems.Trim();

var formatedTotalAmount = (totalAmount / 100).ToString("0.00").Replace(',', '.');
var stringTotal = new string(' ', 12 - formatedTotalAmount.Length) + formatedTotalAmount;
var string2Total = formatedTotalAmount + new string(' ', 18 - formatedTotalAmount.Length);

var stringRechnungsnummer = new string(' ', 12 - rechnungsnummer.Length) + rechnungsnummer;

var startDate = DateTime.ParseExact(datum, "dd.MM.yyyy", CultureInfo.InvariantCulture);
var endDate = startDate.AddDays(30).ToString("dd.MM.yyyy");

var padding1 = herkunftName;
padding1 += new string(' ', 27 - herkunftName.Length);

var padding2 = herkunftAdresse1;
padding2 += new string(' ', 27 - herkunftAdresse1.Length);

var padding3 = herkunftAdresse2;
padding3 += new string(' ', 27 - herkunftAdresse2.Length);

var padding4 = endkundeName;
padding4 += new string(' ', 27 - endkundeName.Length);

var padding5 = endkundeAdresse1;
padding5 += new string(' ', 27 - endkundeAdresse1.Length);

var padding6 = endkundeAdresse2;
padding6 += new string(' ', 27 - endkundeAdresse2.Length);


var txt = @$"-------------------------------------------------



{herkunftName}
{herkunftAdresse1}
{herkunftAdresse2}

{rechnungsname}




Uster, den {datum}                                 {endkundeName}
                                                {endkundeAdresse1}
                                                {endkundeAdresse2}

Kundennummer:      {kundennummer}
Auftragsnummer:    {auftragsnummer}
	
Rechnung Nr{stringRechnungsnummer}
-----------------------
{stringItems}
                                                             -----------       
                                                  Total CHF{stringTotal}

                                                  MWST  CHF        0.00








Zahlungsziel ohne Abzug 30 Tage ({endDate})


Empfangsschein             Zahlteil                

{padding1}------------------------  {herkunftName}       
{padding2}|  QR-CODE             |  {herkunftAdresse1} 
{padding3}|                      |  {herkunftAdresse2}
                           |                      |  
                           |                      |  
00 00000 00000 00000 00000 |                      |  00 00000 00000 00000 00000
                           |                      |     
{padding4}|                      |  {endkundeName}
{padding5}|                      |  {endkundeAdresse1}
{padding6}|                      |  {endkundeAdresse2}
                           ------------------------
Währung  Betrag            Währung  Betrag
CHF      {string2Total}CHF      {formatedTotalAmount}								

-------------------------------------------------
";

//Generate Rechnung als TXT

var txtFileName = $"{kundennummer}_{rechnungsnummer}_invoice.txt";

File.WriteAllText(txtFileName, txt);

Logger.Info("Generierung von txt Datei abgeschlosseen");

//Generate Rechnung als XML

Logger.Info("Beginne generierung von xml Datei");


var xmlFileName = $"{kundennummer}_{rechnungsnummer}_invoice.xml";

var xml = $"""
<XML-FSCM-INVOICE-2003A>
    <INTERCHANGE>
        <IC-SENDER>
            <Pid>{herkunftId}</Pid>
        </IC-SENDER>
        <IC-RECEIVER>
            <Pid>{endkundId}</Pid>
        </IC-RECEIVER>
        <IR-Ref />
    </INTERCHANGE>
    <INVOICE>
        <HEADER>
            <FUNCTION-FLAGS>
                <Confirmation-Flag />
                <Canellation-Flag />
            </FUNCTION-FLAGS>
            <MESSAGE-REFERENCE>
                <REFERENCE-DATE>
                    <Reference-No>202307314522001</Reference-No>
                    <Date>{startDate:yyyyMMdd}</Date>
                </REFERENCE-DATE>
            </MESSAGE-REFERENCE>
            <PRINT-DATE>
                <Date>{startDate:yyyyMMdd}</Date>
            </PRINT-DATE>
            <REFERENCE>
                <INVOICE-REFERENCE>
                    <REFERENCE-DATE>
                        <Reference-No>{rechnungsnummer}</Reference-No>
                        <Date>{startDate:yyyyMMdd}</Date>
                    </REFERENCE-DATE>
                </INVOICE-REFERENCE>
                <ORDER>
                    <REFERENCE-DATE>
                        <Reference-No>{auftragsnummer}</Reference-No>
                        <Date>{startDate:yyyyMMdd}</Date>
                    </REFERENCE-DATE>
                </ORDER>
                <REMINDER Which="MAH">
                    <REFERENCE-DATE>
                        <Reference-No></Reference-No>
                        <Date></Date>
                    </REFERENCE-DATE>
                </REMINDER>
                <OTHER-REFERENCE Type="ADE">
                    <REFERENCE-DATE>
                        <Reference-No>202307164522001</Reference-No>
                        <Date>{startDate:yyyyMMdd}</Date>
                    </REFERENCE-DATE>
                </OTHER-REFERENCE>
            </REFERENCE>
            <BILLER>
                <Tax-No>{rechnungsname}</Tax-No>
                <Doc-Reference Type="ESR-ALT "></Doc-Reference>
                <PARTY-ID>
                    <Pid>{herkunftId}</Pid>
                </PARTY-ID>
                <NAME-ADDRESS Format="COM">
                    <NAME>
                        <Line-35>{herkunftName}</Line-35>
                        <Line-35>{herkunftAdresse1}</Line-35>
                        <Line-35>{herkunftAdresse2}</Line-35>
                        <Line-35></Line-35>
                        <Line-35></Line-35>
                    </NAME>
                    <STREET>
                        <Line-35></Line-35>
                        <Line-35></Line-35>
                        <Line-35></Line-35>
                    </STREET>
                    <City></City>
                    <State></State>
                    <Zip></Zip>
                    <Country></Country>
                </NAME-ADDRESS>
                <BANK-INFO>
                    <Acct-No></Acct-No>
                    <Acct-Name></Acct-Name>
                    <BankId Type="BCNr-nat" Country="CH">001996</BankId>
                </BANK-INFO>
            </BILLER>
            <PAYER>
                <PARTY-ID>
                    <Pid>{endkundId}</Pid>
                </PARTY-ID>
                <NAME-ADDRESS Format="COM">
                    <NAME>
                        <Line-35>{endkundeName}</Line-35>
                        <Line-35>{endkundeAdresse1}</Line-35>
                        <Line-35>{endkundeAdresse2}</Line-35>
                        <Line-35></Line-35>
                        <Line-35></Line-35>
                    </NAME>
                    <STREET>
                        <Line-35></Line-35>
                        <Line-35></Line-35>
                        <Line-35></Line-35>
                    </STREET>
                    <City></City>
                    <State></State>
                    <Zip></Zip>
                    <Country></Country>
                </NAME-ADDRESS>
            </PAYER>
        </HEADER>
        <LINE-ITEM />
        <SUMMARY>
            <INVOICE-AMOUNT>
                <Amount>0000135000</Amount>
            </INVOICE-AMOUNT>
            <VAT-AMOUNT>
                <Amount></Amount>
            </VAT-AMOUNT>
            <DEPOSIT-AMOUNT>
                <Amount></Amount>
                <REFERENCE-DATE>
                    <Reference-No></Reference-No>
                    <Date></Date>
                </REFERENCE-DATE>
            </DEPOSIT-AMOUNT>
            <EXTENDED-AMOUNT Type="79">
                <Amount></Amount>
            </EXTENDED-AMOUNT>
            <TAX>
                <TAX-BASIS>
                    <Amount></Amount>
                </TAX-BASIS>
                <Rate Categorie="S">0</Rate>
                <Amount></Amount>
            </TAX>
            <PAYMENT-TERMS>
                <BASIC Payment-Type="ESR" Terms-Type="1">
                    <TERMS>
                        <Payment-Period Type="M" On-Or-After="1" Reference-Day="31">30</Payment-Period>
                        <Date>20230830</Date>
                    </TERMS>
                </BASIC>
                <DISCOUNT Terms-Type="22">
                    <Discount-Percentage>0.0</Discount-Percentage>
                    <TERMS>
                        <Payment-Period Type="M" On-Or-After="1" Reference-Day="31"></Payment-Period>
                        <Date></Date>
                    </TERMS>
                    <Back-Pack-Container Encode="Base64"> </Back-Pack-Container>
                </DISCOUNT>
            </PAYMENT-TERMS>
        </SUMMARY>
    </INVOICE>
</XML-FSCM-INVOICE-2003A>
""";

Logger.Info("Generierung von xml Datei abgeschlosseen");


var invoiceXmlFile = Path.Combine(myKoohlDirectory, "invoice.xml");
var invoiceTxtFile = Path.Combine(myKoohlDirectory, "invoice.txt");

File.WriteAllText(invoiceXmlFile, xml);
File.WriteAllText(invoiceTxtFile, txt);

ftpSix.UploadFile(invoiceXmlFile, $"in/AP20bBruehwiler/{xmlFileName}");
ftpSix.UploadFile(invoiceTxtFile, $"in/AP20bBruehwiler/{txtFileName}");

Logger.Info("Upload von xml Datei abgeschlosseen");
Logger.Info("Upload von txt Datei abgeschlosseen");

using System.Globalization;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using FluentFTP;
using FluentFTP.Helpers;

// using var client = new FtpClient
// {
//     Host = "ftp.haraldmueller.ch",
//     Credentials = new NetworkCredential("schueler", "studentenpasswort")
// };
//
// client.Connect();
//
// var stream = new MemoryStream();
// client.DownloadStream(stream, "/M122-AP20b/Bruehwiler");
// var reader = new StreamReader(stream);
// var text = reader.ReadToEnd();

var text = File.ReadAllText("Input.data");

var fileContnet = text.Split("\n").Select(x => x.Split(";")).ToArray();

var header = fileContnet.First();
var herkunft = fileContnet.First(x => x.First() == "Herkunft");
var endkunde = fileContnet.First(x => x.First() == "Endkunde");
var content = fileContnet.Where(x => x.First() == "RechnPos").ToArray();

var rechnungsnummer = header[0].RemovePrefix("Rechnung_"); //23003
var auftragsnummer = header[1].RemovePrefix("Auftrag_"); //A003

var datum = header[3]; //21.07.2023

var herkunft_name = herkunft[3]; //Adam Adler
var herkunft_adresse1 = herkunft[4]; //Bahnhofstrasse 1
var herkunft_adresse2 = herkunft[5].Trim(); //8000 Zuerich

var kundennummer = herkunft[2]; //K821
var rechnungsname = herkunft[6]; //CHE-111.222.333 MWST

var endkunde_name = endkunde[2]; //Autoleasing AG
var endkunde_adresse1 = endkunde[3]; //Gewerbestrasse 100
var endkunde_adresse2 = endkunde[4].Trim(); //5000 Aarau

var stringItems = string.Empty;

var totalAmount = 0;

foreach (var row in content)
{
    var nr = row[1];
    var name = row[2];
    var count = row[3];
    var amount1 = row[4];
    var amount2 = row[5];
    var mwst = row[6];

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



var padding1 = herkunft_name;
padding1 += new string(' ', 27 - herkunft_name.Length);

var padding2 = herkunft_adresse1;
padding2 += new string(' ', 27 - herkunft_adresse1.Length);

var padding3 = herkunft_adresse2;
padding3 += new string(' ', 27 - herkunft_adresse2.Length);

var padding4 = endkunde_name;
padding4 += new string(' ', 27 - endkunde_name.Length);

var padding5 = endkunde_adresse1;
padding5 += new string(' ', 27 - endkunde_adresse1.Length);

var padding6 = endkunde_adresse2;
padding6 += new string(' ', 27 - endkunde_adresse2.Length);


var txt = @$"-------------------------------------------------



{herkunft_name}
{herkunft_adresse1}
{herkunft_adresse2}

{rechnungsname}




Uster, den {datum}                                 {endkunde_name}
                                                {endkunde_adresse1}
                                                {endkunde_adresse2}

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

{padding1}------------------------  {herkunft_name}       
{padding2}|  QR-CODE             |  {herkunft_adresse1} 
{padding3}|                      |  {herkunft_adresse2}
                           |                      |  
                           |                      |  
00 00000 00000 00000 00000 |                      |  00 00000 00000 00000 00000
                           |                      |     
{padding4}|                      |  {endkunde_name}
{padding5}|                      |  {endkunde_adresse1}
{padding6}|                      |  {endkunde_adresse2}
                           ------------------------
Währung  Betrag            Währung  Betrag
CHF      {string2Total}CHF      {formatedTotalAmount}								

-------------------------------------------------
";

//Generate Rechnung als TXT

var txtFileName = $"{kundennummer}_{rechnungsnummer}_invoice.txt";

File.WriteAllText(txtFileName, txt);

//Generate Rechnung als XML

var xmlFileName = "[Kundennummer]_[Rechnungsnummer]_invoice.xml";
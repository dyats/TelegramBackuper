using System.Text;
using HtmlAgilityPack;
using OrderGiv3r.VideoDownloader;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var web = new HtmlWeb();
var document = web.Load(Url);
var decodedHtmlComponent = document.GetHtmlComponent();
var link = decodedHtmlComponent.GetLinkFromComponent();

var httpClient = new HttpClient();
await httpClient.DownloadFileAsync(new Uri(link), PathToDownload);

Console.ReadLine();
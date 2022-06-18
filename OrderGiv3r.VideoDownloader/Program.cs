using System.Text;
using HtmlAgilityPack;
using OrderGiv3r.VideoDownloader;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var web = new HtmlWeb();
var document = web.Load(Url);
var decodedHtmlComponent = document.GetHtmlComponent();
var url = decodedHtmlComponent.GetLinkFromComponent();

var client = CreateHttpClient();
await client.DownloadFileAsync(url, PathToDownload);

Console.ReadLine();
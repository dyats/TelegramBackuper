using System.Text;
using HtmlAgilityPack;
using OrderGiv3r.VideoDownloader;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var web = new HtmlWeb();
var url = BaseUrl + VideoNumber;
var document = web.Load(url);
var downloadFromUrl = document.GetUrlForDownload();
var pathToDownload = DesktopPath + VideoNumber + VideoFormat;
var client = CreateHttpClient();
await client.DownloadFileAsync(downloadFromUrl, pathToDownload);

Console.ReadLine();
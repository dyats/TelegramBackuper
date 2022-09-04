using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using OrderGiv3r.VideoDownloader;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

var videoNumber = 11111;

var web = new HtmlWeb();
var url = config["BaseUrl"] + videoNumber;
var document = web.Load(url);
var downloadFromUrl = document.GetUrlForDownload();
var pathToDownload = config["PathToDownload"] + videoNumber + config["VideoFormat"];
var client = CreateHttpClient();
await client.DownloadFileAsync(downloadFromUrl, pathToDownload);

Console.ReadLine();
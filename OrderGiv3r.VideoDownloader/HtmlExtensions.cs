using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace OrderGiv3r.VideoDownloader;

public static class HtmlExtensions
{
    public static string GetUrlForDownload(this HtmlDocument document)
    {
        var match = Regex.Match(document.ParsedText, HtmlMatchCondition);
        return match.Groups[RegexMatchedGroupId].Value;
    }
}
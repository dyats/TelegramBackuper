using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace OrderGiv3r.VideoDownloader;

public static class HtmlExtensions
{
    public static string GetHtmlComponent(this HtmlDocument document)
    {
        var match = Regex.Match(document.ParsedText, HtmlMatchCondition);
        var encodedUri = match.Groups[RegexMatchedGroupId].Value;
        var decodedHtmlComponent = HttpUtility.UrlDecode(encodedUri);
        return decodedHtmlComponent;
    }

    public static string GetLinkFromComponent(this string decodedHtmlComponent)
    {
        var document = new HtmlDocument();
        document.LoadHtml(decodedHtmlComponent);
        var link = document.DocumentNode.SelectSingleNode(UlMatchCondition).Attributes[LiAttribute].Value;
        return link;
    }
}
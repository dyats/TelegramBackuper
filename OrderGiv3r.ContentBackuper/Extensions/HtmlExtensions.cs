using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace OrderGiv3r.Application.Extensions;

public static class HtmlExtensions
{
    public static string GetUrlForDownload(this HtmlDocument document, string matchCondition, int matchedGroup)
    {
        var match = Regex.Match(document.ParsedText, matchCondition);
        return match.Groups[matchedGroup].Value;
    }
}
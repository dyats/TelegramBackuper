namespace OrderGiv3r.Bot;

public static class Constants
{
    public static class RegexCondition
    {
        public const string Link = "((http(s)?(\\:\\/\\/))+(www\\.)?([\\w\\-\\.\\/])*(\\.[a-zA-Z]{2,3}\\/?))[^\\s\\b\\n|]*[^.,;:\\?\\!\\@\\^\\$ -]";
        public const string NumbersInTheEnd = @"\d+$";

        public static class Twitter
        {
            public const string TweetId = "(^.*)/(\\d*)";
        } 
    }
}

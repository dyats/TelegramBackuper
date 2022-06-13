namespace OrderGiv3r.Bot;

public static class Config
{
    public static string OrderGiv3rConfig(string what)
    {
        if (what == "api_id") return ApiId;
        if (what == "api_hash") return ApiHash;
        if (what == "phone_number") return PhoneNumber;
        if (what == "verification_code") return null; // let WTelegramClient ask the user with a console prompt 
        if (what == "password") return Password;     // if user has enabled 2FA
        return null;
    }
}
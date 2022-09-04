﻿using Microsoft.Extensions.Configuration;

namespace OrderGiv3r.Bot;

public class OrderGiv3rConfig
{
    public static string ApiId;
    public static string ApiHash;
    public static string PhoneNumber;
    public static string Password;


    public OrderGiv3rConfig(IConfigurationRoot appConfig)
    {
        ApiId = appConfig["ApiId"];
        ApiHash = appConfig["ApiHash"];
        PhoneNumber = appConfig["PhoneNumber"];
        Password = appConfig["Password"];
    }

    public string GetConfig(string what) =>
        what switch
        {
            "api_id" => ApiId,
            "api_hash" => ApiHash,
            "phone_number" => PhoneNumber,
            "verification_code" => null,  // let WTelegramClient ask the user with a console prompt 
            "password" => Password,// if user has enabled 2FA
            _ => null
        };
}
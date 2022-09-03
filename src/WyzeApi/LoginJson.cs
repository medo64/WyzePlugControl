namespace WyzeApi;

using System.Text.Json.Serialization;

public struct LoginJson {

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("mfa_options")]
    public string[] MfaOptions { get; set; }

    [JsonPropertyName("mfa_details")]
    public LoginMfaDetailsJson? MfaDetails { get; set; }

}

public struct LoginMfaDetailsJson {

    [JsonPropertyName("totp_apps")]
    public LoginTotpAppJson[] TotpApps { get; set; }

}

public struct LoginTotpAppJson {

    [JsonPropertyName("app_id")]
    public string? Id { get; set; }

    [JsonPropertyName("app_nickname")]
    public string? Nickname { get; set; }

}

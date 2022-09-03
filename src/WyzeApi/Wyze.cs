using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Medo.Security.Cryptography;

namespace WyzeApi {
    /// <summary>
    /// Wyze API account.
    /// </summary>
    public sealed class Wyze : IDisposable {

        /// <summary>
        /// Logs user in. Needs to be done before any other action.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <param name="totpKey">TOTP key.</param>
        public bool Login(string email, string password, string? totpKey) {
            var reqLogin = new HttpRequestMessage(HttpMethod.Post, ApiUrlLogin);
            reqLogin.Headers.Add("x-api-key", ApiKey);
            reqLogin.Headers.Add("phone-id", ApiPhoneId);

            var md5Password = ToMd5(ToMd5(ToMd5(password)));
            reqLogin.Content = GetJsonContentType(new {
                email,
                password = md5Password,
            });

            LoginJson loginJson;
            try {
                var resLogin = HttpClient.Send(reqLogin);
                var jsonText = resLogin.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine("[Wyze] Login: " + jsonText);
                loginJson = JsonSerializer.Deserialize<LoginJson>(jsonText);
            } catch (Exception ex) {
                Debug.WriteLine("[Wyze] Login exception: " + ex.Message);
                return false;
            }

            if (loginJson.AccessToken != null) {  // no MFA is needed
                AccessToken = loginJson.AccessToken;
                return true;
            }

            if (!Array.Exists(loginJson.MfaOptions, element => element == "TotpVerificationCode")) {  // no TOTP
                return false;
            }

            if (totpKey == null) { return false; }  // no key, nothing to do

            var reqLogin2 = new HttpRequestMessage(HttpMethod.Post, ApiUrlLogin);
            reqLogin2.Headers.Add("x-api-key", ApiKey);
            reqLogin2.Headers.Add("phone-id", ApiPhoneId);

            var mfaCode = new OneTimePassword(totpKey);
            reqLogin2.Content = GetJsonContentType(new {
                email,
                password = md5Password,
                verification_id = "8b4e53d0-0f83-4b8e-9078-2311d0b0badd",
                mfa_type = "TotpVerificationCode",
                verification_code= mfaCode.GetCode().ToString("000000"),
            });

            LoginJson login2Json;
            try {
                var resLogin2 = HttpClient.Send(reqLogin2);
                var jsonText = resLogin2.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine("[Wyze] Login2: " + jsonText);
                login2Json = JsonSerializer.Deserialize<LoginJson>(jsonText);
            } catch (Exception ex) {
                Debug.WriteLine("[Wyze] Login2 exception: " + ex.Message);
                return false;
            }

            if (login2Json.AccessToken != null) {
                AccessToken = login2Json.AccessToken;
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Logs user out.
        /// </summary>
        public bool Logout() {
            return true;  // TODO
        }


        /// <summary>
        /// Returns all devices associated with the user account.
        /// </summary>
        public IEnumerable<WyzeDevice> GetDevices() {
            if (AccessToken == null) { throw new InvalidOperationException("User not logged in."); }

            var req = new HttpRequestMessage(HttpMethod.Post, ApiUrlDeviceList) {
                Content = GetJsonContentType(new {
                    access_token = AccessToken,
                    sc = "9f275790cab94a72bd206c8876429f3c",
                    sv = "9d74946e652647e9b6c9d59326aef104",
                    app_ver = ApiAppVersion,
                    phone_id = ApiPhoneId,
                    ts = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds,
                })
            };

            var res = HttpClient.Send(req);
            var jsonText = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Debug.WriteLine("[Wyze] GetDevices: " + jsonText);

            var json = GetJsonElements(jsonText);
            if (json.TryGetValue("data", out var dataElement)) {
                var dataJson = GetJsonObjectElements(dataElement);
                if (dataJson.TryGetValue("device_list", out var deviceListElement)) {
                    var deviceList = GetJsonArrayElements(deviceListElement);
                    foreach (var device in deviceList) {
                        var properties = GetJsonObjectStringElements(device);
                        if (properties.TryGetValue("mac", out var mac)
                            && properties.TryGetValue("nickname", out var nickname)
                            && properties.TryGetValue("product_model", out var productModel)
                            && properties.TryGetValue("product_type", out var productType)) {
                            switch (productType) {
                                case "Plug": yield return new WyzePlugDevice(this, mac, nickname, productModel, productType); break;
                                case "OutdoorPlug": yield return new WyzePlugDevice(this, mac, nickname, productModel, productType); break;
                                default: yield return new WyzeDevice(this, mac, nickname, productModel, productType); break;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Changes state of a plug device.
        /// </summary>
        /// <param name="device">Device whose state needs to be changed.</param>
        /// <param name="newState">New plug state.</param>
        internal bool SetPlugPowerState(WyzePlugDevice device, bool newState) {
            if (AccessToken == null) { throw new InvalidOperationException("User not logged in."); }

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, ApiUrlRunAction);
            var req = httpRequestMessage;
            req.Content = GetJsonContentType(new {
                access_token = AccessToken,
                sc = "a626948714654991afd3c0dbd7cdb901",
                sv = "011a6b42d80a4f32b4cc24bb721c9c96",
                app_ver = ApiAppVersion,
                phone_id = ApiPhoneId,
                provider_key = device.ProductModel,
                instance_id = device.Id,
                action_key = newState ? "power_on" : "power_off",
                action_params = new object(),
                ts = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds,
            });

            string jsonText;
            try {
                var res = HttpClient.Send(req);
                jsonText = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine("[Wyze] SetPowerState: " + jsonText);
            } catch {
                return false;
            }

            var json = PartiallyParseJson(jsonText);
            if (json.TryGetValue("access_token", out var accessToken)) {
                AccessToken = accessToken;
                return true;
            } else {
                return false;
            }
        }


        /// <summary>
        /// Gets current user token.
        /// </summary>
        public string? AccessToken { get; private set; }


        #region IDispose

        public void Dispose() {
            if (AccessToken != null) {
                Logout();
                AccessToken = null;
            }
            GC.SuppressFinalize(this);
        }

        #endregion IDispose


        #region API

        public readonly string ApiUrlLogin = "https://auth-prod.api.wyze.com/user/login";
        public readonly string ApiUrlDeviceList = "https://api.wyzecam.com/app/v2/home_page/get_object_list";
        public readonly string ApiUrlRunAction = "https://api.wyzecam.com/app/v2/auto/run_action";

        public readonly string ApiKey = "RckMFKbsds5p6QY3COEXc2ABwNTYY0q18ziEiSEm";
        public readonly string ApiPhoneId = "411e313a-9f9e-11eb-a8b3-0242ac130005";
        public readonly string ApiAppVersion = "com.hualai___2.11.40";

        #endregion API


        #region Helpers

        private static readonly HttpClient HttpClient = new();

        private static string ToMd5(string value) {
            var inBytes = Encoding.UTF8.GetBytes(value);
            var md5Bytes = MD5.Create().ComputeHash(inBytes);
            var sbOutput = new StringBuilder();
            foreach (byte b in md5Bytes) {
                sbOutput.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            }
            return sbOutput.ToString();
        }

        private static IDictionary<string, JsonElement> GetJsonElements(string jsonText) {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);
            return dict ?? new Dictionary<string, JsonElement>();
        }

        private static IDictionary<string, JsonElement> GetJsonObjectElements(JsonElement baseElement) {
            var dict = new Dictionary<string, JsonElement>();
            if (baseElement.ValueKind == JsonValueKind.Object) {
                foreach (var element in baseElement.EnumerateObject()) {
                    dict.Add(element.Name, element.Value);
                }
            }
            return dict;
        }

        private static IEnumerable<JsonElement> GetJsonArrayElements(JsonElement baseElement) {
            if (baseElement.ValueKind == JsonValueKind.Array) {
                foreach (var element in baseElement.EnumerateArray()) {
                    yield return element;
                }
            }
        }

        private static IDictionary<string, string> GetJsonObjectStringElements(JsonElement baseElement) {
            var outputDict = new Dictionary<string, string>();
            if (baseElement.ValueKind == JsonValueKind.Object) {
                foreach (var element in baseElement.EnumerateObject()) {
                    var name = element.Name;
                    var value = element.Value;
                    switch (value.ValueKind) {
                        case JsonValueKind.String: outputDict.Add(name, value.GetString() ?? ""); break;
                        case JsonValueKind.Number: outputDict.Add(name, value.GetRawText()); break;
                        case JsonValueKind.False: outputDict.Add(name, "false"); break;
                        case JsonValueKind.True: outputDict.Add(name, "true"); break;
                    }
                }
            }
            return outputDict;
        }

        private static IDictionary<string, string?> PartiallyParseJson(string jsonText) {
            var outputDict = new Dictionary<string, string?>();
            var jsonDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);
            if (jsonDict != null) {
                foreach (var key in jsonDict.Keys) {
                    if (jsonDict.TryGetValue(key, out var element)) {
                        switch (element.ValueKind) {
                            case JsonValueKind.String: outputDict.Add(key, element.GetString()); break;
                            case JsonValueKind.Number: outputDict.Add(key, element.GetRawText()); break;
                            case JsonValueKind.False: outputDict.Add(key, "false"); break;
                            case JsonValueKind.True: outputDict.Add(key, "true"); break;
                        }
                    }
                }
            }
            return outputDict;
        }

        private static StringContent GetJsonContentType(object jsonObject) {
            var content = new StringContent(JsonSerializer.Serialize<object>(jsonObject));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        #endregion Helpers

    }
}

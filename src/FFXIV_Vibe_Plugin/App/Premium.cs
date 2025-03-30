using FFXIV_Vibe_Plugin.Commons;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace FFXIV_Vibe_Plugin.App
{
    public class Premium
    {
        private bool shouldStop;
        private bool isPremium;
        private string premiumLevel = "";
        private ConfigurationProfile ConfigurationProfile;
        public int TimerCheck = 60000;
        private string server = "http://3.77.95.185:5000";
        public int FreeAccount_MaxTriggers = 10;
        public bool invalidToken = true;
        public bool invalidTokenFormat;
        public string serverMsg = "";

        public Premium(ConfigurationProfile configurationProfile)
        {
            this.ConfigurationProfile = configurationProfile;
            new Task(obj =>
            {
                while (!this.shouldStop)
                {
                    this.updateStatus();
                    Thread.Sleep(this.TimerCheck);
                }
            }, (object)"updateStatusTask").Start();
        }

        public async void updateStatus()
        {
            string token = this.ConfigurationProfile.PREMIUM_TOKEN_SECRET;
            this.invalidTokenFormat = false;
            /*if (!this.isSha256(token))
            {
                this.invalidTokenFormat = true;
                this.resetPremium();
                token = (string) null;
            }
            else
            {
                this.Logger.Log("Updating Premium Status");
                string uriString = this.server + "/v2/token/" + token;
                try
                {
                using (HttpResponseMessage response = await new HttpClient()
                {
                    BaseAddress = new Uri(uriString)
                }.GetAsync(""))
                {
                    response.EnsureSuccessStatusCode();
                    JObject jobject = JObject.Parse(await response.Content.ReadAsStringAsync());
                    string serverToken = jobject["data"].ToString();
                    string str1 = jobject["error"].ToString();
                    string str2 = jobject["tier"].ToString();
                    long int64 = Convert.ToInt64((object) jobject["serverTime"]);
                    Logger logger1 = this.Logger;
                    DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 4);
                    interpolatedStringHandler.AppendLiteral("Web response: ");
                    interpolatedStringHandler.AppendFormatted(serverToken);
                    interpolatedStringHandler.AppendLiteral(" ");
                    interpolatedStringHandler.AppendFormatted(str1);
                    interpolatedStringHandler.AppendLiteral(" ");
                    interpolatedStringHandler.AppendFormatted<long>(int64);
                    interpolatedStringHandler.AppendLiteral(" ");
                    interpolatedStringHandler.AppendFormatted(str2);
                    string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
                    logger1.Log(stringAndClear1);
                    if (str1 != "")
                    {
                    this.serverMsg = str1;
                    this.Logger.Error("Error " + str1);
                    this.resetPremium();
                    }
                    else if (this.isSha256(serverToken) && this.isValidTokenUTC(int64, token, serverToken))
                    {
                    this.isPremium = true;
                    this.premiumLevel = str2 ?? "";
                    this.invalidToken = false;
                    this.serverMsg = "";
                    }
                    else
                    {
                    this.resetPremium();
                    this.serverMsg = "";
                    }
                    Logger logger2 = this.Logger;
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
                    interpolatedStringHandler.AppendLiteral("isPremium=");
                    interpolatedStringHandler.AppendFormatted<bool>(this.isPremium);
                    interpolatedStringHandler.AppendLiteral(", premiumLevel=");
                    interpolatedStringHandler.AppendFormatted(this.premiumLevel);
                    string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
                    logger2.Info(stringAndClear2);
                }
                token = (string) null;
                }
                catch (Exception ex)
                {
                this.Logger.Error("Could not connect to premium server. " + ex.Message);
                this.serverMsg = "Can't connect to server";
                this.resetPremium();
                token = (string) null;
                }
            }*/

            this.isPremium = true;
            this.premiumLevel = "";
            this.invalidToken = false;
            this.serverMsg = "";
        }

        private bool isSha256(string value) => Regex.IsMatch(value, "^[0-9a-fA-F]{64}$");

        public string HashWithSHA256(string value)
        {
          using (SHA256 shA256 = SHA256.Create())
            return Convert.ToHexString(shA256.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLower();
        }

        public bool IsPremium() => this.isPremium;

        public string GetPremiumLevel() => this.premiumLevel;

        private void resetPremium()
        {
            this.isPremium = false;
            this.premiumLevel = "";
            this.invalidToken = true;
        }

        private bool isValidTokenUTC(long serverTime, string token, string serverToken)
        {
            long num1 = serverTime - 60L;
            long num2 = num1 + 120L;
            bool flag = false;
            for (; num1 < num2; ++num1)
            {
              string str = this.HashWithSHA256(token + num1.ToString());
              if (serverToken == str)
              {
                flag = true;
                break;
              }
            }
            return flag;
        }

        public void Dispose() => this.shouldStop = true;
    }
}

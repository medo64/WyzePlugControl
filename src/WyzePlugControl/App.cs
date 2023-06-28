using System;
using System.Diagnostics;
using System.Threading;
using WyzeApi;

namespace WyzePlugControl {
    internal static class App {

        public static void Main(string[] args) {
            Medo.Application.SingleInstance.Attach();  // will exit on its own

            var plugId = (args.Length >= 1) ? args[0] : "";

            var apiKeyId = Environment.GetEnvironmentVariable("WYZE_APIKEYID") ?? "";
            var apiKey = Environment.GetEnvironmentVariable("WYZE_APIKEY") ?? "";
            if (string.IsNullOrEmpty(apiKeyId) || string.IsNullOrEmpty(apiKey)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Must define WYZE_APIKEYID and WYZE_APIKEY environment variables.");
                Console.ResetColor();
                Environment.Exit(1);
            }

            var email = Environment.GetEnvironmentVariable("WYZE_EMAIL") ?? "";
            var password = Environment.GetEnvironmentVariable("WYZE_PASSWORD") ?? "";
            var totp = Environment.GetEnvironmentVariable("WYZE_TOTP") ?? "";
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Must define WYZE_EMAIL and WYZE_PASSWORD environment variables.");
                Console.ResetColor();
                Environment.Exit(1);
            }

            WyzePlugDevice? selectedPlug = null;
            using var wyze = new Wyze(apiKeyId, apiKey);
            if (wyze.Login(email, password, totp)) {
                foreach (var device in wyze.GetDevices()) {
                    if (device is WyzePlugDevice plug) {
                        if (plug.Id.Equals(plugId, StringComparison.OrdinalIgnoreCase)) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            selectedPlug = plug;
                        } else {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }
                        Console.WriteLine(plug.Id + " " + plug.Nickname);
                    } else {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(device.Id + " " + device.Nickname);
                    }
                }
                Console.ResetColor();
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error logging on.");
                Console.ResetColor();
                Environment.Exit(2);
            }

            if (selectedPlug == null) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cannot find plug.");
                Console.ResetColor();
                Environment.Exit(2);
            }


            Thread.Sleep(1000);

            var internalMicStartInfo = new ProcessStartInfo {
                FileName = @"/usr/bin/amixer",
                Arguments = "get Capture",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            bool? lastState = null;
            while (true) {
                var result = Process.Start(internalMicStartInfo)!;
                if (result != null) {
                    var outputText = result.StandardOutput.ReadToEnd();
                    var nextState = !Grep(outputText, "[off]");

                    if (DateTime.Now.Hour is < 8 or >= 22) { nextState = false; }  // force disable between 22:00 and 08:00

                    if (nextState != lastState) {
                        selectedPlug.SetPowerState(nextState);
                        lastState = nextState;
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No output");
                    Console.ResetColor();
                }
                Thread.Sleep(1);
            }
        }


        private static bool Grep(string text, string filter) {
            var lines = text.Split("\n");
            foreach (var line in lines) {
                if (line.Contains(filter)) { return true; }
            }
            return false;
        }

    }
}

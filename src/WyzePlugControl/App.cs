using System;
using WyzeApi;

namespace WyzePlugControl {
    internal static class App {

        public static void Main(string[] args) {
            var plugId = (args.Length >= 1) ? args[0] : "";
            var desiredState = (args.Length >= 2) ? args[1] : "";

            var email = Environment.GetEnvironmentVariable("WYZE_EMAIL") ?? "";
            var password = Environment.GetEnvironmentVariable("WYZE_PASSWORD") ?? "";
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Must define WYZE_EMAIL and WYZE_PASSWORD environment variables.");
                Console.ResetColor();
                Environment.Exit(1);
            }

            using var wyze = new Wyze();
            if (wyze.Login(email, password)) {
                foreach (var device in wyze.GetDevices()) {
                    if (device is WyzePlugDevice plug) {
                        if (plug.Id == plugId) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            if (bool.TryParse(desiredState, out var newState)) {
                                plug.SetPowerState(newState);
                            }
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
        }

    }
}

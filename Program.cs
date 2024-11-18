using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

// TODO: Check out OBS windows 10 capture method

namespace HolocureSlotsBot
{
    internal readonly struct Settings
    {
        public string[] Buttons { get; }

        public Settings(string[] buttons)
        {
            Buttons = buttons;
        }
    }

    static partial class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private static bool _running = true;
        private static readonly Settings _settings;

        private static readonly IntPtr _windowHandle;

        static Program()
        {
            // Keep console open on crash
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                ExceptionHandler
            );

            // Setup
            try
            {
                _settings = GetSettings();
                Console.WriteLine(
                   "Holocure settings found."
                );
                Console.WriteLine(
                        $"Buttons: [{string.Join(", ", _settings.Buttons)}]"
                );

                _windowHandle = GetWindow();
                Console.WriteLine(
                  "Holocure window found."
                );

                Console.WriteLine();
            }
            catch (Exception e)
            {
                // Manually call exception handler as it has not been set yet
                ExceptionHandler(null, new UnhandledExceptionEventArgs(e, false));
            }
        }

        private static void Main(string[] args)
        {
            PreventSleep();

            Console.WriteLine("Bot started.\n");

            // Start bot loop
            int cycleCount = 0;
            Stopwatch perfSw = Stopwatch.StartNew();
            while (true)
            {
                perfSw.Stop();
                HandleKeys();
                perfSw.Start();

                // Play game
                if (_running)
                {
                    InputUtils.SendKeyPress(_windowHandle, _settings.Buttons[0]);
                }

                // Print cycles per second
                cycleCount++;
                if (perfSw.ElapsedMilliseconds >= 4545)
                {
                    Console.WriteLine($"Cycles per second: {cycleCount / perfSw.Elapsed.TotalSeconds:F2}");
                    cycleCount = 0;
                    perfSw.Restart();
                }
            }
        }

        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Console.WriteLine($"\n{args.ExceptionObject}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }

        private static void PreventSleep()
        {
            const uint ES_CONTINUOUS = 0x80000000;
            const uint ES_DISPLAY_REQUIRED = 0x00000002;

            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED);
        }

        private static void HandleKeys()
        {
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.KeyChar == 'p' || info.KeyChar == 'p')
                {
                    _running = !_running;
                    if (!_running)
                    {
                        Console.WriteLine("paused");
                    }
                    else
                    {
                        Console.WriteLine("unpaused");
                    }
                }
            }
        }

        private static Settings GetSettings()
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = $"{userPath}\\AppData\\Local\\HoloCure\\settings.json";
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Settings file not found.");
            }

            string jsonText = File.ReadAllText(filePath);

            Match buttonsMatch = Regex.Match(
                jsonText,
                @"""theButtons"":\[(""\w+""(,""\w+""){5,})\]",
                RegexOptions.IgnoreCase
            );
            if (!buttonsMatch.Success)
            {
                throw new Exception(
                        "settings.json was not formatted correctly. Could not find key bindings."
                );
            }
            string buttonsStr = buttonsMatch.Groups[1].Value.Trim();
            string[] buttons = buttonsStr.Split(',').Select(s => s.Trim(' ', '"')).ToArray();

            return new Settings(buttons);
        }

        private static IntPtr GetWindow()
        {
            Process[] processes = Process.GetProcessesByName("holocure");
            if (processes.Length <= 0)
            {
                throw new Exception("Please open HoloCure.");
            }

            return processes[0].MainWindowHandle;
        }
    }
}

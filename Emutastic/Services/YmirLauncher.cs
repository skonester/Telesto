using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Emutastic.Configuration;
using Emutastic.Models;

namespace Emutastic.Services
{
    public static class YmirLauncher
    {
        public const string EmbeddedCoreId = "ymir_embedded";
        public const string StandaloneCoreId = "ymir_standalone";
        public const string CoreId = StandaloneCoreId;
        public const string EmbeddedDisplayName = "Ymir (embedded experimental)";
        public const string StandaloneDisplayName = "Ymir (standalone fallback)";
        public const string DisplayName = EmbeddedDisplayName;

        public static bool IsYmirCore(string coreName)
            => IsEmbeddedCore(coreName) || IsStandaloneCore(coreName);

        public static bool IsEmbeddedCore(string coreName)
            => string.Equals(coreName, EmbeddedCoreId, StringComparison.OrdinalIgnoreCase);

        public static bool IsStandaloneCore(string coreName)
            => string.Equals(coreName, StandaloneCoreId, StringComparison.OrdinalIgnoreCase);

        public static bool IsAvailable()
            => YmirNativeCore.IsAvailable() || GetExecutablePath() != null;

        public static bool IsStandaloneAvailable()
            => GetExecutablePath() != null;

        public static bool IsEmbeddedAvailable()
            => YmirNativeCore.IsAvailable();

        public static string? GetExecutablePath()
        {
            foreach (string dir in GetCandidateFolders())
            {
                foreach (string exeName in GetExecutableNames())
                {
                    string exe = Path.Combine(dir, exeName);
                    if (File.Exists(exe))
                        return exe;
                }
            }

            return null;
        }

        public static bool IsPreferredFor(Game game, IConfigurationService? configService)
            => GetPreferredYmirCore(game, configService) != null;

        public static string? GetPreferredYmirCore(Game game, IConfigurationService? configService)
        {
            if (!string.Equals(game.Console, "Saturn", StringComparison.OrdinalIgnoreCase))
                return null;

            var preferences = configService?.GetCorePreferences();
            return preferences?.PreferredCores.TryGetValue("Saturn", out string? preferred) == true
                && IsYmirCore(preferred)
                    ? preferred
                    : null;
        }

        public static Process Launch(Game game)
        {
            string exePath = GetExecutablePath()
                ?? throw new FileNotFoundException("Ymir standalone executable was not found.", "ymir.sdl3.exe, ymir-sdl3.exe, or ymir.exe");

            if (!File.Exists(game.RomPath))
                throw new FileNotFoundException("ROM file not found.", game.RomPath);

            string ymirDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            string profileDir = AppPaths.GetFolder("YmirProfiles", "default");
            EnsureProfileSeeded(ymirDir, profileDir);
            SyncSaturnBios(profileDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = ymirDir,
                UseShellExecute = false,
                Arguments = $"--disc {QuoteArg(game.RomPath)} --profile {QuoteArg(profileDir)}"
            };

            return Process.Start(startInfo)
                ?? throw new InvalidOperationException("Ymir process did not start.");
        }

        public static string InstallStatusText()
            => IsAvailable() ? "installed" : "not installed";

        private static void EnsureProfileSeeded(string ymirDir, string profileDir)
        {
            Directory.CreateDirectory(profileDir);

            string sourceConfig = Path.Combine(ymirDir, "Ymir.toml");
            string destConfig = Path.Combine(profileDir, "Ymir.toml");
            if (File.Exists(sourceConfig) && !File.Exists(destConfig))
                File.Copy(sourceConfig, destConfig);
            ConfigureProfile(destConfig);

            string sourceIpl = Path.Combine(ymirDir, "ipl.bin");
            if (File.Exists(sourceIpl))
            {
                string iplDir = Path.Combine(profileDir, "roms", "ipl");
                Directory.CreateDirectory(iplDir);
                string destIpl = Path.Combine(iplDir, Path.GetFileName(sourceIpl));
                if (!File.Exists(destIpl))
                    File.Copy(sourceIpl, destIpl);
            }
        }

        private static void SyncSaturnBios(string profileDir)
        {
            string systemDir = AppPaths.GetFolder("System");
            string iplDir = Path.Combine(profileDir, "roms", "ipl");
            Directory.CreateDirectory(iplDir);

            foreach (string fileName in new[] { "sega_101.bin", "mpr-17933.bin", "mpr-17941.bin" })
            {
                string source = Path.Combine(systemDir, fileName);
                if (!File.Exists(source))
                    continue;

                string dest = Path.Combine(iplDir, fileName);
                if (!File.Exists(dest))
                    File.Copy(source, dest);
            }
        }

        private static void ConfigureProfile(string configPath)
        {
            if (!File.Exists(configPath))
                return;

            try
            {
                string config = File.ReadAllText(configPath, Encoding.UTF8);
                config = ReplaceTomlBool(config, "CheckForUpdates", false);
                config = ReplaceTomlBool(config, "InternalBackupRAMPerGame", true);
                File.WriteAllText(configPath, config, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[YmirLauncher] Failed to configure Ymir profile: {ex.Message}");
            }
        }

        private static string ReplaceTomlBool(string text, string key, bool value)
        {
            string replacement = $"{key} = {value.ToString().ToLowerInvariant()}";
            string pattern = $@"(?m)^{Regex.Escape(key)}\s*=\s*(true|false)\s*$";
            return Regex.IsMatch(text, pattern)
                ? Regex.Replace(text, pattern, replacement)
                : text + Environment.NewLine + replacement + Environment.NewLine;
        }

        private static IEnumerable<string> GetCandidateFolders()
        {
            string exeFolder = AppPaths.GetExeFolder();
            yield return Path.Combine(exeFolder, "ymircore");
            yield return Path.Combine(exeFolder, "portable", "ymircore");
            yield return Path.Combine(AppPaths.GetNativeFolder(), "ymircore");

            string? current = AppContext.BaseDirectory;
            for (int i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
            {
                yield return Path.Combine(current, "portable", "ymircore");
                current = Directory.GetParent(current)?.FullName;
            }

            yield return Path.Combine(Environment.CurrentDirectory, "portable", "ymircore");
        }

        private static IEnumerable<string> GetExecutableNames()
        {
            yield return "ymir.sdl3.exe";
            yield return "ymir-sdl3.exe";
            yield return "ymir.exe";
        }

        private static string QuoteArg(string value)
            => "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}

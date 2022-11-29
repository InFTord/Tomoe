using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using OoLunar.DSharpPlus.CommandAll.Attributes;
using OoLunar.DSharpPlus.CommandAll.Commands;

namespace OoLunar.Tomoe.Commands.Common
{
    public sealed partial class BotInfoCommand : BaseCommand
    {
        [Command("bot_info")]
        public static Task BotInfoAsync(CommandContext context)
        {
            DiscordEmbedBuilder embedBuilder = new()
            {
                Title = "Bot Info",
                Color = new DiscordColor("#7b84d1")
            };

            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            embedBuilder.AddField("Heap Memory", GC.GetTotalMemory(false).Bytes().ToString("MB", CultureInfo.InvariantCulture), true);
            embedBuilder.AddField("Process Memory", currentProcess.WorkingSet64.Bytes().ToString("MB", CultureInfo.InvariantCulture), true);
            embedBuilder.AddField("Total Memory Available", currentProcess.PrivateMemorySize64.Bytes().ToString("MB", CultureInfo.InvariantCulture), true);

            embedBuilder.AddField("Runtime Version", RuntimeInformation.FrameworkDescription, true);
            embedBuilder.AddField("Guild Count", context.Client.Guilds.Count.ToMetric(), true);

            embedBuilder.AddField("Prefixes", "`/`", true);
            embedBuilder.AddField("Bot Uptime", LastCommaRegex().Replace((Process.GetCurrentProcess().StartTime - DateTime.Now).Humanize(3), " and "), true);
            embedBuilder.AddField("Bot Version", typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion, true);

            embedBuilder.AddField("DSharpPlus Library Version", typeof(DiscordClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion, true);
            embedBuilder.AddField("Websocket Ping", context.Client.Ping + "ms", true);
            embedBuilder.AddField("Operating System", $"{Environment.OSVersion} {RuntimeInformation.OSArchitecture.ToString().ToLower(CultureInfo.InvariantCulture)}", true);

            return context.ReplyAsync(new DiscordMessageBuilder().AddEmbed(embedBuilder));
        }

        [GeneratedRegex(", (?=[^,]*$)", RegexOptions.Compiled)]
        private static partial Regex LastCommaRegex();
    }
}
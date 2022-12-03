using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using OoLunar.DSharpPlus.CommandAll;
using OoLunar.DSharpPlus.CommandAll.EventArgs;
using OoLunar.DSharpPlus.CommandAll.Exceptions;

namespace OoLunar.Tomoe.Events.Handlers
{
    public sealed class CommandErorredHandler
    {
        [DiscordEvent]
        public static Task OnErroredAsync(CommandAllExtension extension, CommandErroredEventArgs eventArgs)
        {
            if (eventArgs.Exception is CommandNotFoundException commandNotFoundException)
            {
                return eventArgs.Context.ReplyAsync($"Unknown command: {commandNotFoundException.CommandString}");
            }

            DiscordEmbedBuilder embedBuilder = new()
            {
                Title = "Command Error",
                Description = $"{Formatter.InlineCode(eventArgs.Context.CurrentCommand.FullName)} failed to execute.",
                Color = new DiscordColor("#6b73db")
            };

            switch (eventArgs.Exception)
            {
                case DiscordException discordError:
                    embedBuilder.AddField("HTTP Code", discordError.WebResponse.ResponseCode.ToString(), true);
                    embedBuilder.AddField("Error Message", discordError.JsonMessage, true);
                    return eventArgs.Context.ReplyAsync(new DiscordMessageBuilder().AddEmbed(embedBuilder));
                default:
                    embedBuilder.AddField("Error Message", eventArgs.Exception.Message, true);
                    embedBuilder.AddField("Stack Trace", Formatter.BlockCode(FormatStackTrace(eventArgs.Exception.StackTrace), "csharp"), false);
                    return eventArgs.Context.ReplyAsync(new DiscordMessageBuilder().AddEmbed(embedBuilder));
            }
        }

        private static string FormatStackTrace(string? text) => text == null
            ? "No stack trace available."
            : string.Join('\n', text.Split('\n').Select(line => ReplaceFirst(line.Trim(), "at", "-")));

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            ReadOnlySpan<char> textSpan = text.AsSpan();
            return pos < 0 ? text : string.Concat(textSpan[..pos], replace, textSpan[(pos + search.Length)..]);
        }
    }
}

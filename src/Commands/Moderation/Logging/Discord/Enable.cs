namespace Tomoe.Commands
{
    using DSharpPlus;
    using DSharpPlus.SlashCommands;
    using System.Linq;
    using System.Threading.Tasks;
    using Tomoe.Commands.Attributes;
    using Tomoe.Db;

    public partial class Moderation : SlashCommandModule
    {
        public partial class Logging : SlashCommandModule
        {
            public partial class Discord : SlashCommandModule
            {
                [SlashCommand("enable", "Changes where events are logged."), Hierarchy(Permissions.ManageGuild)]
                public async Task Enable(InteractionContext context, [Option("log_type", "Which event to change.")] DiscordEvent logType)
                {
                    LogSetting logSetting = Database.LogSettings.FirstOrDefault(databaseLogSetting => databaseLogSetting.GuildId == context.Guild.Id && databaseLogSetting.DiscordEvent == logType);
                    if (logSetting == null)
                    {
                        await context.EditResponseAsync(new()
                        {
                            Content = $"Error: The {Formatter.InlineCode(logType.ToString())} event was never setup! Run {Formatter.InlineCode("/logging change")} to do so now.",
                        });
                        return;
                    }
                    else
                    {
                        logSetting.IsLoggingEnabled = true;
                    }
                    await Database.SaveChangesAsync();

                    await context.EditResponseAsync(new()
                    {
                        Content = $"All messages related to the {Formatter.InlineCode(logType.ToString())} event will be logged."
                    });
                }
            }
        }
    }
}
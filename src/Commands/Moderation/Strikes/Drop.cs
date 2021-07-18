namespace Tomoe.Commands
{
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Tomoe.Commands.Attributes;
    using Tomoe.Db;

    public partial class Moderation : SlashCommandModule
    {
        public partial class Strikes : SlashCommandModule
        {
            [SlashCommand("drop", "Drops a previously issued strike for an individual."), Hierarchy(Permissions.KickMembers)]
            public async Task Drop(InteractionContext context, [Option("strike_id", "Which strike to drop.")] long strikeId, [Option("reason", "Why is the strike being dropped?")] string reason = Constants.MissingReason)
            {
                Strike strike = Database.Strikes.FirstOrDefault(databaseStrike => databaseStrike.LogId == strikeId && databaseStrike.GuildId == context.Guild.Id);
                if (strike.Dropped)
                {
                    await context.EditResponseAsync(new()
                    {
                        Content = $"Error: Strike #{strikeId} is already dropped!"
                    });
                }

                DiscordMember guildVictim = await strike.VictimId.GetMember(context.Guild);
                bool sentDm = await guildVictim.TryDmMember($"{context.Member.Mention} ({context.Member.Username}#{context.Member.Discriminator}) dropped strike #{strikeId}.\nReason: {Formatter.BlockCode(Formatter.Strip(reason))}");

                strike.VictimMessaged = sentDm;
                strike.Reasons.Add("Dropped: " + reason);
                strike.Changes.Add(DateTime.UtcNow);
                strike.Dropped = true;

                Dictionary<string, string> keyValuePairs = new();
                keyValuePairs.Add("guild_name", context.Guild.Name);
                keyValuePairs.Add("guild_count", Public.TotalMemberCount[context.Guild.Id].ToMetric());
                keyValuePairs.Add("guild_id", context.Guild.Id.ToString(CultureInfo.InvariantCulture));
                keyValuePairs.Add("victim_username", guildVictim.Username);
                keyValuePairs.Add("victim_tag", guildVictim.Discriminator);
                keyValuePairs.Add("victim_mention", guildVictim.Mention);
                keyValuePairs.Add("victim_id", guildVictim.Id.ToString(CultureInfo.InvariantCulture));
                keyValuePairs.Add("victim_displayname", guildVictim.DisplayName);
                keyValuePairs.Add("moderator_username", context.Member.Username);
                keyValuePairs.Add("moderator_tag", context.Member.Discriminator);
                keyValuePairs.Add("moderator_mention", context.Member.Mention);
                keyValuePairs.Add("moderator_id", context.Member.Id.ToString(CultureInfo.InvariantCulture));
                keyValuePairs.Add("moderator_displayname", context.Member.DisplayName);
                keyValuePairs.Add("punishment_reason", reason);
                keyValuePairs.Add("strike_id", strike.LogId.ToString(CultureInfo.InvariantCulture));
                await ModLog(context.Guild, keyValuePairs, CustomEvent.Drop, Database);

                await context.EditResponseAsync(new()
                {
                    Content = $"Strike #{strikeId} has been dropped, <@{strike.VictimId}> {(sentDm ? "has been notified" : "could not be messaged")}. Reason: {reason}"
                });
            }
        }
    }
}
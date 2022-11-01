using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Humanizer;
using Tomoe.Commands.Attributes;

namespace Tomoe.Commands.Moderation
{
    public sealed class KickCommand : ApplicationCommandModule
    {
        [SlashCommand("kick", "Kicks a member from the guild, sending them off with a dm."), Hierarchy(Permissions.KickMembers)]
        public static async Task KickAsync(InteractionContext context, [Option("victim", "Who to kick from the guild.")] DiscordUser victimUser, [Option("reason", "Why is the victim being kicked from the guild?")] string reason = Constants.MissingReason)
        {
            DiscordMember victimMember = await victimUser.Id.GetMemberAsync(context.Guild);
            if (victimMember == null)
            {
                await context.EditResponseAsync(new()
                {
                    Content = $"Error: {victimUser.Mention} is not in the guild!"
                });
                return;
            }

            bool sentDm = await victimUser.TryDmMemberAsync($"You've been kicked from {context.Guild.Name} by {context.Member.Mention} ({Formatter.InlineCode(context.Member.Id.ToString(CultureInfo.InvariantCulture))}). Reason: {reason}");
            await victimMember.RemoveAsync(reason);

            Dictionary<string, string> keyValuePairs = new()
            {
                { "guild_name", context.Guild.Name },
                { "guild_count", Program.TotalMemberCount[context.Guild.Id].ToMetric() },
                { "guild_id", context.Guild.Id.ToString(CultureInfo.InvariantCulture) },
                { "victim_username", victimMember.Username },
                { "victim_tag", victimMember.Discriminator },
                { "victim_mention", victimMember.Mention },
                { "victim_id", victimMember.Id.ToString(CultureInfo.InvariantCulture) },
                { "victim_displayname", victimMember.DisplayName },
                { "moderator_username", context.Member.Username },
                { "moderator_tag", context.Member.Discriminator },
                { "moderator_mention", context.Member.Mention },
                { "moderator_id", context.Member.Id.ToString(CultureInfo.InvariantCulture) },
                { "moderator_displayname", context.Member.DisplayName },
                { "punishment_reason", reason }
            };
            await ModLogCommand.ModLogAsync(context.Guild, keyValuePairs, DiscordEvent.Ban);

            await context.EditResponseAsync(new()
            {
                Content = $"{victimUser.Mention} has been kicked{(sentDm ? "" : "(failed to dm)")}. Reason: {reason}"
            });
        }
    }
}

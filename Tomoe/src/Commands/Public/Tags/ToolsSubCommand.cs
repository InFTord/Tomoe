using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Tomoe.Models;

namespace Tomoe.Commands.Common
{
    public sealed partial class TagCommand : ApplicationCommandModule
    {
        public Database Database { private get; set; } = null!;

        public async Task<Tag?> GetTagAsync(string tagName, ulong guildId)
        {
            tagName = tagName.ToLowerInvariant();
            Tag? tag = Database.Tags.FirstOrDefault(databaseTag => databaseTag.Name == tagName && databaseTag.GuildId == guildId);

            // Test if the tag is an alias
            if (tag is not null && tag.IsAlias)
            {
                // Retrieve the original tag
                tag = Database.Tags.FirstOrDefault(databaseTag => databaseTag.Name == tag.AliasTo && databaseTag.GuildId == guildId);
                // This shouldn't happen since Tags.Delete should also delete aliases, but it's here for safety.
                if (tag is not null)
                {
                    Database.Tags.Remove(tag);
                    await Database.SaveChangesAsync();
                    return null;
                }
            }
            return tag;
        }

        public async Task<bool> CanModifyTagAsync(Tag tag, ulong memberId, DiscordGuild guild)
        {
            if (tag.OwnerId == memberId)
            {
                return true;
            }

            if (guild.OwnerId == memberId)
            {
                return true;
            }

            // Tag creators should have permission over their tag's aliases.
            if (tag.IsAlias)
            {
                Tag? originalTag = Database.Tags.FirstOrDefault(databaseTag => databaseTag.AliasTo == tag.Name && databaseTag.GuildId == guild.Id);
                if (originalTag is null)
                {
                    return true;
                }
                else if (originalTag.OwnerId == memberId)
                {
                    return true;
                }
            }

            DiscordMember? discordMember = await memberId.GetMemberAsync(guild);
            if (discordMember is null)
            {
                return true;
            }
            else if (discordMember.Permissions.HasPermission(Permissions.ManageMessages))
            {
                return true;
            }

            GuildConfig guildConfig = Database.GuildConfigs.First(guildConfig => guildConfig.Id == guild.Id);
            return guildConfig.AdminRoles.Intersect(discordMember.Roles.Select(role => role.Id)).Any();
        }
    }
}
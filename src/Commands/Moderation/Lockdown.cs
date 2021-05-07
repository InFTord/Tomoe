namespace Tomoe.Commands.Moderation
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Tomoe.Db;
    using static Tomoe.Commands.Moderation.ModLogs;

    [Group("lockdown"), Description("Locks a channel, role or the entire server."), Aliases("lock_down", "lock"), RequireGuild, RequireUserPermissions(Permissions.ManageRoles | Permissions.ManageChannels), RequireBotPermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
    public class Lockdown : BaseCommandModule
    {

        public Database Database { private get; set; }

        public static async Task LockChannel(CommandContext context, DiscordChannel channel, List<DiscordRole> roles = null, Database database = null)
        {
            bool saveDatabase = database == null;
            if (database == null)
            {
                IServiceScope scope = Program.ServiceProvider.CreateScope();
                database = scope.ServiceProvider.GetService<Database>();
            }

            List<Lock> locks = new();
            if (roles == null)
            {
                roles = new();
                roles.AddRange(context.Guild.Roles.Values);
            }

            foreach (DiscordRole role in roles)
            {
                if (!role.HasPermission(Permissions.SendMessages) && !role.HasPermission(Permissions.AddReactions))
                {
                    continue;
                }

                DiscordOverwrite discordOverwrite = channel.PermissionOverwrites.FirstOrDefault(overwrite => overwrite.Id == role.Id);
                Permissions channelPermissions = channel.Type switch
                {
                    ChannelType.Category => Permissions.SendMessages | Permissions.AddReactions | Permissions.UseVoice,
                    ChannelType.Voice => Permissions.UseVoice,
                    ChannelType.Text => Permissions.SendMessages | Permissions.AddReactions,
                    _ => Permissions.SendMessages | Permissions.AddReactions | Permissions.UseVoice
                };

                Lock lockRole = new();
                lockRole.GuildId = context.Guild.Id;
                lockRole.ChannelId = channel.Id;
                lockRole.RoleId = role.Id;
                if (discordOverwrite == null)
                {
                    lockRole.HadPreviousOverwrite = false;
                }
                else
                {
                    lockRole.HadPreviousOverwrite = true;
                    lockRole.Allowed = discordOverwrite.Allowed;
                    lockRole.Denied = discordOverwrite.Denied;
                }

                locks.Add(lockRole);
                await channel.AddOverwriteAsync(role, discordOverwrite.Allowed, discordOverwrite.Denied.Grant(channelPermissions), "Channel lockdown");
            }

            database.Locks.AddRange(locks);
            if (saveDatabase)
            {
                _ = await database.SaveChangesAsync();
                await database.DisposeAsync();
            }
        }

        [Command("channel")]
        public async Task Channel(CommandContext context, DiscordChannel channel, [RemainingText] string lockReason = Constants.MissingReason)
        {
            await LockChannel(context, channel, null, Database);
            await Record(context.Guild, LogType.LockChannel, Database, $"{context.User.Mention} locked channel {channel.Mention}. Reason: {lockReason}");
            _ = await Database.SaveChangesAsync();
            _ = await Program.SendMessage(context, $"Channel successfully locked. All roles below me cannot send messages or react. To undo this, run `>>unlock channel`");
        }

        [Command("channel")]
        public async Task Channel(CommandContext context, [RemainingText] string lockReason = Constants.MissingReason) => await Channel(context, context.Channel, lockReason);

        [Command("server")]
        public async Task Server(CommandContext context, [RemainingText] string lockReason = Constants.MissingReason)
        {
            foreach (DiscordChannel channel in context.Guild.Channels.Values.OrderBy(channel => channel.Type))
            {
                await LockChannel(context, channel, null, Database);
            }

            await Record(context.Guild, LogType.LockServer, Database, $"{context.User.Mention} locked the server. Reason: {lockReason}");
            _ = await Database.SaveChangesAsync();
            _ = await Program.SendMessage(context, $"Server successfully locked. All roles below me cannot send messages or react. To undo this, run `>>unlock server`");
        }

        [Command("role")]
        public async Task Role(CommandContext context, DiscordRole role, DiscordChannel channel = null, [RemainingText] string lockReason = Constants.MissingReason)
        {
            if (channel == null)
            {
                foreach (DiscordChannel guildChannel in context.Guild.Channels.Values.OrderBy(channel => channel.Type))
                {
                    await LockChannel(context, guildChannel, new() { role }, Database);
                }
                await Record(context.Guild, LogType.LockRole, Database, $"{context.User.Mention} locked the role {role.Mention} across the server. Reason: {lockReason}");
                _ = await Database.SaveChangesAsync();
                _ = await Program.SendMessage(context, $"The role {role.Mention} cannot send messages or react in the server. To undo this, run `>>unlock role {role.Mention}`");
            }
            else
            {
                await LockChannel(context, channel, new() { role }, Database);
                await Record(context.Guild, LogType.LockRole, Database, $"{context.User.Mention} locked the role {role.Mention} in channel {channel.Mention}. Reason: {lockReason}");
                _ = await Database.SaveChangesAsync();
                _ = await Program.SendMessage(context, $"Role successfully locked channel {channel.Mention}. The role {role.Mention} cannot send messages or react. To undo this, run `>>unlock role #{channel.Name}`");
            }
        }

        [Command("bots")]
        public async Task Bots(CommandContext context, DiscordChannel channel = null, [RemainingText] string lockReason = Constants.MissingReason)
        {
            List<DiscordRole> roles = context.Guild.Roles.Values.Where(role => role.IsManaged).ToList();
            if (channel == null)
            {
                foreach (DiscordRole role in roles)
                {
                    foreach (DiscordChannel guildChannel in context.Guild.Channels.Values.OrderBy(channel => channel.Type))
                    {
                        await LockChannel(context, guildChannel, new() { role }, Database);
                    }
                    await Record(context.Guild, LogType.LockBots, Database, $"{context.User.Mention} locked the bots across the server. Reason: {lockReason}");
                    _ = await Database.SaveChangesAsync();
                    _ = await Program.SendMessage(context, $"The bots cannot send messages or react in the server. To undo this, run `>>unlock bots`");
                }
            }
            else
            {
                foreach (DiscordRole role in roles)
                {
                    await LockChannel(context, channel, new() { role }, Database);
                }
                await Record(context.Guild, LogType.LockBots, Database, $"{context.User.Mention} locked bots from the channel {channel.Mention}. Reason: {lockReason}");
                _ = await Program.SendMessage(context, $"Bots are successfully locked in channel {channel.Mention}. Bots cannot send messages or react. To undo this, run `>>unlock bots #{channel.Name}`");
            }
        }
    }
}
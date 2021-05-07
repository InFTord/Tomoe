namespace Tomoe.Commands.Moderation
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;
    using Tomoe.Commands.Moderation.Attributes;
    using Tomoe.Db;
    using static Tomoe.Commands.Moderation.Config;
    using static Tomoe.Commands.Moderation.ModLogs;

    public class MuteMemeBan : BaseCommandModule
    {
        public Database Database { private get; set; }

        [Command("antimeme"), RequireGuild, RequireBotPermissions(Permissions.ManageRoles), RequireUserPermissions(Permissions.ManageMessages), Description("Grants the victim the `Antimeme` role, which prevents them from reacting to messages, embedding links, uploading files, streaming and forces push-to-talk. The intention of this role is to prevent abuse of Discord's rich messaging features, or when someone is being really annoying by conversating with every known method except through words."), Punishment(false)]
        public async Task Antimeme(CommandContext context, [Description("Who's being antimeme'd.")] DiscordUser victim, [Description("Why is the victim being antimeme'd."), RemainingText] string antimemeReason = Constants.MissingReason) => await Punish(context, victim, antimemeReason, RoleAction.Antimeme, (await Database.GuildConfigs.FirstAsync(guildConfig => guildConfig.Id == context.Guild.Id)).AntimemeRole.GetRole(context.Guild), $"You've been antimemed from {Formatter.Bold(context.Guild.Name)}. Reason: {Formatter.BlockCode(Formatter.Strip(antimemeReason))}Context: {context.Message.JumpLink}\nNote: Antimeme prevents you from reacting to messages, sending embeds, uploading files, streaming to voice channels, and adds the push-to-talk restriction to voice channels.");

        [Command("mute"), RequireGuild, RequireBotPermissions(Permissions.ManageRoles), RequireUserPermissions(Permissions.ManageMessages), Aliases("silence", "shut", "zip"), Description("Grants the victim the `Muted` role, which prevents them from sending messages, reacting to messages and speaking in voice channels."), Punishment(false)]
        public async Task Mute(CommandContext context, [Description("Who's being muted.")] DiscordUser victim, [Description("Why is the victim being muted."), RemainingText] string muteReason = Constants.MissingReason) => await Punish(context, victim, muteReason, RoleAction.Mute, (await Database.GuildConfigs.FirstAsync(guildConfig => guildConfig.Id == context.Guild.Id)).MuteRole.GetRole(context.Guild), $"You've been muted from {Formatter.Bold(context.Guild.Name)}. Reason: {Formatter.BlockCode(Formatter.Strip(muteReason))}Context: {context.Message.JumpLink}\nNote: A mute prevents you from having any sort of interaction with the guild. It makes the entire guild readonly.");

        [Command("voiceban"), RequireGuild, RequireBotPermissions(Permissions.ManageRoles), RequireUserPermissions(Permissions.MoveMembers), Aliases("vb"), Description("Grants the victim the `Voicebanned` role, which prevents them from connecting to voice channels."), Punishment(false)]
        public async Task Voiceban(CommandContext context, [Description("Who's being voicebanned.")] DiscordUser victim, [Description("Why is the victim being voicebanned."), RemainingText] string voicebanReason = Constants.MissingReason) => await Punish(context, victim, voicebanReason, RoleAction.Voiceban, (await Database.GuildConfigs.FirstAsync(guildConfig => guildConfig.Id == context.Guild.Id)).VoicebanRole.GetRole(context.Guild), $"You've been voicebanned from {Formatter.Bold(context.Guild.Name)}. Reason: {Formatter.BlockCode(Formatter.Strip(voicebanReason))}Context: {context.Message.JumpLink}\nNote: A voiceban prevents you from connecting to voice channels.");

        public async Task Punish(CommandContext context, DiscordUser victim, string punishReason, RoleAction roleAction, DiscordRole discordRole, string dmMessage)
        {
            if (discordRole == null)
            {
                _ = await Program.SendMessage(context, Constants.MissingRole);
                return;
            }

            string rolename = roleAction switch
            {
                RoleAction.Mute => "muted",
                RoleAction.Antimeme => "antimemed",
                RoleAction.Voiceban => "voicebanned",
                _ => "punished"
            };

            bool sentDm;
            try
            {
                sentDm = await ByProgram(context.Guild, victim, context.User, punishReason, roleAction, discordRole, dmMessage);
            }
            catch (ArgumentException error)
            {
                _ = await Program.SendMessage(context, error.Message);
                return;
            }
            _ = await Program.SendMessage(context, $"{victim.Mention} has been {rolename}{(sentDm ? '.' : " (Failed to dm).")} Reason: ```\n{punishReason}```");
        }

        public static async Task<bool> ByProgram(DiscordGuild discordGuild, DiscordUser victim, DiscordUser issuer, string punishReason, RoleAction roleAction, DiscordRole discordRole, string dmMessage)
        {
            DiscordMember discordVictim = await victim.Id.GetMember(discordGuild);
            bool sentDm = false;
            if (discordVictim != null)
            {
                await discordVictim.GrantRoleAsync(discordRole, punishReason);
                sentDm = await discordVictim.TryDmMember(dmMessage);
            }

            using IServiceScope scope = Program.ServiceProvider.CreateScope();
            Database database = scope.ServiceProvider.GetService<Database>();
            GuildConfig guildConfig = await database.GuildConfigs.FirstOrDefaultAsync(guildConfig => guildConfig.Id == discordGuild.Id) ?? new GuildConfig(discordGuild.Id);
            GuildUser databaseVictim = await database.GuildUsers.FirstOrDefaultAsync(guildUser => guildUser.UserId == victim.Id && guildUser.GuildId == discordGuild.Id) ?? new GuildUser(victim.Id);

            switch (roleAction)
            {
                case RoleAction.Antimeme:
                    databaseVictim.IsAntimemed = databaseVictim.IsAntimemed ? throw new ArgumentException(Formatter.Bold("[Error]: User is already antimemed.")) : true;
                    break;
                case RoleAction.Mute:
                    databaseVictim.IsMuted = databaseVictim.IsMuted ? throw new ArgumentException(Formatter.Bold("[Error]: User is already muted.")) : true;
                    break;
                case RoleAction.Voiceban:
                    databaseVictim.IsVoicebanned = databaseVictim.IsVoicebanned ? throw new ArgumentException(Formatter.Bold("[Error]: User is already voicebanned.")) : true;
                    break;
                default:
                    throw new ArgumentException("Unable to determine which punishment to set.");
            }

            string rolename = roleAction switch
            {
                RoleAction.Mute => "muted",
                RoleAction.Antimeme => "antimemed",
                RoleAction.Voiceban => "voicebanned",
                _ => "punished"
            };

            LogType logType = roleAction switch
            {
                RoleAction.Mute => LogType.Mute,
                RoleAction.Antimeme => LogType.Antimeme,
                RoleAction.Voiceban => LogType.Voiceban,
                _ => LogType.Unknown
            };

            await Record(discordGuild, logType, database, $"{issuer.Mention} {rolename} {victim.Mention}{(sentDm ? '.' : " (Failed to dm).")} Reason: {punishReason}");
            _ = await database.SaveChangesAsync();
            return sentDm;
        }
    }
}
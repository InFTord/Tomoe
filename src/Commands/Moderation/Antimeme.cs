using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tomoe.Commands.Moderation.Attributes;
using Tomoe.Db;

namespace Tomoe.Commands.Moderation
{
	public class Antimeme : BaseCommandModule
	{
		public Database Database { private get; set; }
		[Command("antimeme"), RequireGuild, RequireBotPermissions(Permissions.ManageRoles), RequireUserPermissions(Permissions.ManageMessages), Aliases("anti_meme", "memeban", "meme_ban", "nomeme", "no_meme"), Description("Grants the victim the `Antimeme` role, which prevents them from reacting to messages, embedding links and uploading files. The voice channels, this prevents the victim from streaming and they must use push-to-talk. The intention of this role is to prevent abuse of Discord's rich messaging features, or when someone is being really annoying by conversating with every known method except with messages."), Punishment(false)]
		public async Task Byuser(CommandContext context, DiscordUser victim, [RemainingText] string antimemeReason = Constants.MissingReason)
		{
			// Test if the guild is in the database. Bot owner might've removed it on accident, and we don't want the bot to fail completely if the guild is missing.
			Guild guild = await Database.Guilds.FirstOrDefaultAsync(guild => guild.Id == context.Guild.Id);
			if (guild == null)
			{
				_ = await Program.SendMessage(context, Constants.GuildNotInDatabase);
				return;
			}

			// GetRole is used in case the role id is 0 (default value) and will either return the Discord role or null
			DiscordRole antimemeRole = guild.AntimemeRole.GetRole(context.Guild);
			if (antimemeRole == null)
			{
				_ = await Program.SendMessage(context, Constants.MissingRole);
				return;
			}

			DiscordMember guildVictim = await victim.Id.GetMember(context.Guild);

			// Get databaseVictim or create it if they don't exist
			GuildUser databaseVictim = guild.Users.FirstOrDefault(user => user.Id == victim.Id);
			if (databaseVictim == null)
			{
				databaseVictim = new(victim.Id);
				if (guildVictim != null)
				{
					databaseVictim.Roles = guildVictim.Roles.Except(new[] { context.Guild.EveryoneRole }).Select(role => role.Id).ToList();
				}
			}
			databaseVictim.IsAntimemed = true;

			// If the user is in the guild, assign the antimeme role
			bool sentDm = false;
			if (guildVictim != null)
			{
				await guildVictim.GrantRoleAsync(antimemeRole, antimemeReason);
				// If the user isn't a bot, attempt to dm them to make them aware of their punishment
				if (!guildVictim.IsBot)
				{
					try
					{
						_ = await guildVictim.SendMessageAsync($"You've been antimemed from {Formatter.Bold(context.Guild.Name)}. Reason: {Formatter.BlockCode(Formatter.Strip(antimemeReason))}Context: {context.Message.JumpLink}\nNote: Antimeme prevents you from reacting to messages, sending embeds, uploading files, streaming to voice channels, and adds the push-to-talk restriction to voice channels.");
						sentDm = true;
					}
					catch (Exception) { }
				}
			}

			if (guild.ProgressiveStrikes)
			{
				Strike strike = new();
				strike.GuildId = context.Guild.Id;
				strike.IssuerId = context.User.Id;
				strike.JumpLinks.Add(context.Message.JumpLink);
				strike.Reasons.Add(antimemeReason);
				strike.VictimId = victim.Id;
				strike.VictimMessaged = sentDm;
				_ = Database.Strikes.Add(strike);
				_ = await Database.SaveChangesAsync();
				await Strikes.ProgressiveStrike(context.Guild, victim, strike);
			}

			_ = await Program.SendMessage(context, $"{victim.Mention} has been antimemed{(sentDm ? '.' : " (Failed to dm).")}");
		}

		public static async Task ByProgram(DiscordGuild discordGuild, DiscordUser victim, Uri jumplink, string antimemeReason = Constants.MissingPermissions)
		{
			using IServiceScope scope = Program.ServiceProvider.CreateScope();
			Database database = scope.ServiceProvider.GetService<Database>();
			// Test if the guild is in the database. Bot owner might've removed it on accident, and we don't want the bot to fail completely if the guild is missing.
			Guild guild = await database.Guilds.FirstOrDefaultAsync(guild => guild.Id == discordGuild.Id);
			if (guild == null) return;

			// GetRole is used in case the role id is 0 (default value) and will either return the Discord role or null
			DiscordRole antimemeRole = guild.AntimemeRole.GetRole(discordGuild);
			if (antimemeRole == null) return;

			DiscordMember guildVictim = await victim.Id.GetMember(discordGuild);

			// Get databaseVictim or create it if they don't exist
			GuildUser databaseVictim = guild.Users.FirstOrDefault(user => user.Id == victim.Id);
			if (databaseVictim == null)
			{
				databaseVictim = new(victim.Id);
				if (guildVictim != null)
				{
					databaseVictim.Roles = guildVictim.Roles.Except(new[] { discordGuild.EveryoneRole }).Select(role => role.Id).ToList();
				}
			}
			databaseVictim.IsVoicebanned = true;

			// If the user is in the guild, assign the muted role
			bool sentDm = false;
			if (guildVictim != null)
			{
				await guildVictim.GrantRoleAsync(antimemeRole, antimemeReason);
				// If the user isn't a bot, attempt to dm them to make them aware of their punishment
				if (!guildVictim.IsBot)
				{
					try
					{
						_ = await guildVictim.SendMessageAsync($"You've been antimemed from {Formatter.Bold(discordGuild.Name)}. Reason: {Formatter.BlockCode(Formatter.Strip(antimemeReason))}Context: {jumplink}\nNote: Antimeme prevents you from reacting to messages, sending embeds, uploading files, streaming to voice channels, and adds the push-to-talk restriction to voice channels.");
						sentDm = true;
					}
					catch (Exception) { }
				}
			}

			if (guild.ProgressiveStrikes)
			{
				Strike strike = new();
				strike.GuildId = discordGuild.Id;
				strike.IssuerId = Program.Client.CurrentUser.Id;
				strike.JumpLinks.Add(jumplink);
				strike.Reasons.Add(antimemeReason);
				strike.VictimId = victim.Id;
				strike.VictimMessaged = sentDm;
				_ = database.Strikes.Add(strike);
				_ = await database.SaveChangesAsync();
				await Strikes.ProgressiveStrike(discordGuild, victim, strike);
			}
		}
	}
}

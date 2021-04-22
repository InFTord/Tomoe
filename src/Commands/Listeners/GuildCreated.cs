using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tomoe.Db;

namespace Tomoe.Commands.Listeners
{
	public class GuildCreated
	{
		/// <summary>
		/// Adds the guild to the database, or fixes channel permissions for the punishment roles.
		/// </summary>
		/// <param name="_client">Unused <see cref="DiscordClient"/>.</param>
		/// <param name="eventArgs">Used to get the guild id, punishment roles and to fix channel permissions.</param>
		public static async Task Handler(DiscordClient _client, GuildCreateEventArgs eventArgs)
		{
			using IServiceScope scope = Program.ServiceProvider.CreateScope();
			Database database = scope.ServiceProvider.GetService<Database>();
			GuildConfig guildConfig = await database.GuildConfigs.FirstOrDefaultAsync(guild => guild.Id == eventArgs.Guild.Id);
			if (guildConfig == null)
			{
				guildConfig = new(eventArgs.Guild.Id);
				_ = database.GuildConfigs.Add(guildConfig);
				_ = await database.SaveChangesAsync();
			}
			else
			{
				DiscordRole muteRole = guildConfig.MuteRole.GetRole(eventArgs.Guild);
				DiscordRole antimemeRole = guildConfig.AntimemeRole.GetRole(eventArgs.Guild);
				DiscordRole voicebanRole = guildConfig.VoicebanRole.GetRole(eventArgs.Guild);
				if (muteRole != null) Moderation.Config.FixPermissions(eventArgs.Guild, Moderation.Config.RoleAction.Mute, muteRole);
				if (antimemeRole != null) Moderation.Config.FixPermissions(eventArgs.Guild, Moderation.Config.RoleAction.Antimeme, antimemeRole);
				if (voicebanRole != null) Moderation.Config.FixPermissions(eventArgs.Guild, Moderation.Config.RoleAction.Voiceban, voicebanRole);
			}

			// Find new users by removing the database's current user list's through id's
			List<ulong> newGuildIds = eventArgs.Guild.Members.Keys.Except(database.GuildUsers.Select(guildUser => guildUser.GuildId == eventArgs.Guild.Id ? guildUser.UserId : 0)).ToList();
			List<GuildUser> updatedGuildUsers = new();
			foreach (ulong memberId in newGuildIds)
			{
				DiscordMember member = eventArgs.Guild.Members[memberId];
				GuildUser guildUser = new(memberId);
				guildUser.Roles = member.Roles.Except(new[] { eventArgs.Guild.EveryoneRole }).Select(role => role.Id).ToList();
				updatedGuildUsers.Add(guildUser);
			}
			// If the updated guild user count isn't zero, save the new users to the database. An exception will be thrown if nothing new is added.
			if (updatedGuildUsers.Count != 0)
			{
				database.GuildUsers.AddRange(updatedGuildUsers);
				_ = await database.SaveChangesAsync();
			}
		}
	}
}

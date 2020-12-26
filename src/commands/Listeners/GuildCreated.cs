using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Tomoe.Commands.Listeners
{
	public class GuildCreated
	{
		public static async Task Handler(DiscordClient _, GuildCreateEventArgs eventArgs)
		{
			if (Program.Database.Guild.GuildIdExists(eventArgs.Guild.Id))
			{
				ulong? muteRoleId = Program.Database.Guild.MuteRole(eventArgs.Guild.Id);
				if (muteRoleId.HasValue)
				{
					DiscordRole muteRole = eventArgs.Guild.GetRole(muteRoleId.Value);
					if (muteRole != null)
					{
						await Config.Mute.FixMuteRolePermissions(eventArgs.Guild, muteRole);
					}
				}
			}
			else
			{
				Program.Database.Guild.InsertGuildId(eventArgs.Guild.Id);
			}
		}
	}
}

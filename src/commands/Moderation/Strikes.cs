using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Tomoe.Commands.Moderation.Attributes;
using Tomoe.Database.Interfaces;

namespace Tomoe.Commands.Moderation
{
	[Group("strike"), Description("Gives a strike/warning to the specified victim."), RequireUserPermissions(Permissions.KickMembers), Aliases("warn"), Punishment(true)]
	public class Strikes : BaseCommandModule
	{
		[GroupCommand]
		public async Task Add(CommandContext context, DiscordUser victim, [Description("(Optional) Should prompt to confirm with the self strike")] bool confirmed = false, [RemainingText] string strikeReason = Program.MissingReason)
		{
			bool sentDm = false;
			DiscordMember guildVictim = await context.Guild.GetMemberAsync(victim.Id);

			if (guildVictim != null && !guildVictim.IsBot) try
				{
					await guildVictim.SendMessageAsync($"You've been given a strike by **{context.User.Mention}** from **{context.Guild.Name}**. Reason: ```\n{strikeReason ?? Program.MissingReason}\n```");
					sentDm = true;
				}
				catch (UnauthorizedException) { }
			Strike strike = Program.Database.Strikes.Add(context.Guild.Id, victim.Id, context.User.Id, strikeReason, context.Message.JumpLink.ToString(), sentDm).Value;
			_ = Program.SendMessage(context, $"Case #{strike.Id}, {victim.Mention} has been striked{(sentDm ? '.' : " (Failed to DM).")} This is strike #{strike.StrikeCount}. Reason: ```\n{strikeReason ?? Program.MissingReason}\n```", null, new UserMention(victim.Id));
		}

		[Command("check"), Description("Gets the users past history"), RequireUserPermissions(Permissions.KickMembers), Aliases("history")]
		public async Task Check(CommandContext context, DiscordUser victim)
		{
			DiscordEmbedBuilder embedBuilder = new();
			embedBuilder.Title = $"{victim.Username}'s Past History";
			Strike[] pastStrikes = Program.Database.Strikes.GetVictim(context.Guild.Id, victim.Id);
			if (pastStrikes == null) _ = Program.SendMessage(context, "No previous strikes have been found!");
			else
			{
				foreach (Strike strike in Program.Database.Strikes.GetVictim(context.Guild.Id, victim.Id)) embedBuilder.Description += $"Case #{strike.Id} [on {strike.CreatedAt.ToString("MMM' 'dd', 'yyyy' 'HH':'mm':'ss")}, Issued by {(await context.Client.GetUserAsync(strike.IssuerId)).Mention}]({strike.JumpLink}) {(strike.Dropped ? "(Dropped)" : null)}\n";
				_ = Program.SendMessage(context, null, embedBuilder.Build());
			}
		}
	}
}

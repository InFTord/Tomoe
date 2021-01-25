using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Tomoe.Database.Interfaces;
using Tomoe.Types;

namespace Tomoe.Commands.Moderation
{
	[Group("strike"), Description("Gives a strike/warning to the specified victim."), RequireUserPermissions(Permissions.KickMembers), Aliases("warn")]
	public class Strikes : BaseCommandModule
	{
		[GroupCommand]
		public async Task Add(CommandContext context, DiscordUser victim, [Description("(Optional) Should prompt to confirm with the self strike")] bool confirmed = false, [RemainingText] string strikeReason = Program.MissingReason)
		{
			if (victim == context.Guild.CurrentMember)
			{
				_ = Program.SendMessage(context, Program.SelfAction);
				return;
			}
			else if (victim == context.User && !confirmed)
			{
				DiscordMessage discordMessage = Program.SendMessage(context, "**[Notice: You're about to kick yourself. Are you sure about this?]**");
				_ = new Queue(discordMessage, context.User, new(async eventArgs =>
				{
					if (eventArgs.Emoji == Queue.ThumbsUp) await Add(context, context.User, true, strikeReason);
					else if (eventArgs.Emoji == Queue.ThumbsDown) _ = Program.SendMessage(context, "Aborting...");
				}));
				return;
			}

			bool sentDm = true;

			try
			{
				DiscordMember guildVictim = await context.Guild.GetMemberAsync(victim.Id);
				if (guildVictim.Hierarchy > (await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)).Hierarchy || guildVictim.Hierarchy >= context.Member.Hierarchy)
				{
					_ = Program.SendMessage(context, Program.Hierarchy);
					return;
				}
				else if (!guildVictim.IsBot) _ = await guildVictim.SendMessageAsync($"You've been given a strike by **{context.User.Mention}** from **{context.Guild.Name}**. Reason: ```\n{strikeReason.Filter() ?? Program.MissingReason}\n```");

			}
			catch (NotFoundException)
			{
				sentDm = false;
			}
			catch (BadRequestException)
			{
				sentDm = false;
			}
			catch (UnauthorizedException)
			{
				sentDm = false;
			}
			Strike strike = Program.Database.Strikes.Add(context.Guild.Id, victim.Id, context.User.Id, strikeReason, context.Message.JumpLink.ToString(), sentDm).Value;
			_ = Program.SendMessage(context, $"Case #{strike.Id}, {victim.Mention} has been striked{(sentDm ? '.' : " (Failed to DM).")} This is strike #{strike.StrikeCount}. Reason: ```\n{strikeReason.Filter(ExtensionMethods.FilteringAction.CodeBlocksZeroWidthSpace) ?? Program.MissingReason}\n```", null, new UserMention(victim.Id));
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

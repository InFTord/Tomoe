using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Tomoe.Types;

namespace Tomoe.Commands.Moderation
{
	public class Ban : BaseCommandModule
	{
		[Command("ban"), Description("Bans people from the guild, sending them off with a private message."), RequireGuild, RequireUserPermissions(Permissions.BanMembers), RequireBotPermissions(Permissions.BanMembers)]
		public async Task BanUser(CommandContext context, [Description("The person to be banned.")] DiscordUser victim, [Description("(Optional) Removed the victim's messages from the pass `x` days.")] int pruneDays = 7, [Description("(Optional) Should prompt to confirm with the self ban")] bool confirmed = false, [Description("(Optional) The reason why the person is being banned."), RemainingText] string banReason = Program.MissingReason)
		{
			if (victim == context.Client.CurrentUser)
			{
				_ = Program.SendMessage(context, Program.SelfAction);
				return;
			}
			else if (victim == context.User && !confirmed)
			{
				DiscordMessage discordMessage = Program.SendMessage(context, "**[Notice: You're about to ban yourself. Are you sure about this?]**");
				_ = new Queue(discordMessage, context.User, new(async eventArgs =>
				{
					if (eventArgs.Emoji == Queue.ThumbsUp) await BanUser(context, context.User, pruneDays, true, banReason);
					else if (eventArgs.Emoji == Queue.ThumbsDown) _ = Program.SendMessage(context, "Aborting...");
				}));
				return;
			}

			if (pruneDays < 7) pruneDays = 7;
			bool sentDm = true;
			try
			{
				DiscordMember guildVictim = await context.Guild.GetMemberAsync(victim.Id);
				if (guildVictim.Hierarchy > (await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)).Hierarchy || guildVictim.Hierarchy >= context.Member.Hierarchy)
				{
					_ = Program.SendMessage(context, Program.Hierarchy);
					return;
				}
				else if (!guildVictim.IsBot) _ = await guildVictim.SendMessageAsync($"You've been banned by **{context.User.Mention}** from **{context.Guild.Name}**. Reason: ```\n{banReason.Filter() ?? Program.MissingReason}\n```");
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
			await context.Guild.BanMemberAsync(victim.Id, pruneDays, banReason ?? Program.MissingReason);
			_ = Program.SendMessage(context, $"{victim.Mention} has been permanently banned{(sentDm ? '.' : " (Failed to DM).")} Reason: ```\n{banReason.Filter(ExtensionMethods.FilteringAction.CodeBlocksZeroWidthSpace) ?? Program.MissingReason}```\n", null, new UserMention(victim.Id));
		}

		[Command("ban"), RequireGuild]
		public async Task BanUser(CommandContext context, [Description("The person to be banned.")] DiscordUser victim, [Description("(Optional) The reason why the person is being banned."), RemainingText] string banReason) => BanUser(context, victim, default, default, banReason);

		[Command("ban"), RequireGuild]
		public async Task BanUser(CommandContext context, [Description("The person to be banned.")] DiscordUser victim, [Description("(Optional) Removed the victim's messages from the pass `x` days.")] int pruneDays = 7) => BanUser(context, victim, pruneDays, default, Program.MissingReason);

		[Command("ban"), RequireGuild]
		public async Task BanUsers(CommandContext context, [Description("(Optional) Removed the victim's messages from the pass `x` days.")] int pruneDays = 7, [Description("(Optional) The reason why the people are being banned.")] string banReason = Program.MissingReason, [Description("The people to be banned.")] params DiscordUser[] victims)
		{
			if (pruneDays < 7) pruneDays = 7;
			List<IMention> mentions = new();
			foreach (DiscordUser victim in victims)
			{
				if (victim == context.Client.CurrentUser)
				{
					_ = Program.SendMessage(context, Program.SelfAction);
					return;
				}

				try
				{
					DiscordMember guildVictim = await context.Guild.GetMemberAsync(victim.Id);
					if (guildVictim.Hierarchy > (await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)).Hierarchy || guildVictim.Hierarchy >= context.Member.Hierarchy)
					{
						_ = Program.SendMessage(context, Program.Hierarchy);
						return;
					}
					else if (!victim.IsBot) _ = await guildVictim.SendMessageAsync($"You've been banned by **{context.User.Mention}** from **{context.Guild.Name}**. Reason: ```\n{banReason.Filter() ?? Program.MissingReason}\n```");
				}
				catch (NotFoundException) { }
				catch (BadRequestException) { }
				catch (UnauthorizedException) { }
				await context.Guild.BanMemberAsync(victim.Id, pruneDays, banReason ?? Program.MissingReason);
				mentions.Add(new UserMention(victim.Id));
			}
			_ = Program.SendMessage(context, $"Successfully massbanned {string.Join<DiscordUser>(", ", victims)}. Reason: ```\n{banReason.Filter()}\n```", null, mentions.ToArray());
		}

		[Command("ban"), RequireGuild]
		public async Task BanUsers(CommandContext context, [Description("(Optional) The reason why the people are being banned.")] string banReason = Program.MissingReason, [Description("The people to be banned.")] params DiscordUser[] victims) => BanUsers(context, default, banReason, victims);

		[Command("ban"), RequireGuild]
		public async Task BanUsers(CommandContext context, [Description("The people to be banned.")] params DiscordUser[] victims) => BanUsers(context, default, default, victims);
	}
}

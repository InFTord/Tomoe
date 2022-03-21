using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Emzi0767.Utilities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tomoe.Models;
using Tomoe.Utils;

namespace Tomoe.Commands.Common
{
    public class Poll : BaseCommandModule
    {
        public DatabaseList<PollModel, Guid> PollModelList { private get; init; } = null!;

        [Command("poll")]
        [Description("Creates a public message for people to vote on.")]
        public async Task PollAsync(CommandContext context, [Description("The question for people to vote on. Use quotes `\"Like this\"` to use multiple words.")] string question, [Description("When to end the poll.")] TimeSpan timeout, [Description("Which channel to post the poll in. Defaults to the current channel.")] DiscordChannel? channel = null, [Description("A list of options to choose from."), RemainingText] params string[] wordAndEmojiList)
        {
            channel ??= context.Channel;
            if (wordAndEmojiList.Length < 2)
            {
                await context.RespondAsync("You need to provide at least two options to vote on.");
                return;
            }
            else if (wordAndEmojiList.Length > 24)
            {
                await context.RespondAsync("You can only provide up to 24 options to vote on.");
                return;
            }
            else if (wordAndEmojiList.Distinct().Count() != wordAndEmojiList.Length)
            {
                await context.RespondAsync("There may not be any duplicate choices.");
                return;
            }
            else if (!channel.PermissionsFor(context.Member).HasPermission(Permissions.AccessChannels))
            {
                // Treat it as CNext can't find the channel; silently fail.
                return;
            }
            else if (!channel.PermissionsFor(context.Member).HasPermission(Permissions.SendMessages))
            {
                await context.RespondAsync($"[Error]: You cannot create a poll in {channel.Mention} since you're missing the {Permissions.SendMessages.Humanize()} permission.");
                return;
            }
            else if (!channel.PermissionsFor(context.Guild.CurrentMember).HasPermission(Permissions.AccessChannels))
            {
                await context.RespondAsync($"[Error]: I cannot create a poll in {channel.Mention} since I'm missing the {Permissions.AccessChannels.Humanize()} permission.");
                return;
            }
            else if (!channel.PermissionsFor(context.Guild.CurrentMember).HasPermission(Permissions.SendMessages))
            {
                await context.RespondAsync($"[Error]: I cannot create a poll in {channel.Mention} since I'm missing the {Permissions.SendMessages.Humanize()} permission.");
                return;
            }

            PollModel poll = new()
            {
                GuildId = context.Guild.Id,
                ChannelId = channel.Id,
                UserId = context.User.Id,
                Question = question,
                ExpiresAt = DateTime.UtcNow.Add(timeout),
                Votes = new Dictionary<string, ulong[]>(wordAndEmojiList.Select(x => new KeyValuePair<string, ulong[]>(x, Array.Empty<ulong>())))
            };
            PollModelList.Add(poll);

            List<DiscordActionRowComponent> actionRowComponents = new();
            List<DiscordButtonComponent> buttons = new();
            IArgumentConverter<DiscordEmoji> emojiArgumentConverter = new DiscordEmojiConverter();
            foreach (string wordOrEmoji in wordAndEmojiList)
            {
                if (buttons.Count == 5)
                {
                    actionRowComponents.Add(new DiscordActionRowComponent(buttons));
                    buttons.Clear();
                }

                string[] splitList = wordOrEmoji.Split(" ");
                DiscordButtonComponent button;
                button = !wordOrEmoji.StartsWith(" ", true, CultureInfo.InvariantCulture)
                    && (await emojiArgumentConverter.ConvertAsync(splitList[0], context)).IsDefined(out DiscordEmoji? emoji)
                    && emoji != null
                        ? (new(ButtonStyle.Secondary, $"poll\v{poll.Id}\v{wordOrEmoji}", string.Join(' ', splitList.Skip(1)), false, new DiscordComponentEmoji(emoji)))
                        : (new(ButtonStyle.Secondary, $"poll\v{poll.Id}\v{wordOrEmoji}", wordOrEmoji));
                buttons.Add(button);
            }
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Danger, $"poll\v{poll.Id}\v\tcancel", "Remove my vote!"));
            actionRowComponents.Add(new DiscordActionRowComponent(buttons));
            buttons.Clear();

            DiscordMessageBuilder builder = new();
            builder.Content = $"{question}\nThe poll ends {Formatter.Timestamp(timeout)}";
            builder.AddComponents(actionRowComponents);

            DiscordMessage message;
            if (channel == context.Channel)
            {
                message = await context.RespondAsync(builder);
            }
            else
            {
                message = await channel.SendMessageAsync(builder);

                DiscordMessageBuilder responseBuilder = new();
                responseBuilder.Content = "Poll created!";
                responseBuilder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"poll\v{poll.Id}\v\tview", "View poll!", false));
                await context.RespondAsync(responseBuilder);
            }

            poll.MessageId = message.Id;
            PollModelList.Update(poll);
        }

        public static async Task VoteExpired(object? sender, PollModel pollModel)
        {
            if (!Program.BotReady)
            {
                return;
            }

            List<KeyValuePair<string, ulong[]>> votes = pollModel.Votes.ToList();
            votes.Sort((x, y) => y.Value.Length.CompareTo(x.Value.Length));
            Dictionary<string, ulong[]> sortedVotes = votes.ToDictionary(x => x.Key, x => x.Value);
            string[] winners = sortedVotes.Where(x => x.Value.Length != 0 && sortedVotes.Values.First().Length == x.Value.Length).Select(x => x.Key).ToArray();
            string winnerString = winners.Length switch
            {
                0 => "The winner is... Nobody! There weren't any votes...",
                1 => $"The winner is {winners[0]} with {sortedVotes[winners[0]].Length.ToMetric()} vote{(sortedVotes[winners[0]].Length == 1 ? null : "s")}!", // The winner is Minecraft with 14,012 votes!
                2 => $"We have a two way tie between {winners[0]} and {winners[1]}. Both have {sortedVotes[winners[0]].Length.ToMetric()} vote{(sortedVotes[winners[0]].Length == 1 ? null : "s")}!", // We have a two way tie between Minecraft and Terraria. Both have 1 vote!
                _ => $"We have a {winners.Length.ToWords()} way tie, each with {sortedVotes[winners[0]].Length.ToMetric()} vote{(sortedVotes[winners[0]].Length == 1 ? null : "s")}! Nobody could decide between {winners.Humanize()}." // We have a six way tie, each with 14,012 votes! Nobody could decide between Minecraft, Terraria, Hollow Knight, Mario Kart Wii, Wii Sports and Smash Bros.!
            };

            DatabaseList<PollModel, Guid> pollModelList = (DatabaseList<PollModel, Guid>)sender!;
            pollModelList.Remove(pollModel);

            Logger<Poll> logger = Program.ServiceProvider.GetService<Logger<Poll>>()!;
            DiscordClient client = Program.DiscordShardedClient.GetShard(pollModel.GuildId);
            if (client == null)
            {
                logger.LogTrace("{PollId}: Failed to get client for guild {GuildId}. Testing to see if the guild still exists...", pollModel.Id, pollModel.GuildId);
                try
                {
                    await Program.DiscordShardedClient.ShardClients[0].GetGuildAsync(pollModel.GuildId);
                    client = Program.DiscordShardedClient.ShardClients[0];
                    logger.LogWarning("{PollId}: Guild {GuildId} still exists. Must be a library bug.", pollModel.Id, pollModel.GuildId);
                }
                catch (NotFoundException) // Guild's been deleted.
                {
                    logger.LogDebug("{PollId}: Failed to get guild {GuildId} from cache as it's been deleted.", pollModel.Id, pollModel.GuildId);
                    DatabaseContext database = Program.ServiceProvider.GetService<DatabaseContext>()!;
                    database.TempRoles.RemoveRange(database.TempRoles.Where(x => x.GuildId == pollModel.GuildId));
                    await database.SaveChangesAsync();
                    pollModelList.Remove(pollModel);
                    return;
                }
                catch (DiscordException error)
                {
                    logger.LogInformation(error, "{PollId}: Failed to get guild {GuildId} from rest request. Error: (HTTP {HTTPCode}) {JsonError}", pollModel.Id, pollModel.GuildId, error.WebResponse.ResponseCode, error.JsonMessage);
                    return;
                }
                return;
            }

            DiscordGuild guild = client.Guilds[pollModel.GuildId];
            if (guild.IsUnavailable)
            {
                logger.LogDebug("{PollId}: Guild {GuildId} is unavailable, not executing the poll. Adding a 5 minute delay.", pollModel.Id, pollModel.GuildId);
                pollModel.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
                pollModelList.Update(pollModel);
                return;
            }

            if (!guild.Channels.TryGetValue(pollModel.ChannelId, out DiscordChannel? channel) || channel == null)
            {
                logger.LogTrace("{PollId}: Failed to get channel {ChannelId} from cache, going to try making a rest request.", pollModel.Id, pollModel.ChannelId);
                try
                {
                    channel = await client.GetChannelAsync(pollModel.ChannelId);
                }
                catch (NotFoundException) // Channel's been deleted.
                {
                    logger.LogDebug("{PollId}: Failed to get channel {ChannelId} as it's been deleted.", pollModel.Id, pollModel.ChannelId);
                    DatabaseContext database = Program.ServiceProvider.GetService<DatabaseContext>()!;
                    database.Polls.RemoveRange(database.Polls.Where(x => x.GuildId == pollModel.GuildId && x.ChannelId == pollModel.ChannelId));
                    await database.SaveChangesAsync();
                    pollModelList.Remove(pollModel);
                    return;
                }
                catch (DiscordException error)
                {
                    logger.LogInformation(error, "{PollId}: Failed to get channel {ChannelId} from rest request. Error: (HTTP {HTTPCode}) {JsonError}", pollModel.Id, pollModel.ChannelId, error.WebResponse.ResponseCode, error.JsonMessage);
                    return;
                }
            }

            DiscordMessage message;
            try
            {
                message = await channel.GetMessageAsync(pollModel.MessageId);
            }
            catch (NotFoundException) // Message's been deleted.
            {
                logger.LogDebug("{PollId}: Failed to get message {MessageId} as it's been deleted.", pollModel.Id, pollModel.MessageId);
                DatabaseContext database = Program.ServiceProvider.GetService<DatabaseContext>()!;
                database.Polls.RemoveRange(database.Polls.Where(x => x.GuildId == pollModel.GuildId && x.MessageId == pollModel.MessageId));
                await database.SaveChangesAsync();
                logger.LogDebug("{PollId}: Removed poll from database due to deleted message ({MessageId}) in guild {GuildId}.", pollModel.Id, pollModel.MessageId, pollModel.GuildId);
                return;
            }
            catch (DiscordException error)
            {
                logger.LogWarning(error, "{PollId}: Failed to get message {MessageId} from rest request. Error: (HTTP {HTTPCode}) {JsonError}", pollModel.Id, pollModel.MessageId, error.WebResponse.ResponseCode, error.JsonMessage);
                return;
            }

            if (!channel.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels))
            {
                await SendDm(logger, guild, pollModel, message, winnerString, true);
            }
            else if (!channel.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.SendMessages))
            {
                await SendDm(logger, guild, pollModel, message, winnerString, await EditMessage(logger, pollModel, message));
            }
            else
            {
                await EditMessage(logger, pollModel, message);
                DiscordMessageBuilder builder = new();
                builder.WithReply(message.Id, false, false);
                builder.WithContent(winnerString);
                builder.WithAllowedMentions(Mentions.None);
                await channel.SendMessageAsync(builder);
            }
        }

        private static async Task<bool> SendDm(Logger<Poll> logger, DiscordGuild guild, PollModel pollModel, DiscordMessage message, string content, bool failedToEdit = false)
        {
            if (!guild.Members.TryGetValue(pollModel.UserId, out DiscordMember? member))
            {
                logger.LogTrace("{PollId}: Failed to get member {UserId} in guild {GuildId} from cache, going to try making a rest request.", pollModel.Id, pollModel.UserId, pollModel.GuildId);
                try
                {
                    member = await guild.GetMemberAsync(pollModel.UserId);
                }
                catch (NotFoundException) // Member has left the guild.
                {
                    logger.LogDebug("{PollId}: Failed to get member {UserId} as they've left guild {GuildId}.", pollModel.Id, pollModel.UserId, pollModel.GuildId);
                    return false;
                }
                catch (DiscordException error)
                {
                    logger.LogInformation(error, "{PollId}: Failed to get member {UserId} in guild {GuildId} from rest request. Error: (HTTP {HTTPCode}) {JsonError}", pollModel.Id, pollModel.UserId, pollModel.GuildId, error.WebResponse.ResponseCode, error.JsonMessage);
                    return false;
                }
            }

            string messageContent = "> " + message.Content;
            messageContent += '\n' + content;
            if (failedToEdit)
            {
                messageContent += $"\nThe poll results were sent in a DM due to the bot missing the required Discord permissions to edit the original poll message. To prevent this in the future, please consider giving the bot the {Formatter.InlineCode(Permissions.AccessChannels.Humanize())} permission.";
            }
            else
            {
                messageContent += $"\nThe poll results were sent in a DM due to the bot missing the required Discord permissions to edit the original poll message. To prevent this in the future, please consider giving the bot the {Formatter.InlineCode(Permissions.SendMessages.Humanize())} permission.";
            }

            try
            {
                DiscordMessageBuilder builder = new();
                builder.Content = messageContent;
                builder.AddComponents(new DiscordActionRowComponent(new[] { new DiscordLinkButtonComponent(message.JumpLink.ToString(), "View Poll") }));
                await member.SendMessageAsync(builder);
            }
            catch (DiscordException)
            {
                logger.LogDebug("{PollId}: Failed to send DM to member {UserId} from guild {GuildId}.", pollModel.Id, pollModel.UserId, pollModel.GuildId);
                return false;
            }

            return true;
        }

        private static async Task<bool> EditMessage(Logger<Poll> logger, PollModel pollModel, DiscordMessage message)
        {
            try
            {
                DiscordMessageBuilder builder = new();
                builder.Content = Formatter.Strike(pollModel.Question) + "\nThe poll has ended! Thanks for submitting your vote!";
                builder.AddComponents(message.Components.Select(component => new DiscordActionRowComponent(component.Components.Select(x => ((DiscordButtonComponent)x).Disable()))));

                if (!SystemFonts.TryGet("SansSerif", CultureInfo.InvariantCulture, out FontFamily sansSerifFont))
                {
                    // Unable to load the font, this means no histogram
                    await message.ModifyAsync(builder);
                    return true;
                }

                using MemoryStream memoryStream = new();
                // Generate a histogram of the votes
                SortedList<int, string> results = new(new Dictionary<int, string>(pollModel.Votes.Select(x => new KeyValuePair<int, string>(x.Value.Length, x.Key))), default);
                using (Image<Rgba32> image = new(1024, 1024))
                {
                    float average = (float)results.Keys.Sum() / results.Count; // This is shown on the left side of the histogram
                    int imageSize = image.Size().Width;
                    int padding = imageSize / 10;
                    image.Mutate(x => x.BackgroundColor(Color.GhostWhite));
                    Pen blackPen = new(Color.Black, imageSize / 50);
                    FontRectangle textBox = TextMeasurer.MeasureBounds("Total Number of Polls", new(new Font(sansSerifFont, 48)));
                    image.Mutate(x => x.DrawPolygon(blackPen, new PointF((imageSize / 2) - textBox.Width, padding), new PointF((imageSize / 2) + textBox.Width, padding)));
                    image.Mutate(x => x.DrawText("Total Number of Polls", new(sansSerifFont, 48), Color.Black, new PointF((imageSize / 2) - (textBox.Width / 2), padding)));

                    DrawingOptions emptyDrawingOptions = new(); // Because there's nothing wrong with the default drawing options
                    float lastX = padding;
                    foreach (KeyValuePair<int, string> result in results)
                    {
                        float barWidth = (imageSize - (padding * 2) - (int)blackPen.StrokeWidth) / average;
                        float barHeight = (imageSize - (padding * 2) - (int)blackPen.StrokeWidth) * result.Key / average;
                        lastX += barWidth;
                        image.Mutate(x => x.Draw(emptyDrawingOptions, blackPen, new RectangleF(lastX, padding, image.Width, image.Height)));
                    }
                    image.SaveAsPng(memoryStream);
                }

                builder.WithFile("histogram.png", memoryStream);
                await message.ModifyAsync(builder);
                return true;
            }
            catch (DiscordException error)
            {
                logger.LogDebug(error, "{PollId}: Failed to edit message {MessageId} in channel {ChannelId} in guild {GuildId}. Error: (HTTP {HTTPCode}) {JsonError}", pollModel.Id, pollModel.MessageId, pollModel.ChannelId, pollModel.GuildId, error.WebResponse.ResponseCode, error.JsonMessage);
                return false;
            }
        }
    }
}
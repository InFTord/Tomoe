using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Tomoe.Models;

namespace Tomoe.Commands.Common
{
    public sealed partial class TagCommand : ApplicationCommandModule
    {
        [SlashCommand("author", "Gets the author of a tag.")]
        public async Task AuthorAsync(InteractionContext context, [Option("name", "Which tag to gather information on.")] string tagName)
        {
            Tag? tag = await GetTagAsync(tagName, context.Guild.Id);
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = tag is null ? $"Error: Tag `{tagName.ToLowerInvariant()}` does not exist!" : $"<@{tag.OwnerId}> ({tag.OwnerId})",
                IsEphemeral = tag is null
            });
        }
    }
}
namespace Tomoe.Commands
{
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.SlashCommands;
    using Tomoe.Db;

    public partial class Public : ApplicationCommandModule
    {
        public partial class Tags : ApplicationCommandModule
        {
            [SlashCommand("author", "Gets the author of a tag.")]
            public async Task Author(InteractionContext context, [Option("name", "Which tag to gather information on.")] string tagName)
            {
                Tag tag = await GetTagAsync(tagName, context.Guild.Id);
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = tag == null ? $"Error: Tag `{tagName.ToLowerInvariant()}` does not exist!" : $"<@{tag.OwnerId}> ({tag.OwnerId})",
                    IsEphemeral = tag == null
                });
            }
        }
    }
}

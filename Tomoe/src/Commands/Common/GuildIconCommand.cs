using System;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using OoLunar.DSharpPlus.CommandAll.Attributes;
using OoLunar.DSharpPlus.CommandAll.Commands;

namespace OoLunar.Tomoe.Commands.Common
{
    public sealed class GuildIconCommand : BaseCommand
    {
        [Command("guild_icon", "guild_avatar", "guild_picture"), CommandOverloadPriority(0, true)]
        public static Task ExecuteAsync(CommandContext context, ImageFormat imageFormat = ImageFormat.Auto, ushort imageDimensions = 0) => context.Guild is null
            ? context.ReplyAsync($"Command `/{context.CurrentCommand.FullName}` can only be used in a guild.")
            : context.ReplyAsync(context.Guild.GetIconUrl(imageFormat == ImageFormat.Unknown ? ImageFormat.Auto : imageFormat, imageDimensions == 0 ? (ushort)1024 : imageDimensions));

        [Command("guild_icon")]
        public static async Task ExecuteAsync(CommandContext context, ulong guildId = 0, ImageFormat imageFormat = ImageFormat.Auto, ushort imageDimensions = 0)
        {
            if (context.Client.Guilds.TryGetValue(guildId, out DiscordGuild? guild))
            {
                await context.ReplyAsync(guild.GetIconUrl(imageFormat, imageDimensions));
                return;
            }

            DiscordGuildPreview guildPreview = await context.Client.GetGuildPreviewAsync(guildId);
            string? iconUrl = GetIconUrl(guildPreview, imageFormat, imageDimensions);
            if (iconUrl == null)
            {
                await context.ReplyAsync("Could not find an icon for the guild.");
            }
            else
            {
                await context.ReplyAsync(iconUrl);
            }
        }

        /// <summary>
        /// Gets guild's icon URL, in requested format and size.
        /// </summary>
        /// <param name="imageFormat">The image format of the icon to get.</param>
        /// <param name="imageSize">The maximum size of the icon. Must be a power of two, minimum 16, maximum 4096.</param>
        /// <returns>The URL of the guild's icon.</returns>
        public static string? GetIconUrl(DiscordGuildPreview guildPreview, ImageFormat imageFormat, ushort imageSize = 1024)
        {
            if (string.IsNullOrWhiteSpace(guildPreview.Icon))
            {
                return null;
            }
            else if (imageFormat == ImageFormat.Unknown)
            {
                imageFormat = ImageFormat.Auto;
            }

            // Makes sure the image size is in between Discord's allowed range.
            if (imageSize is < 16 or > 4096)
            {
                throw new ArgumentOutOfRangeException(nameof(imageSize), imageSize, "Image Size is not in between 16 and 4096.");
            }
            // Checks to see if the image size is not a power of two.
            else if (!(imageSize is not 0 && (imageSize & (imageSize - 1)) is 0))
            {
                throw new ArgumentOutOfRangeException(nameof(imageSize), imageSize, "Image size is not a power of two.");
            }

            // Get the string variants of the method parameters to use in the urls.
            string stringImageFormat = imageFormat switch
            {
                ImageFormat.Gif => "gif",
                ImageFormat.Jpeg => "jpg",
                ImageFormat.Png => "png",
                ImageFormat.WebP => "webp",
                ImageFormat.Auto => !string.IsNullOrWhiteSpace(guildPreview.Icon) ? (guildPreview.Icon.StartsWith("a_", false, CultureInfo.InvariantCulture) ? "gif" : "png") : "png",
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat)),
            };

            string stringImageSize = imageSize.ToString(CultureInfo.InvariantCulture);
            return $"https://cdn.discordapp.com/icons/{guildPreview.Id}/{guildPreview.Icon}.{stringImageFormat}?size={stringImageSize}";
        }
    }
}

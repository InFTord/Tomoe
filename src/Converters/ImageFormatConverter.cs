using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace Tomoe.Converters
{
    public class ImageFormatConverter : IArgumentConverter<ImageFormat>
    {
        public Task<Optional<ImageFormat>> ConvertAsync(string value, CommandContext ctx) => value.ToLowerInvariant() switch
        {
            "png" => Task.FromResult(Optional.FromValue(ImageFormat.Png)),
            "jpeg" or "jpg" => Task.FromResult(Optional.FromValue(ImageFormat.Jpeg)),
            "webp" => Task.FromResult(Optional.FromValue(ImageFormat.WebP)),
            "gif" => Task.FromResult(Optional.FromValue(ImageFormat.Gif)),
            "unknown" or "auto" => Task.FromResult(Optional.FromValue(ImageFormat.Auto)),
            _ => Task.FromResult(Optional.FromNoValue<ImageFormat>())
        };
    }
}
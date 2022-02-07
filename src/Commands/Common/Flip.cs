using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace Tomoe.Commands.Common
{
    public class Flip : BaseCommandModule
    {
        [Command("flip"), Description("A simple heads or tails command."), Aliases("choose", "pick")]
        public async Task FlipAsync(CommandContext context) => await context.RespondAsync(Random.Shared.Next(0, 2) == 0 ? "Heads" : "Tails");

        [Command("flip")]
        public async Task FlipAsync(CommandContext context, [Description("Have Tomoe pick from the choices listed.")] params string[] choices) => await context.RespondAsync(choices[Random.Shared.Next(0, choices.Length)]);
    }
}
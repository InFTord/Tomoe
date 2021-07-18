namespace Tomoe.Commands
{
    using DSharpPlus;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Tomoe.Db;
    using Tomoe.Utilities.Types;

    public partial class Listeners
    {
        public static Dictionary<string, QueueButton> QueueButtons { get; private set; } = new();

        public static async Task ButtonClicked(DiscordClient discordClient, ComponentInteractionCreateEventArgs componentInteractionCreateEventArgs)
        {
            string id = componentInteractionCreateEventArgs.Id.Split('-')[0];
            int stage = int.Parse(componentInteractionCreateEventArgs.Id.Split('-')[1], NumberStyles.Number, CultureInfo.InvariantCulture);

            using IServiceScope scope = Program.ServiceProvider.CreateScope();
            Database database = scope.ServiceProvider.GetService<Database>();
            PermanentButton button = database.PermanentButtons.FirstOrDefault(button => button.ButtonId == id && button.GuildId == componentInteractionCreateEventArgs.Guild.Id);
            if (button != null)
            {
                switch (button.ButtonType)
                {
                    case ButtonType.MenuRole:
                        componentInteractionCreateEventArgs.Handled = true;
                        if (stage == 1)
                        {
                            await Moderation.MenuRoles.Assign(componentInteractionCreateEventArgs.Interaction, await componentInteractionCreateEventArgs.User.Id.GetMember(componentInteractionCreateEventArgs.Guild), componentInteractionCreateEventArgs.Id, database);
                        }
                        else if (stage == 2)
                        {
                            await Moderation.MenuRoles.Assign(componentInteractionCreateEventArgs, componentInteractionCreateEventArgs.Id, database);
                        }
                        break;
                    case ButtonType.Temporary:
                        if (QueueButtons.TryGetValue(id, out QueueButton queueButton1) && queueButton1.UserId == componentInteractionCreateEventArgs.User.Id)
                        {
                            queueButton1.SelectedButton = queueButton1.Components.First(discordComponent => discordComponent.CustomId == componentInteractionCreateEventArgs.Id);
                            QueueButtons.Remove(id);
                            await componentInteractionCreateEventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate, new());
                            componentInteractionCreateEventArgs.Handled = true;
                        }
                        break;
                    default:
                        return;
                }
            }
            // TODO: Remake the QueueButton class
            if (QueueButtons.TryGetValue(id, out QueueButton queueButton) && queueButton.UserId == componentInteractionCreateEventArgs.User.Id)
            {
                queueButton.SelectedButton = queueButton.Components.First(discordComponent => discordComponent.CustomId == componentInteractionCreateEventArgs.Id);
                QueueButtons.Remove(id);
                await componentInteractionCreateEventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate, new());
                componentInteractionCreateEventArgs.Handled = true;
            }
        }
    }
}

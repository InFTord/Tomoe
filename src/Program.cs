namespace Tomoe
{
    using DSharpPlus;
    using DSharpPlus.SlashCommands;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using System.Threading.Tasks;
    using Tomoe.Utilities.Configs;

    public class Program
    {
        public static DiscordShardedClient Client { get; private set; }
        public static Config Config { get; private set; }
        public static ServiceProvider ServiceProvider { get; private set; }

        public static void Main() => MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        public static async Task MainAsync()
        {
            ServiceCollection services = new();
            Config = await Config.Load();
            Config.Logger.Load(services);
            await Config.Database.Load(services);
            ServiceProvider = services.BuildServiceProvider();
            Serilog.ILogger logger = Log.Logger.ForContext<Program>();

            DiscordConfiguration discordConfiguration = new()
            {
                Intents = DiscordIntents.GuildMembers // Required for persistent roles
                    | DiscordIntents.Guilds // Required for channel permissions and member cache
                    | DiscordIntents.GuildPresences // Required for member cache
                    | DiscordIntents.GuildMessages // Required for automod
                    | DiscordIntents.GuildMessageReactions, // Required for reaction roles
                Token = Config.DiscordApiToken,
                LoggerFactory = ServiceProvider.GetService<ILoggerFactory>()
            };

            SlashCommandsConfiguration slashCommandsConfiguration = new()
            {
                Services = ServiceProvider
            };

            logger.Information("Registering commands...");
            Client = new(discordConfiguration);

            logger.Information("Connecting to Discord...");
            await Client.StartAsync();

            Client.ComponentInteractionCreated += Commands.Listeners.ButtonClicked;
            Client.GuildMemberAdded += Commands.Listeners.PersistentRoles;
            Client.GuildMemberRemoved += Commands.Listeners.PersistentRoles;
            Client.GuildDownloadCompleted += Commands.Listeners.GuildDownloadCompleted;
            Client.GuildAvailable += Commands.Listeners.GuildMemberCache;
            Client.GuildCreated += Commands.Listeners.GuildMemberCache;

            foreach (SlashCommandsExtension slashCommandShardExtension in (await Client.UseSlashCommandsAsync(slashCommandsConfiguration)).Values)
            {
                slashCommandShardExtension.RegisterCommands(typeof(Commands.Moderation), 730888468330315827);
                slashCommandShardExtension.RegisterCommands(typeof(Commands.Public), 730888468330315827);
                slashCommandShardExtension.SlashCommandErrored += Commands.Listeners.CommandErrored;
            }
            logger.Information("Commands up!");
            await Task.Delay(-1);
        }
    }
}
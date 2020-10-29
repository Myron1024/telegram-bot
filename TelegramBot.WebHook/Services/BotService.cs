using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramBot.WebHook.Models;

namespace TelegramBot.WebHook.Services
{
    public class BotService : IBotService
    {
        private readonly BotConfiguration _config;

        public BotService(IOptions<BotConfiguration> config)
        {
            _config = config.Value;
            Client = new TelegramBotClient(_config.BotToken);
            Token = _config.BotToken;
        }

        public TelegramBotClient Client { get; }

        public string Token { get; }
    }
}

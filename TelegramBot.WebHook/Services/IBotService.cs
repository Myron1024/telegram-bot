using Telegram.Bot;

namespace TelegramBot.WebHook.Services
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }

        string Token { get; }
    }
}
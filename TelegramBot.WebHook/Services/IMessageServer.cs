using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.WebHook.Services
{
    public interface IMessageServer
    {
        Task OnMessageReceived(TelegramBotClient Bot, Message message);
    }
}

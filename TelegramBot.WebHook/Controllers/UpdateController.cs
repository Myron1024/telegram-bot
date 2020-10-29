using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using TelegramBot.WebHook.Services;
using Newtonsoft.Json;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.WebHook.Controllers
{
    public class UpdateController : Controller
    {
        private readonly ILogger _logger;
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IMessageServer _messageServer;

        public UpdateController(IUpdateService updateService, ILogger<UpdateController> logger, IBotService botService, IMessageServer messageServer)
        {
            _updateService = updateService;
            _logger = logger;
            _botService = botService;
            _messageServer = messageServer;
        }

        public IActionResult Index()
        {
            return Content("Hi");
        }

        [HttpPost]
        public async Task<IActionResult> PostMsg([FromBody]Update update)
        {
            _logger.LogInformation("update::" + JsonConvert.SerializeObject(update));

            if (update.Type != UpdateType.Message)
            {
                return Content("0");
            }

            var message = update.Message;
            var Bot = _botService.Client;

            await _messageServer.OnMessageReceived(Bot, message);

            return Ok();
        }
    }
}
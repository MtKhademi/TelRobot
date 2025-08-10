using Telegram.Bot;

namespace BotTelegram;

public class BotTelegramService
{
    private readonly string _token;
    private readonly TelegramBotClient _botClient;

    public BotTelegramService(string token)
    {
        _token = token;
        _botClient = new TelegramBotClient(_token);
    }

    public async Task ExecuteAsync()
    {

        var me = await _botClient.GetMe();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
    }


    private async Task ME()
    {
            
    }


}
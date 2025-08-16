using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.IO;
using System.Linq;
using System.Threading;

namespace BotTelegram;

public class BotTelegramService
{
    private readonly string _token;
    private readonly TelegramBotClient _botClient;
    private CancellationTokenSource? _cts;

    public BotTelegramService(string token)
    {
        _token = token;
        _botClient = new TelegramBotClient(_token);
        _botClient.OnMessage += BotClient_OnMessage;
    }

    public async Task ExecuteAsync()
    {
        // Log bot identity and keep the app alive while polling via events
        await MEAsync();
        Console.WriteLine("Bot is up. Press Enter to stop.");
        Console.ReadLine();
    }


    private async Task MEAsync()
    {
        Console.WriteLine("==========> 1. GetMe");
        var me = await _botClient.GetMe();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
        Console.WriteLine("=========================================");
    }

    private async Task BotClient_OnMessage(Message message, UpdateType type)
    {
        Console.WriteLine("==========> OnMessage [Receive message]");
        Console.WriteLine($"=> Message ID: {message.MessageId}");
        Console.WriteLine($"=> Type: {type}");
        Console.WriteLine($"=> From: {message.From?.FirstName} {message.From?.LastName} (ID: {message.From?.Id})");
        Console.WriteLine($"=> Chat ID: {message.Chat.Id}");
        Console.WriteLine($"=> Date: {message.Date}");

        if (type == UpdateType.Message)
        {
            // Photos (users sending as photo)
            if (message.Photo != null && message.Photo.Any())
            {
                try
                {
                    var bestPhoto = message.Photo.OrderBy(p => p.FileSize).Last();
                    Console.WriteLine($"=> Photo received. FileId: {bestPhoto.FileId}, Size: {bestPhoto.FileSize}");

                    var file = await _botClient.GetFile(bestPhoto.FileId);

                    var downloadsDir = Path.Combine(AppContext.BaseDirectory, "downloads", "photos");
                    Directory.CreateDirectory(downloadsDir);

                    var ext = Path.GetExtension(file.FilePath ?? string.Empty);
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";

                    var localFileName = $"{message.MessageId}_{bestPhoto.FileUniqueId}{ext}";
                    var localPath = Path.Combine(downloadsDir, localFileName);

                    await using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await _botClient.DownloadFile(file.FilePath!, fs);
                    }

                    Console.WriteLine($"=> Photo saved to: {localPath}");
                    await _botClient.SendMessage(message.Chat, $"Saved your photo as {localFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!! Failed to download photo: {ex.Message}");
                    await _botClient.SendMessage(message.Chat, "Sorry, I couldn't download the photo.");
                }

                Console.WriteLine("=========================================");
                return;
            }

            // Image-like documents
            if (message.Document != null && (message.Document.MimeType?.StartsWith("image/") ?? false))
            {
                try
                {
                    var doc = message.Document;
                    Console.WriteLine($"=> Image document received. FileId: {doc.FileId}, Name: {doc.FileName}, Size: {doc.FileSize}");

                    var file = await _botClient.GetFile(doc.FileId);

                    var downloadsDir = Path.Combine(AppContext.BaseDirectory, "downloads", "photos");
                    Directory.CreateDirectory(downloadsDir);

                    var ext = Path.GetExtension(doc.FileName ?? string.Empty);
                    if (string.IsNullOrEmpty(ext)) ext = Path.GetExtension(file.FilePath ?? string.Empty);
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";

                    var localFileName = $"{message.MessageId}_{doc.FileUniqueId}{ext}";
                    var localPath = Path.Combine(downloadsDir, localFileName);

                    await using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await _botClient.DownloadFile(file.FilePath!, fs);
                    }

                    Console.WriteLine($"=> Image document saved to: {localPath}");
                    await _botClient.SendMessage(message.Chat, $"Saved your image as {localFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!! Failed to download image document: {ex.Message}");
                    await _botClient.SendMessage(message.Chat, "Sorry, I couldn't download the image file.");
                }

                Console.WriteLine("=========================================");
                return;
            }

            // Fallback: text or other types
            if (!string.IsNullOrEmpty(message.Text))
            {
                Console.WriteLine($"=> Received message: {message.Text}");
                await _botClient.SendMessage(message.Chat, $"You said: {message.Text}");
            }
        }
        Console.WriteLine("=========================================");
    }

}
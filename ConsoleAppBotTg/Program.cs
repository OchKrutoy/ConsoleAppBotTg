using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
class Program
{
    private static readonly string BotToken = "9288914108:AAHNcsNZZ3NQf-8PD_USC_irldDNn0yrxAo";
    static async Task Main()
    {
        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated(); 
        }
        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );
        var me = await botClient.GetMe();
        Console.WriteLine($"Start listening for @{me.Username}");
        Console.WriteLine("Press any key to exit");
        Console.ReadLine();
        cts.Cancel();
    }
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText } message)
            return;

        var chatId = message.Chat.Id;
        var text = messageText.Trim();
        using var db = new AppDbContext();

        if (text.StartsWith("/add "))
        {
            string taskText = text.Substring(5).Trim();

            if (string.IsNullOrWhiteSpace(taskText))
            {
                await botClient.SendMessage(chatId, "Напиши задачу после команды. Пример: /add Купить хлеб", cancellationToken: cancellationToken);
                return;
            }

            var newItem = new TodoItem { ChatId = chatId, Text = taskText, IsCompleted = false };
            db.TodoItems.Add(newItem);
            await db.SaveChangesAsync();

            await botClient.SendMessage(chatId, $"Задача добавлена: {taskText}", cancellationToken: cancellationToken);
        }
        else if (text == "/list")
        {
            var items = db.TodoItems.Where(t => t.ChatId == chatId).ToList();

            if (items.Count == 0)
            {
                await botClient.SendMessage(chatId, "Твой список пуст. Добавь задачу: /add Купить хлеб", cancellationToken: cancellationToken);
                return;
            }

            string result = "Твои задачи:\n";
            foreach (var item in items)
            {
                string status = item.IsCompleted ? "✅" : "⬜";
                result += $"{status} {item.Id}. {item.Text}\n";
            }

            await botClient.SendMessage(chatId, result, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(chatId, "Я тебя не понимаю. Доступные команды:\n/add [текст] - добавить\n/list - показать", cancellationToken: cancellationToken);
        }
    }
    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
public class  TodoItem
{
    public int Id { get; set; }
    public long ChatId { get; set; } 
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

}
public class AppDbContext : DbContext
{
    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=bot_todo.db");
    }
}
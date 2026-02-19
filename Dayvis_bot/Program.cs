using Telegram.Bot;

partial class Program
{
    static async Task Main(string[] args)
    {
        var bot = new TelegramBotClient("TELEGRAM_BOT_TOKEN");
        var me = await bot.GetMe();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
    }

}


using Telegram.Bot;

partial class Program
{
    static async Task Main(string[] args)
    {
        var bot = new TelegramBotClient("8344813594:AAG4-K41gYmMfvXknZkBCTkApCyHc1kxqXc");
        var me = await bot.GetMe();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
    }
}
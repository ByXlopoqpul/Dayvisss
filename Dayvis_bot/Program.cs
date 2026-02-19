using Telegram.Bot;

var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("❌ ОШИБКА: Токен бота не найден!");
    return;
}

var bot = new TelegramBotClient(token);
var me = await bot.GetMe();
Console.WriteLine($"✅ Бот запущен: {me.FirstName} (@{me.Username})");

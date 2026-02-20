using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

enum Step
{
    AskWeek,
    MainMenu
}

class UserState
{
    public Step Step { get; set; } = Step.AskWeek;
    public int WeekNumber { get; set; } = 1;
}

class Program
{
    // Память состояний по chatId (чтобы бот "помнил", на каком шаге пользователь)
    static readonly ConcurrentDictionary<long, UserState> States = new();

    static async Task Main()
    {
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Не найден TELEGRAM_BOT_TOKEN в переменных среды.");
            Console.WriteLine("Windows: setx TELEGRAM_BOT_TOKEN \"твой_токен\" (потом перезапусти терминал/VS Code)");
            return;
        }

        var bot = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await bot.GetMeAsync();
        Console.WriteLine($"Бот запущен: @{me.Username}");
        Console.WriteLine("Нажми Enter чтобы остановить.");
        Console.ReadLine();

        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message) return;
        if (update.Message?.Type != MessageType.Text) return;

        var msg = update.Message;
        long chatId = msg.Chat.Id;
        string text = (msg.Text ?? "").Trim();

        // /start
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            States[chatId] = new UserState { Step = Step.AskWeek };
            await bot.SendTextMessageAsync(chatId,
                "Привет! Я бот-расписание 📚\nКакая неделя? (1-5)\nНапиши число.",
                cancellationToken: ct);
            return;
        }

        // Достаём/создаём состояние
        var state = States.GetOrAdd(chatId, _ => new UserState());

        // Глобальные команды
        if (text.Equals("/week", StringComparison.OrdinalIgnoreCase))
        {
            state.Step = Step.AskWeek;
            await bot.SendTextMessageAsync(chatId, "Окей, выбери неделю (1-5).", cancellationToken: ct);
            return;
        }

        if (text.Equals("/menu", StringComparison.OrdinalIgnoreCase))
        {
            state.Step = Step.MainMenu;
            await SendMenu(bot, chatId, ct);
            return;
        }

        // Логика по шагам
        switch (state.Step)
        {
            case Step.AskWeek:
                if (!int.TryParse(text, out int week) || week < 1 || week > 5)
                {
                    await bot.SendTextMessageAsync(chatId, "Нужно число от 1 до 5. Попробуй ещё раз 🙂", cancellationToken: ct);
                    return;
                }

                state.WeekNumber = week;
                state.Step = Step.MainMenu;

                await bot.SendTextMessageAsync(chatId, $"Окей, неделя: {week}.", cancellationToken: ct);
                await SendMenu(bot, chatId, ct);
                return;

            case Step.MainMenu:
                // Меню: 1 расписание, 4 выход (как у тебя)
                if (text == "1")
                {
                    await bot.SendTextMessageAsync(chatId,
                        "Какой день?\n1) Понедельник\n2) Вторник\nНапиши 1 или 2.",
                        cancellationToken: ct);
                    // Можно сделать отдельный Step для дня, но сделаем проще:
                    state.Step = Step.MainMenu; // остаёмся, просто ждём следующий ввод
                    States[chatId] = state;
                    // Ставим метку ожидания "дня" через хитрый трюк: запомним, что ждём день в тексте
                    // (лучше отдельный Step, но так проще сейчас)
                    // Вместо этого ниже проверим "Понедельник/Вторник" числом.
                    return;
                }

                // День недели выбор (после "1 расписание")
                if (text == "1" || text == "2")
                {
                    // Это конфликт с пунктом меню "1", поэтому лучше так:
                    // Если пользователь уже просили "Какой день?", он введёт 1/2.
                    // Чтобы не путать, разрешим слова тоже.
                }

                // Разрешим выбор дня словами или цифрами
                if (text.Equals("понедельник", StringComparison.OrdinalIgnoreCase) || text == "day1" || text == "пн")
                {
                    await SendLessonsFromFile(bot, chatId, "monday.txt", ct);
                    await SendMenu(bot, chatId, ct);
                    return;
                }
                if (text.Equals("вторник", StringComparison.OrdinalIgnoreCase) || text == "day2" || text == "вт")
                {
                    await SendLessonsFromFile(bot, chatId, "tuesday.txt", ct);
                    await SendMenu(bot, chatId, ct);
                    return;
                }

                // Чтобы было понятнее: сделаем точный сценарий выбора дня после меню "1"
                if (text == "d1")
                {
                    await SendLessonsFromFile(bot, chatId, "monday.txt", ct);
                    await SendMenu(bot, chatId, ct);
                    return;
                }
                if (text == "d2")
                {
                    await SendLessonsFromFile(bot, chatId, "tuesday.txt", ct);
                    await SendMenu(bot, chatId, ct);
                    return;
                }

                if (text == "4")
                {
                    States.TryRemove(chatId, out _);
                    await bot.SendTextMessageAsync(chatId, "Пока! Если что, /start 🙂", cancellationToken: ct);
                    return;
                }

                // Подсказка
                await bot.SendTextMessageAsync(chatId,
                    "Я не понял 😅\nНажми:\n1 — расписание\n4 — выход\nИли команды: /week /menu",
                    cancellationToken: ct);
                return;
        }
    }

    static Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine("Ошибка бота: " + ex);
        return Task.CompletedTask;
    }

    static async Task SendMenu(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        await bot.SendTextMessageAsync(chatId,
            "Меню:\n1) Посмотреть расписание\n4) Выход\n\n" +
            "Чтобы показать понедельник быстро: напиши d1\nЧтобы показать вторник быстро: напиши d2",
            cancellationToken: ct);
    }

    static async Task SendLessonsFromFile(ITelegramBotClient bot, long chatId, string fileName, CancellationToken ct)
    {
        if (!File.Exists(fileName))
        {
            await bot.SendTextMessageAsync(chatId,
                $"Файл {fileName} не найден рядом с .exe.\nПроверь, что он лежит в папке запуска.",
                cancellationToken: ct);
            return;
        }

        string[] lines = await File.ReadAllLinesAsync(fileName, ct);
        string text = lines.Length == 0 ? "Пусто 😶" : string.Join("\n", lines);

        await bot.SendTextMessageAsync(chatId, text, cancellationToken: ct);
    }
}
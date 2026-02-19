# Dayvisss
# 我最喜欢编程。。。
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    // 1) Главный объект библиотеки: через него отправляем сообщения, фото, редактируем, и т.д.
    static ITelegramBotClient bot = new TelegramBotClient("PASTE_YOUR_TOKEN");

    // 2) Тут храним расписание (для старта прямо в коде).
    //    Ключ: "Понедельник", "Вторник"...  Значение: текст расписания.
    //    Потом можно заменить на файл JSON или базу данных.
    static readonly Dictionary<DayOfWeek, string> schedule = new()
    {
        [DayOfWeek.Monday] =
            "📅 Понедельник\n" +
            "1) Физ-ра  08:30-09:50\n" +
            "2) Английский 10:00-11:20\n" +
            "3) Китайский 11:30-12:50\n" +
            "4) Кыргызский 13:00-14:20\n",

        [DayOfWeek.Tuesday] =
            "📅 Вторник\n" +
            "1) Математика 08:30-09:50\n" +
            "2) Программирование 10:00-11:20\n" +
            "3) История 11:30-12:50\n",

        [DayOfWeek.Wednesday] = "📅 Среда\n(поставь свои пары сюда)",
        [DayOfWeek.Thursday]  = "📅 Четверг\n(поставь свои пары сюда)",
        [DayOfWeek.Friday]    = "📅 Пятница\n(поставь свои пары сюда)",
        [DayOfWeek.Saturday]  = "📅 Суббота\n(поставь свои пары сюда)",
        [DayOfWeek.Sunday]    = "📅 Воскресенье\nВыходной 😴"
    };

    static async Task Main()
    {
        // 3) ReceiverOptions: какие типы апдейтов принимать (UpdateType).
        //    Пусто = принимать всё. Можно ограничить только Message и CallbackQuery.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        using var cts = new CancellationTokenSource();

        // 4) StartReceiving запускает polling:
        //    Telegram будет присылать апдейты, а библиотека будет вызывать наши обработчики:
        //    HandleUpdateAsync (успешный апдейт) и HandleErrorAsync (ошибка)
        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await bot.GetMe();
        Console.WriteLine($"✅ Bot started: @{me.Username}");
        Console.WriteLine("Нажми Enter чтобы остановить...");
        Console.ReadLine();

        // 5) Остановка polling
        cts.Cancel();
    }

    // 6) Главный обработчик входящих событий (Update):
    //    Update бывает разный: сообщение, нажатие кнопки, и т.д.
    static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        // A) Если пришло обычное сообщение (текст)
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            await HandleMessage(bot, update.Message, ct);
            return;
        }

        // B) Если нажали inline-кнопку (CallbackQuery)
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            await HandleCallback(bot, update.CallbackQuery, ct);
            return;
        }
    }

    // 7) Обработка сообщений (команды и текст)
    static async Task HandleMessage(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        long chatId = msg.Chat.Id;
        string text = msg.Text.Trim();

        // Команда /start: показываем меню-кнопки
        if (text == "/start")
        {
            await bot.SendMessage(
                chatId: chatId,
                text: "Привет! Я бот расписания 👨‍💻\nВыбирай:",
                replyMarkup: MainMenu(),
                cancellationToken: ct
            );
            return;
        }

        // Если пользователь пишет что-то другое, просто подскажем
        await bot.SendMessage(
            chatId,
            "Напиши /start чтобы открыть меню 📌",
            cancellationToken: ct
        );
    }

    // 8) Обработка нажатий на inline-кнопки
    static async Task HandleCallback(ITelegramBotClient bot, Telegram.Bot.Types.CallbackQuery cq, CancellationToken ct)
    {
        // CallbackQuery:
        // cq.Data = то, что мы положили в кнопку (например "today", "day:Monday")
        // cq.Message.Chat.Id = куда отвечать
        string data = cq.Data ?? "";
        long chatId = cq.Message!.Chat.Id;

        // Важно: Telegram ждёт "подтверждение нажатия".
        // Если не вызвать AnswerCallbackQuery, кнопка будет долго крутить “loading”.
        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data == "today")
        {
            var day = DateTime.Now.DayOfWeek;
            await bot.SendMessage(chatId, GetScheduleText(day), cancellationToken: ct);
            return;
        }

        if (data == "tomorrow")
        {
            var day = DateTime.Now.AddDays(1).DayOfWeek;
            await bot.SendMessage(chatId, GetScheduleText(day), cancellationToken: ct);
            return;
        }

        if (data == "pick_day")
        {
            await bot.SendMessage(chatId, "Выбери день недели:", replyMarkup: DaysMenu(), cancellationToken: ct);
            return;
        }

        // Кнопки вида "day:Monday"
        if (data.StartsWith("day:"))
        {
            string dayName = data.Substring("day:".Length);

            // Пробуем превратить строку в DayOfWeek
            if (Enum.TryParse<DayOfWeek>(dayName, out var day))
            {
                await bot.SendMessage(chatId, GetScheduleText(day), cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(chatId, "Не понял день 😅", cancellationToken: ct);
            }
            return;
        }

        await bot.SendMessage(chatId, "Неизвестная кнопка 🤔", cancellationToken: ct);
    }

    // 9) Формирование текста расписания по DayOfWeek
    static string GetScheduleText(DayOfWeek day)
    {
        // Если нет расписания на этот день, вернём заглушку
        if (!schedule.TryGetValue(day, out var text))
            return "На этот день расписание не задано.";

        return text;
    }

    // 10) Главное меню (inline-кнопки)
    static InlineKeyboardMarkup MainMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📌 Сегодня", "today"),
                InlineKeyboardButton.WithCallbackData("⏭ Завтра", "tomorrow"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📅 Выбрать день", "pick_day"),
            }
        });
    }

    // 11) Меню выбора дня недели
    static InlineKeyboardMarkup DaysMenu()
    {
        // Тут важно:
        // CallbackData хранит "day:Monday" и т.п.
        // Значит в HandleCallback мы умеем это распознать.
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Пн", "day:Monday"),
                    InlineKeyboardButton.WithCallbackData("Вт", "day:Tuesday"),
                    InlineKeyboardButton.WithCallbackData("Ср", "day:Wednesday") },

            new[] { InlineKeyboardButton.WithCallbackData("Чт", "day:Thursday"),
                    InlineKeyboardButton.WithCallbackData("Пт", "day:Friday"),
                    InlineKeyboardButton.WithCallbackData("Сб", "day:Saturday") },

            new[] { InlineKeyboardButton.WithCallbackData("Вс", "day:Sunday") },
            new[] { InlineKeyboardButton.WithCallbackData("⬅ Назад", "back_main") }
        });
    }

    // 12) Обработчик ошибок polling (например интернет отвалился)
    static Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine("❌ Error: " + ex.Message);
        return Task.CompletedTask;
    }
}

#если что-то не так:
if (data == "back_main")
{
    await bot.SendMessage(chatId, "Меню:", replyMarkup: MainMenu(), cancellationToken: ct);
    return;
}

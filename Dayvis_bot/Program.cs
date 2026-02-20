using System;
using System.Collections.Generic;
using System.IO;
using Telegram.Bot;

class Lesson
{
    // public string Subject {get; set;}      
    // public List<string> Notes {get; set;}
}

class Program
{
    static async Task Main()
    {
        var bot = new TelegramBotClient("TELEGRAM_BOT_TOKEN");

        bool isProgram = true;     

        string[] tuesdayLessons = File.ReadAllLines("tuesday.txt");
        string[] mondayLessons = File.ReadAllLines("monday.txt");

        while (isProgram)
        {
            await bot.SendMessage(msg.Chat, "Какая неделя? (1, 2, ... 5)");

            int input = Convert.ToInt32(Console.ReadLine());

            switch (input)
            {
                case 1:
                    Console.WriteLine("\n1. Посмотреть расписание.\n2. Добавить/удалить заметки к предмету.\n3. Редактировать расписание\n4. Выход.\n");
                    int week = Convert.ToInt32(Console.ReadLine());

                    switch (week)
                    {
                        case 1:
                            MondayLessons(mondayLessons);
                            break;

                        case 2:
                            Console.WriteLine("Добавить (1), удалить (2)");
                            int addOrDelete = Convert.ToInt32(Console.ReadLine());

                            switch (addOrDelete)
                            {
                                case 1:
                                    Console.Write("\n: ");
                                    string Add = Console.ReadLine();
                                    break;
                            }
                            break;
                    }
                    break;   

                case 2:
                    

                    break;



                case 4:
                    isProgram = false;
                    break;
            }
        }
    }


    static void MondayLessons(string[] mondayLessons)
    {
        foreach(string number in mondayLessons)
        {
            Console.Write("\n" + number);
        }
    }
    static void TuesdayLessons(string[] tuesdayLessons)
    {
        foreach(string number in tuesdayLessons)
        {
            Console.Write("\n" + number);
        }
    }


}
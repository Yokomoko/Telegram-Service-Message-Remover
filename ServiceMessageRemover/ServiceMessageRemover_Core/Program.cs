using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace ServiceMessageRemover_Core {
    class Program {
        public static List<Tuple<long, int>> DeletedMessages = new List<Tuple<long, int>>();
        public static IConfigurationRoot Configuration;
        public static TelegramBotClient BotClient { get; set; }

        static void Main(string[] args) {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appconfig.json");
            Configuration = builder.Build();


            BotClient = new TelegramBotClient(Configuration["TelegramToken"]);

            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            var me = BotClient.GetMeAsync().Result;
            Console.Title = me.Username;
            BotClient.OnMessage += BotOnMessageReceived;
            Timer t = new Timer(TimerCallback, null, 0, 3600000);

            var arr = new[] { UpdateType.Message };
            BotClient.StartReceiving(arr);
            Console.ReadLine();
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {

            ConsoleColor colorBefore = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ForegroundColor = colorBefore;
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs) {
            if (messageEventArgs.Message.Type == MessageType.ChatMembersAdded || messageEventArgs.Message.Type == MessageType.ChatMemberLeft) {
                try {
                    var chatId = messageEventArgs.Message.Chat.Id;

                    if (DeletedMessages.Any(d => d.Item1 == messageEventArgs.Message.Chat.Id) && messageEventArgs.Message.NewChatMembers.Length > 0) {
                        var index = DeletedMessages.FindIndex(d => d.Item1 == messageEventArgs.Message.Chat.Id);
                        DeletedMessages[index] = Tuple.Create(DeletedMessages[index].Item1, DeletedMessages[index].Item2 + messageEventArgs.Message.NewChatMembers.Length);
                    }
                    else {
                        DeletedMessages.Add(Tuple.Create(chatId, 1));
                    }
                    await BotClient.DeleteMessageAsync(messageEventArgs.Message.Chat.Id, messageEventArgs.Message.MessageId);
                    Console.WriteLine($"{DateTime.Now} - Deleted Service Message");
                }
                //Prevent exception being thrown on previously deleted messages, sometimes they come through multiple times for some reason.
                catch (Exception e) {
                    Console.WriteLine($"{DateTime.Now} - Error: {Environment.NewLine}{Environment.NewLine} {e.Message}");
                }
            }
            else if (messageEventArgs.Message.Type == MessageType.Text) {
                if (messageEventArgs.Message.Entities.Any(d => d.Type == MessageEntityType.Url)) {
                    if (messageEventArgs.Message.Text.Contains("t.me")) {
                        await BotClient.DeleteMessageAsync(messageEventArgs.Message.Chat.Id, messageEventArgs.Message.MessageId);
                        Console.WriteLine($"Deleted Telegram Link: \n{messageEventArgs.Message.From.FirstName} - {messageEventArgs.Message.Text}");
                    }
                }
            }
        }


        public static async void TimerCallback(Object o) {
            Console.Clear();
            foreach (var item in DeletedMessages) {
                if (item.Item2 > 0) {
                    await BotClient.SendTextMessageAsync(item.Item1, $"{item.Item2} new Brainiac{(item.Item2 == 1 ? "" : "s")} joined in the last 1 hour");
                    Console.WriteLine($"{DateTime.Now} - {item.Item1} - {item.Item2} new Brainiac{(item.Item2 == 1 ? "" : "s")} joined in the last 1 hour");
                }
                DeletedMessages = new List<Tuple<long, int>>();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace ServiceMessageRemover {
    class Program {
        public static TelegramBotClient BotClient = new TelegramBotClient(""); // Your API Key Here
        public static List<Tuple<long, int>> DeletedMessages = new List<Tuple<long, int>>();
        static void Main() {
            var me = BotClient.GetMeAsync().Result;
            Console.Title = me.Username;
            BotClient.OnMessage += BotOnMessageReceived;
            Timer t = new Timer(TimerCallback, null, 0, 3600000);

            var arr = new[] { UpdateType.MessageUpdate };
            BotClient.StartReceiving(arr);
            Console.ReadLine();
        }

        public static async void TimerCallback(Object o) {
            foreach (var item in DeletedMessages) {
                if (item.Item2 > 0) {
                    await BotClient.SendTextMessageAsync(item.Item1, $"{item.Item2} new Brain{(item.Item2 == 1 ? "" : "Z")} joined in the last hour");
                    Console.WriteLine($"{item.Item1} - {item.Item2} new BrainZ joined in the last hour");
                }
                DeletedMessages = new List<Tuple<long, int>>();
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs) {
            if (messageEventArgs.Message.Type == MessageType.ServiceMessage) {
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
                    Console.WriteLine("Deleted Service Message");
                    Console.ReadLine();



                }
                //Prevent exception being thrown on previously deleted messages, sometimes they come through multiple times for some reason.
                catch { }
            }
        }
    }
}

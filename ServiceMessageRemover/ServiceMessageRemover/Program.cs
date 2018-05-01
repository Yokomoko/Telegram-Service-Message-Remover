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
        public static TelegramBotClient BotClient = new TelegramBotClient(); // Your API Key here

        static void Main() {
            var me = BotClient.GetMeAsync().Result;
            Console.Title = me.Username;
            BotClient.OnMessage += BotOnMessageReceived;

            var arr = new[] { UpdateType.MessageUpdate };
            BotClient.StartReceiving(arr);
            Console.ReadLine();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs) {
            if (messageEventArgs.Message.Type == MessageType.ServiceMessage) {
                try {
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

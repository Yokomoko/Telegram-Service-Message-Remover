using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Telegram.Bot.Args;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace GifPolice {

    internal class Program {

        #region Public Fields

        public static TelegramBotClient BotClient = new TelegramBotClient(""); // Your telegram bot id
        public static List<UserTracker> TrackedUsers = new List<UserTracker>();
        public static List<StrikeTracker> StrikedUsers = new List<StrikeTracker>();

        public const int MaxStrikeCount = 3;
        public const int DaysToRestrict = 2;

        public const int NumberofMinutes = 1;
        public const int MaxGifCount = 1;

        #endregion Public Fields

        #region Public Methods

        public static async void TimerCallback(Object o) {
            Console.Clear();
        }

        #endregion Public Methods

        #region Private Methods

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs) {
            if ((messageEventArgs.Message.Type == MessageType.Photo || messageEventArgs.Message.Type == MessageType.Sticker || messageEventArgs.Message.Type == MessageType.Document) && messageEventArgs.Message.Date >= DateTime.UtcNow.AddMinutes(-1)) {
                try {
                    var chatId = messageEventArgs.Message.Chat.Id;
                    var userId = messageEventArgs.Message.From.Id;
                    var userName = string.IsNullOrEmpty(messageEventArgs.Message.From.Username) ? messageEventArgs.Message.From.FirstName + " " + messageEventArgs.Message.From.LastName : "@" + messageEventArgs.Message.From.Username;

                    if (TrackedUsers.Any(d => d.ChatId == chatId && d.UserId == userId)) {
                        foreach (var userTracker in TrackedUsers.Where(d => d.ChatId == chatId && d.UserId == userId && d.PostDate < DateTime.UtcNow.AddMinutes(-NumberofMinutes)).ToList()) {
                            TrackedUsers.Remove(userTracker);
                        }
                    }

                    var newTracking = new UserTracker {
                        ChatId = chatId,
                        UserId = userId,
                        PostDate = messageEventArgs.Message.Date
                    };
                    TrackedUsers.Add(newTracking);

                    if (TrackedUsers.Count(d => d.ChatId == chatId && d.UserId == userId) > MaxGifCount) {
                        foreach (var strikeTracker in StrikedUsers.Where(d => d.ChatId == chatId && d.UserId == userId && d.LastStrike < DateTime.UtcNow.AddDays(-DaysToRestrict))) {
                            StrikedUsers.Remove(strikeTracker);
                        }

                        var usr = StrikedUsers.FirstOrDefault(d => d.ChatId == chatId && d.UserId == userId);

                        if (usr == null) {
                            usr = new StrikeTracker { ChatId = chatId, UserId = userId };
                            StrikedUsers.Add(usr);
                        }
                        usr.Strikes++;
                        usr.LastStrike = messageEventArgs.Message.Date;

                        await BotClient.DeleteMessageAsync(messageEventArgs.Message.Chat.Id, messageEventArgs.Message.MessageId);

                        if (usr.Strikes >= MaxStrikeCount) {
                            try {
                                await BotClient.RestrictChatMemberAsync(chatId, userId, DateTime.UtcNow.AddDays(1), true, false, false, false);
                            }
                            catch { } //Clearly a naughty admin

                            await BotClient.SendTextMessageAsync(chatId, $"{userName} - Please do not spam media messages! Max Strike Count has been reached. Media Messages Restricted.");
                        }
                        else {
                            await BotClient.SendTextMessageAsync(chatId, $"{userName} - Please do not spam media messages! {MaxStrikeCount - usr.Strikes} strike{(usr.Strikes == 2 ? "" : "s") } remaining.");
                        }
                        Console.WriteLine($"{DateTime.Now} - Deleted Media Message from {userName}");
                       
                    }
                }

                //Prevent exception being thrown on previously deleted messages, sometimes they come through multiple times for some reason.
                catch (Exception e) {
                    Console.WriteLine($"{DateTime.Now} - Error: {Environment.NewLine}{Environment.NewLine} {e.Message}");
                
                }
            }
        }

        private static void Main() {
            var me = BotClient.GetMeAsync().Result;
            Console.Title = me.Username;
            BotClient.OnMessage += BotOnMessageReceived;
            Timer t = new Timer(TimerCallback, null, 0, 600000);

            BotClient.StartReceiving();
            Console.ReadLine();
        }



        #endregion Private Methods

        public class StrikeTracker {
            public long ChatId { get; set; }
            public long UserId { get; set; }
            public byte Strikes { get; set; }
            public DateTime LastStrike { get; set; }
        }

        public class UserTracker {
            public long ChatId { get; set; }
            public long UserId { get; set; }
            public DateTime PostDate { get; set; }
        }
    }
}
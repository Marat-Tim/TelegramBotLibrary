using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Newtonsoft.Json;

namespace MyBot
{
    /// <summary>
    /// Надстройка над интерфейсом ITelegramBot для удобства использования.
    /// </summary>
    public class Bot
    {
        /// <summary>
        /// Максимальное количество символов, которое может послать метод SendMessage.
        /// </summary>
        private const int MessageMaxLength = 2000;

        /// <summary>
        /// Токен от bot father.
        /// </summary>
        private string apiToken;

        /// <summary>
        /// Набор сообщений от пользователей.
        /// Ключ - id пользователя,
        /// значение - лист со списком из кусочков сообщения.
        /// </summary>
        private Dictionary<long, List<PieceOfMessage>> messagesFromUsers;

        /// <summary>
        /// Штука, отвечающая за работу с телеграмом(из библиотеки Telegram.Bot).
        /// </summary>
        private ITelegramBotClient bot;

        /// <summary>
        /// Токен для отмены отслеживания апдейтов.
        /// </summary>
        public CancellationTokenSource CTS { get => cts; }
        private CancellationTokenSource cts;

        public Bot(string apiToken)
        {
            this.apiToken = apiToken;
            bot = new TelegramBotClient(apiToken);
        }

        /// <summary>
        /// Посылает кусок сообщения(синхронно).
        /// </summary>
        /// <param name="chatID">Чат куда нужно отправить кусок сообщения.</param>
        /// <param name="pieceOfMessage">Кусок сообщения, который нужно послать.</param>
        private void SendPieceOfMessage(long chatID, PieceOfMessage pieceOfMessage)
        {
            var task = Task.Run(() => bot.SendTextMessageAsync(chatID, JsonConvert.SerializeObject(pieceOfMessage)));
            Task.WaitAll(task);
        }

        /// <summary>
        /// Отправляет сообщение любой длины.
        /// </summary>
        /// <param name="chatID">Чат куда нужно отправить кусок сообщения.</param>
        /// <param name="text">Текст, который нужно послать.</param>
        public void SendMessage(long chatID, string text)
        {
            int i;
            for (i = 0; i < text.Length / MessageMaxLength; i++)
            {
                SendPieceOfMessage(chatID, new PieceOfMessage(
                    text[(i * MessageMaxLength)..((i + 1) * MessageMaxLength)],
                    i + 1,
                    text.Length / MessageMaxLength + 1));
            }
            SendPieceOfMessage(chatID, new PieceOfMessage(
                    text[(i * MessageMaxLength)..(^0)],
                    i + 1,
                    i + 1));
        }

        /// <summary>
        /// Запускает отслеживание апдейтов и реагирует на них.
        /// </summary>
        /// <param name="ReactionToUpdates">Реакция на апдейт.</param>
        public void StartReceiving(Action<ITelegramBotClient, string, Update, CancellationToken> ReactionToUpdates)
        {
            cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };
            bot.StartReceiving(
                (botClient, update, cancellationToken) =>
                {
                    if (update.Message != null)
                    {
                        PieceOfMessage msg = new PieceOfMessage();
                        // При получении апдейта пытаемся его десериализовать.
                        try
                        {
                            msg = JsonConvert.DeserializeObject<PieceOfMessage>(update.Message.Text);
                        }
                        catch { }
                        // Если от пользователя еще не приходило сообщений, то добавляем его в словарь.
                        if (!messagesFromUsers.ContainsKey(update.Message.From.Id))
                        {
                            messagesFromUsers.Add(update.Message.From.Id, new List<PieceOfMessage>());
                        }
                        messagesFromUsers[update.Message.From.Id].Add(msg);
                        // Если у пользователя можно собрать из кусков полное сообщение, то вызываем реакцию на него.
                        if (messagesFromUsers[update.Message.From.Id].Count == msg.TotalNumberOfMessages)
                        {
                            ReactionToUpdates(bot,
                                string.Join("", messagesFromUsers[update.Message.From.Id]),
                                update,
                                cancellationToken);
                            messagesFromUsers[update.Message.From.Id].Clear();
                        };
                    }
                },
                (botClient, exception, cancellationToken) => { },
                receiverOptions,
                CTS.Token
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MyBot
{
    /// <summary>
    /// Кусок сообщения. Нужно чтобы можно было отправлять сообщения, длинее 4096 символов(по кусочкам).
    /// </summary>
    public class PieceOfMessage
    {
        /// <summary>
        /// Текст сообщения.
        /// </summary>
        [JsonIgnore]
        public string Text => text;
        [JsonProperty("text")]
        private string text;

        /// <summary>
        /// Номер данного куска сообщения среди всех.
        /// </summary>
        [JsonIgnore]
        public int MessageNumber { get => messageNumber; }
        [JsonProperty("messageNumber")]
        private int messageNumber;

        /// <summary>
        /// Общее количество сообщений.
        /// </summary>
        [JsonProperty("totalNumberOfMessages")]
        private int totalNumberOfMessages;
        [JsonIgnore]
        public int TotalNumberOfMessages { get => totalNumberOfMessages; }

        public PieceOfMessage() { }

        public PieceOfMessage(string text, int messageNumber, int totalNumberOfMessages)
        {
            this.text = text;
            this.totalNumberOfMessages = totalNumberOfMessages;
            this.messageNumber = messageNumber;
        }
    }
}

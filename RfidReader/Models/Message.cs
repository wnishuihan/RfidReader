namespace RfidReader.Model
{
    public class Message
    {
        public Message(MessageType messageType, object content)
        {
            MessageType = messageType;
            Content = content;
        }

        public MessageType MessageType
        {
            get;
            private set;
        }

        public object Content { get; private set; }
    }

    public enum MessageType
    {
        SwitchLanguage
    }
}

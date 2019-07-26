namespace FluentTerminal.Models.Responses
{
    public class PauseTerminalOutputResponse : IMessage
    {
        public const byte Identifier = 16;

        byte IMessage.Identifier => Identifier;

        public bool Success { get; set; }
        public string ShellExecutableName { get; set; }
    }
}
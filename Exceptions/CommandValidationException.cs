namespace DreamBot.Exceptions
{
    internal class CommandValidationException(string message) : Exception
    {
        public string Message { get; private set; } = message;
    }
}
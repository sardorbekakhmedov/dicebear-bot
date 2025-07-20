namespace DicebearBot.Services;


public class DicebearBotException : Exception
{
    public DicebearBotException(string message) : base(message)
    { }

       public DicebearBotException(string message, Exception innerException) : base(message, innerException)
    { }
}

public class SendMessageException : Exception
{
    public SendMessageException(string message) : base(message)
    { }

       public SendMessageException(string message, Exception innerException) : base(message, innerException)
    { }
}
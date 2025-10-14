namespace Agdata.Rewards.Domain.Exceptions;

public sealed class DomainException : Exception
{
    public int StatusCode { get; }

    public DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

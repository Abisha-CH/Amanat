namespace Amanat.Services;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }
}

public sealed class DomainUnavailableException : Exception
{
    public DomainUnavailableException(string message) : base(message)
    {
    }
}

public sealed class DomainUnauthorizedException : Exception
{
    public DomainUnauthorizedException(string message) : base(message)
    {
    }
}

namespace Infrastructure.Authentication;

public sealed class UserContextUnavailableException : Exception
{
    public UserContextUnavailableException() : base("User context is unavailable")
    {
    }
}

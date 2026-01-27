using HotChocolate;

namespace Todo.Api.GraphQL;

public class ErrorFilter : IErrorFilter
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ErrorFilter> _logger;

    public ErrorFilter(IWebHostEnvironment env, ILogger<ErrorFilter> logger)
    {
        _env = env;
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is GraphQLException)
            return error;

        if (error.Exception is not null)
            _logger.LogError(error.Exception, "Unhandled GraphQL error: {Message}", error.Message);

        // Hide internal errors in production
        if (_env.IsProduction())
            return error.WithMessage("An unexpected error occurred");

        return error;
    }
}

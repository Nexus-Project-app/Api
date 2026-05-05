using Application.Abstractions.Messaging;
using Application.Posts.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Posts;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string? Title { get; init; }
        public string? Content { get; init; }
        public List<string>? Tags { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("posts/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdatePostCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePostCommand
            {
                PostId = id,
                Title = request.Title,
                Content = request.Content,
                Tags = request.Tags
            };

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}

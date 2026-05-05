using Application.Abstractions.Messaging;
using Application.Posts.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Posts;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("posts", async (
            Request request,
            ICommandHandler<CreatePostCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreatePostCommand
            {
                Title = request.Title,
                Content = request.Content,
                Tags = request.Tags
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Posts)
        .RequireAuthorization();
    }
}

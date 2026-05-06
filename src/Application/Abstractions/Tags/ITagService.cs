using Domain.Tags;

namespace Application.Abstractions.Tags;

public interface ITagService
{
    Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken = default);
}

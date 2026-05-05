using Application.Abstractions.Data;
using Application.Abstractions.Tags;
using Domain.Tags;
using Microsoft.EntityFrameworkCore;

namespace Application.Tags;

internal sealed class TagService(IApplicationDbContext context) : ITagService
{
    public async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken = default)
    {
        var names = tagNames
            .Select(n => n.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        List<Tag> existing = await context.Tags
            .Where(t => names.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var existingNames = existing.Select(t => t.Name).ToList();

        var created = names
            .Where(n => !existingNames.Contains(n))
            .Select(n => new Tag { Id = Guid.NewGuid(), Name = n })
            .ToList();

        if (created.Count > 0)
        {
            context.Tags.AddRange(created);
        }

        return [.. existing, .. created];
    }
}

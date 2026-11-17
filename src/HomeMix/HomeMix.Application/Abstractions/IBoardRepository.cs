using HomeMix.Domain.Boards;

namespace HomeMix.Application.Abstractions;

public interface IBoardRepository
{
    Task<WidgetBoard?> FindAsync(Guid householdId, string place, CancellationToken cancellationToken);

    void Add(WidgetBoard board);
}

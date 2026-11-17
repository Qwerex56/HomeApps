using HomeMix.Application.Abstractions;
using HomeMix.Domain.Boards;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

internal sealed class BoardRepository(HomeMixDbContext db) : IBoardRepository
{
    public Task<WidgetBoard?> FindAsync(Guid householdId, string place, CancellationToken cancellationToken) =>
        db.Boards.FirstOrDefaultAsync(b => b.HouseholdId == householdId && b.Place == place, cancellationToken);

    public void Add(WidgetBoard board) => db.Boards.Add(board);
}

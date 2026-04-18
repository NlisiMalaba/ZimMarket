using Microsoft.EntityFrameworkCore;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using Portico.Domain.Content;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class SlideRepository : ISlideRepository
{
    private readonly PorticoDbContext _db;

    public SlideRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Slide>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Slides.AsNoTracking()
            .OrderBy(slide => slide.Order)
            .ThenBy(slide => slide.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Slide?> FindAsync(int id, CancellationToken cancellationToken)
    {
        return _db.Slides.FirstOrDefaultAsync(slide => slide.Id == id, cancellationToken);
    }

    public async Task AddAsync(Slide slide, CancellationToken cancellationToken)
    {
        await _db.Slides.AddAsync(slide, cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    public void Remove(Slide slide) => _db.Slides.Remove(slide);
}

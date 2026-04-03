using Microsoft.EntityFrameworkCore;
using YourProject.API.Data;

public class FiscalYearService
{
    private readonly AppDbContext _db;
    public FiscalYearService(AppDbContext db) => _db = db;

    public async Task<bool> IsClosed(int year)
    {
        var fy = await _db.FiscalYears.FirstOrDefaultAsync(x => x.Year == year);
        return fy?.IsClosed ?? false;
    }

    public async Task CloseYear(int year)
    {
        var fy = await _db.FiscalYears.FirstOrDefaultAsync(x => x.Year == year);

        if (fy == null)
        {
            fy = new FiscalYear { Year = year, IsClosed = true };
            _db.FiscalYears.Add(fy);
        }
        else
        {
            fy.IsClosed = true;
        }

        await _db.SaveChangesAsync();
    }
    public async Task ReopenYear(int year)
    {
        var fy = await _db.FiscalYears.FirstOrDefaultAsync(x => x.Year == year);

        if (fy == null)
            return;

        fy.IsClosed = false;

        await _db.SaveChangesAsync();
    }
}
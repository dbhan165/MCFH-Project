using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

/// <summary>DbSet ScrapePackage — admin CRUD các gói scrape order. Mapping trong McfhDbContext.cs (OnModelCreating).</summary>
public partial class McfhDbContext
{
    public virtual DbSet<ScrapePackage> ScrapePackages { get; set; }
}
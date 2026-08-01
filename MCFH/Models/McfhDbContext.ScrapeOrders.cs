using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

/// <summary>DbSet ScrapeOrders — mapping đã cấu hình trong McfhDbContext.cs (OnModelCreating).</summary>
public partial class McfhDbContext
{
    public virtual DbSet<ScrapeOrder> ScrapeOrders { get; set; }
}

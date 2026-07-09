using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

public partial class McfhDbContext
{
    public virtual DbSet<PlatformCookie> PlatformCookies { get; set; }
}

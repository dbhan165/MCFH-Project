using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

/// <summary>
/// Partial mapping cho BREVO_KEYS + PAYOS_KEYS — multi-secret vault cho Brevo + PayOS.
/// Cả 2 bảng đều lưu encrypted secret, kèm flag IsDefault để service chọn row active.
/// </summary>
public partial class McfhDbContext
{
    public virtual DbSet<BrevoKey> BrevoKeys { get; set; }
    public virtual DbSet<PayOsKey> PayOsKeys { get; set; }
}

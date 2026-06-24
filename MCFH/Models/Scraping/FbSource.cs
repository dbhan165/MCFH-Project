// Models/FbSource.cs
namespace MCFH.Models;

public partial class FbSource
{
    public int FbSourceId { get; set; }
    public string GroupUrl { get; set; } = null!;
    public string? GroupName { get; set; }
    public string? Status { get; set; }
    public int AddedBy { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual User AddedByNavigation { get; set; } = null!;
}
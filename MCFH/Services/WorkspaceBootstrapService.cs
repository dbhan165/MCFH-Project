using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

/// <summary>
/// Khởi tạo WORKSPACE_CREDITS theo schema DB mới (không sửa WorkspaceService gốc).
/// </summary>
public class WorkspaceBootstrapService
{
    private readonly McfhDbContext _context;

    public const int DefaultFreeAiCredits = 100;

    public WorkspaceBootstrapService(McfhDbContext context) => _context = context;

    public async Task<bool> EnsureCreditsAsync(int workspaceId, int? aiCreditLimit = null)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId && w.IsDeleted != true);
        if (workspace == null) return false;

        var exists = await _context.WorkspaceCredits.AnyAsync(c => c.WorkspaceId == workspaceId);
        if (exists) return true;

        _context.WorkspaceCredits.Add(new WorkspaceCredit
        {
            WorkspaceId = workspaceId,
            TotalCredits = aiCreditLimit ?? DefaultFreeAiCredits,
            UsedCredits = 0,
            LastUpdated = DateTime.Now
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsMemberAsync(int workspaceId, int userId) =>
        await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
}

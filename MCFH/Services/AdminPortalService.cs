using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class AdminPortalService
{
    private readonly McfhDbContext _context;
    private readonly BespokeReportService _bespoke;

    public AdminPortalService(McfhDbContext context, BespokeReportService bespoke)
    {
        _context = context;
        _bespoke = bespoke;
    }

    public async Task<AdminDashboardDto?> GetDashboardAsync(int userId)
    {
        if (!await IsAdminAsync(userId)) return null;

        var bespoke = await _context.BespokeRequests.ToListAsync();
        var recent = await _context.BespokeRequests
            .Include(r => r.Client)
            .Include(r => r.Reporter)
            .OrderByDescending(r => r.RequestId)
            .Take(8)
            .ToListAsync();

        return new AdminDashboardDto
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalReporters = await _context.Users.CountAsync(u => u.SystemRole == "Reporter"),
            TotalClients = await _context.Users.CountAsync(u => u.SystemRole == "Client"),
            TotalWorkspaces = await _context.Workspaces.CountAsync(w => w.IsDeleted != true),
            TotalProjects = await _context.Projects.CountAsync(p => p.IsDeleted != true),
            TotalMentions = await _context.ScrapedFeedbacks.CountAsync(f => f.IsDeleted != true && f.ProjectId != null),
            PendingBespoke = bespoke.Count(r => r.Status is "pending" or "quoted"),
            InProgressBespoke = bespoke.Count(r => r.Status is "assigned" or "in_progress"),
            CompletedBespoke = bespoke.Count(r => r.Status == "completed"),
            RecentBespoke = recent.Select(r => new AdminRecentBespokeDto
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Status = r.Status ?? "pending",
                ClientName = r.Client?.FullName,
                ReporterName = r.Reporter?.FullName,
                Deadline = r.Deadline
            }).ToList()
        };
    }

    public async Task<AdminUserListDto?> ListUsersAsync(
        int adminUserId, string? search, string? role, int page = 1, int pageSize = 20)
    {
        if (!await IsAdminAsync(adminUserId)) return null;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(q) ||
                u.FullName.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            query = query.Where(u => u.SystemRole == r);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserItemDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                SystemRole = u.SystemRole,
                IsBanned = u.IsBanned == true,
                IsVerified = u.IsVerified == true,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new AdminUserListDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(int adminUserId, int targetUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return null;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == targetUserId);
        if (user == null) return null;

        var ownedWorkspaces = await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.OwnerId == targetUserId && w.IsDeleted != true)
            .Include(w => w.Projects)
            .Include(w => w.Subscriptions)
            .ThenInclude(s => s.Plan)
            .ToListAsync();

        var memberRows = await _context.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.Workspace)
            .ThenInclude(w => w.Projects)
            .Include(m => m.Workspace)
            .ThenInclude(w => w.Subscriptions)
            .ThenInclude(s => s.Plan)
            .Include(m => m.Role)
            .Where(m => m.UserId == targetUserId && m.Workspace.IsDeleted != true)
            .ToListAsync();

        var ownedIds = ownedWorkspaces.Select(w => w.WorkspaceId).ToHashSet();
        var workspaceDtos = new List<AdminUserWorkspaceDto>();

        foreach (var ws in ownedWorkspaces)
        {
            var activeSub = ws.Subscriptions
                .Where(s => s.Status == "active")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            workspaceDtos.Add(new AdminUserWorkspaceDto
            {
                WorkspaceId = ws.WorkspaceId,
                Name = ws.Name,
                MembershipRole = "Owner",
                IsOwner = true,
                ProjectCount = ws.Projects.Count(p => p.IsDeleted != true),
                SubscriptionPlan = activeSub?.Plan?.Name,
                SubscriptionStatus = activeSub?.Status,
                CreatedAt = ws.CreatedAt
            });
        }

        foreach (var row in memberRows.Where(m => !ownedIds.Contains(m.WorkspaceId)))
        {
            var ws = row.Workspace;
            var activeSub = ws.Subscriptions
                .Where(s => s.Status == "active")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            workspaceDtos.Add(new AdminUserWorkspaceDto
            {
                WorkspaceId = ws.WorkspaceId,
                Name = ws.Name,
                MembershipRole = row.Role.RoleName,
                IsOwner = false,
                ProjectCount = ws.Projects.Count(p => p.IsDeleted != true),
                SubscriptionPlan = activeSub?.Plan?.Name,
                SubscriptionStatus = activeSub?.Status,
                CreatedAt = ws.CreatedAt
            });
        }

        var workspaceIds = workspaceDtos.Select(w => w.WorkspaceId).Distinct().ToList();
        var totalProjects = workspaceIds.Count == 0
            ? 0
            : await _context.Projects.CountAsync(p =>
                workspaceIds.Contains(p.WorkspaceId) && p.IsDeleted != true);

        var bespokeClient = await _context.BespokeRequests
            .AsNoTracking()
            .Where(r => r.ClientId == targetUserId)
            .OrderByDescending(r => r.SubmittedAt ?? r.AssignedAt)
            .Take(10)
            .Select(r => new AdminUserBespokeDto
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Status = r.Status ?? "pending",
                Involvement = "Client",
                SubmittedAt = r.SubmittedAt
            })
            .ToListAsync();

        var bespokeReporter = await _context.BespokeRequests
            .AsNoTracking()
            .Where(r => r.ReporterId == targetUserId)
            .OrderByDescending(r => r.AssignedAt ?? r.SubmittedAt)
            .Take(10)
            .Select(r => new AdminUserBespokeDto
            {
                RequestId = r.RequestId,
                Title = r.Title,
                Status = r.Status ?? "pending",
                Involvement = "Reporter",
                SubmittedAt = r.AssignedAt ?? r.SubmittedAt
            })
            .ToListAsync();

        var bespokeRequests = bespokeClient
            .Concat(bespokeReporter)
            .OrderByDescending(r => r.SubmittedAt)
            .Take(10)
            .ToList();

        var recentPayments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Plan)
            .Where(p => p.CreatedBy == targetUserId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(p => new AdminUserPaymentDto
            {
                PaymentId = p.PaymentId,
                Amount = p.Amount,
                Status = p.Status,
                Type = p.Type,
                PlanName = p.Plan != null ? p.Plan.Name : null,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var bespokeAsClient = await _context.BespokeRequests.CountAsync(r => r.ClientId == targetUserId);
        var bespokeAsReporter = await _context.BespokeRequests.CountAsync(r => r.ReporterId == targetUserId);
        var unreadNotifications = await _context.Notifications.CountAsync(n =>
            n.UserId == targetUserId && n.IsRead != true);

        return new AdminUserDetailDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            AuthProvider = user.AuthProvider,
            SystemRole = user.SystemRole,
            IsBanned = user.IsBanned == true,
            IsVerified = user.IsVerified == true,
            VerifiedAt = user.VerifiedAt,
            BannedAt = user.BannedAt,
            CreatedAt = user.CreatedAt,
            Stats = new AdminUserActivityStatsDto
            {
                OwnedWorkspaces = ownedWorkspaces.Count,
                MemberWorkspaces = memberRows.Count(m => !ownedIds.Contains(m.WorkspaceId)),
                TotalProjects = totalProjects,
                BespokeAsClient = bespokeAsClient,
                BespokeAsReporter = bespokeAsReporter,
                UnreadNotifications = unreadNotifications
            },
            Workspaces = workspaceDtos.OrderByDescending(w => w.CreatedAt).ToList(),
            BespokeRequests = bespokeRequests,
            RecentPayments = recentPayments
        };
    }

    public async Task<AdminUserItemDto?> UpdateUserAsync(
        int adminUserId, int targetUserId, UpdateAdminUserDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return null;

        var user = await _context.Users.FindAsync(targetUserId);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.SystemRole))
        {
            var role = dto.SystemRole.Trim();
            if (role is "Admin" or "Reporter" or "Client")
                user.SystemRole = role;
        }

        if (dto.IsBanned.HasValue)
        {
            user.IsBanned = dto.IsBanned.Value;
            user.BannedAt = dto.IsBanned.Value ? DateTime.Now : null;
        }

        await _context.SaveChangesAsync();

        return new AdminUserItemDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            SystemRole = user.SystemRole,
            IsBanned = user.IsBanned == true,
            IsVerified = user.IsVerified == true,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<List<PortalBespokeRequestDto>> ListBespokeRequestsAsync(int adminUserId) =>
        await _bespoke.ListPortalRequestsAsync(adminUserId);

    public async Task<BespokeRequestItemDto?> AssignReporterAsync(
        int adminUserId, int requestId, int reporterId) =>
        await _bespoke.AssignReporterGlobalAsync(adminUserId, requestId, reporterId);

    public async Task<List<ReporterOptionDto>> ListReportersAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        return await _context.Users
            .Where(u => u.SystemRole == "Reporter" && u.IsBanned != true)
            .OrderBy(u => u.FullName)
            .Select(u => new ReporterOptionDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email
            })
            .ToListAsync();
    }

    public async Task<List<SystemProxyDto>> ListProxiesAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        return await _context.SystemProxies
            .OrderByDescending(p => p.LastUsedAt)
            .Select(p => new SystemProxyDto
            {
                ProxyId = p.ProxyId,
                IpAddress = p.IpAddress,
                Port = p.Port,
                AuthUser = p.AuthUser,
                Status = p.Status,
                FailCount = p.FailCount ?? 0,
                LastUsedAt = p.LastUsedAt,
                Enabled = p.Status != "disabled"
            })
            .ToListAsync();
    }

    public async Task<SystemProxyDto?> CreateProxyAsync(int adminUserId, UpsertSystemProxyDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return null;

        var proxy = new SystemProxy
        {
            IpAddress = dto.IpAddress.Trim(),
            Port = dto.Port,
            AuthUser = dto.AuthUser,
            AuthPass = dto.AuthPass,
            Status = dto.Enabled ? (dto.Status ?? "active") : "disabled",
            FailCount = 0,
            LastUsedAt = null
        };
        _context.SystemProxies.Add(proxy);
        await _context.SaveChangesAsync();

        return new SystemProxyDto
        {
            ProxyId = proxy.ProxyId,
            IpAddress = proxy.IpAddress,
            Port = proxy.Port,
            AuthUser = proxy.AuthUser,
            Status = proxy.Status,
            FailCount = 0,
            LastUsedAt = proxy.LastUsedAt,
            Enabled = proxy.Status != "disabled"
        };
    }

    public async Task<SystemProxyDto?> UpdateProxyAsync(int adminUserId, int proxyId, UpsertSystemProxyDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return null;

        var proxy = await _context.SystemProxies.FindAsync(proxyId);
        if (proxy == null) return null;

        proxy.IpAddress = dto.IpAddress.Trim();
        proxy.Port = dto.Port;
        proxy.AuthUser = dto.AuthUser;
        if (!string.IsNullOrWhiteSpace(dto.AuthPass))
            proxy.AuthPass = dto.AuthPass;
        proxy.Status = dto.Enabled ? (dto.Status ?? "active") : "disabled";
        await _context.SaveChangesAsync();

        return new SystemProxyDto
        {
            ProxyId = proxy.ProxyId,
            IpAddress = proxy.IpAddress,
            Port = proxy.Port,
            AuthUser = proxy.AuthUser,
            Status = proxy.Status,
            FailCount = proxy.FailCount ?? 0,
            LastUsedAt = proxy.LastUsedAt,
            Enabled = proxy.Status != "disabled"
        };
    }

    public async Task<bool> DeleteProxyAsync(int adminUserId, int proxyId)
    {
        if (!await IsAdminAsync(adminUserId)) return false;

        var proxy = await _context.SystemProxies.FindAsync(proxyId);
        if (proxy == null) return false;

        _context.SystemProxies.Remove(proxy);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScrapingJobDto>> ListScrapingJobsAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        return await _context.ScrapingJobs
            .Include(j => j.Project)
            .Include(j => j.Proxy)
            .OrderByDescending(j => j.StartedAt)
            .Take(100)
            .Select(j => new ScrapingJobDto
            {
                JobId = j.JobId,
                ProjectId = j.ProjectId,
                ProjectName = j.Project.Name,
                SourceId = j.SourceId,
                Status = j.Status,
                TotalScraped = j.TotalScraped ?? 0,
                ErrorLog = j.ErrorLog,
                ProxyIp = j.Proxy != null ? j.Proxy.IpAddress : null,
                StartedAt = j.StartedAt,
                FinishedAt = j.FinishedAt
            })
            .ToListAsync();
    }

    public async Task<List<SystemSettingDto>> ListSettingsAsync(int adminUserId)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        return await _context.SystemSettings
            .OrderBy(s => s.SettingKey)
            .Select(s => new SystemSettingDto
            {
                SettingId = s.SettingId,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                IsEncrypted = s.IsEncrypted == true,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SystemSettingDto>> UpdateSettingsAsync(
        int adminUserId, UpdateSystemSettingsDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        foreach (var (key, value) in dto.Settings)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    IsEncrypted = key.Contains("KEY", StringComparison.OrdinalIgnoreCase) ||
                                  key.Contains("SECRET", StringComparison.OrdinalIgnoreCase),
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = adminUserId
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = adminUserId;
            }
        }

        await _context.SaveChangesAsync();
        return await ListSettingsAsync(adminUserId);
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}

using MCFH.DTOs;
using MCFH.DTOs.ProjectDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class AdminPortalService
{
    private readonly McfhDbContext _context;
    private readonly BespokeReportService _bespoke;
    private readonly EncryptionService _encryption;

    public AdminPortalService(McfhDbContext context, BespokeReportService bespoke, EncryptionService encryption)
    {
        _context = context;
        _bespoke = bespoke;
        _encryption = encryption;
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

        var subs = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == "active" && s.Plan != null)
            .GroupBy(s => s.Plan.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        var subColors = new[] { "#111827", "#ef4444", "#3b82f6", "#10b981", "#f59e0b" };
        var subscriptionData = subs.Select((s, i) => new AdminSubscriptionChartDto
        {
            Name = s.Name,
            Value = s.Count,
            Color = subColors[i % subColors.Length]
        }).ToList();

        var jobs = await _context.ScrapingJobs
            .OrderByDescending(j => j.StartedAt)
            .Take(5)
            .ToListAsync();

        var recentJobs = jobs.Select(j => new AdminRecentJobDto
        {
            Id = j.JobId.Length > 8 ? j.JobId[..8] : j.JobId,
            Status = string.IsNullOrWhiteSpace(j.Status) ? "RUNNING" : j.Status.ToUpper(),
            Progress = (j.Status?.ToLower()) switch
            {
                "completed" => 100,
                "failed" => 100, // failed is also 100% of its lifespan but colored red in UI
                _ => 50
            }
        }).ToList();

        var proxies = await _context.SystemProxies.ToListAsync();
        var proxyOverview = proxies.Select(p => new AdminProxyHealthDto
        {
            Name = p.IpAddress,
            Health = Math.Max(0, 100 - (p.FailCount ?? 0) * 5)
        }).Take(5).ToList();

        var startDate = DateTime.UtcNow.AddMonths(-7);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);
        var now = DateTime.UtcNow;

        // Auto-sync payment status for ScrapeOrders that are paid, scraping, or completed
        var paidScrapeOrders = await _context.ScrapeOrders
            .Include(o => o.User)
            .Where(o => o.Status == "paid" || o.Status == "scraping" || o.Status == "completed")
            .AsNoTracking()
            .ToListAsync();

        var pendingPaymentIdsForPaidOrders = paidScrapeOrders
            .Where(o => o.PaymentId.HasValue)
            .Select(o => o.PaymentId!.Value)
            .ToList();

        if (pendingPaymentIdsForPaidOrders.Count > 0)
        {
            var pendingPaymentsToFix = await _context.Payments
                .Where(p => pendingPaymentIdsForPaidOrders.Contains(p.PaymentId) && p.Status != "success" && p.Status != "paid")
                .ToListAsync();

            if (pendingPaymentsToFix.Count > 0)
            {
                foreach (var p in pendingPaymentsToFix)
                {
                    p.Status = "success";
                    if (!p.PaidAt.HasValue) p.PaidAt = p.CreatedAt ?? now;
                }
                await _context.SaveChangesAsync();
            }
        }

        var allSuccessfulPayments = await _context.Payments
            .Include(p => p.CreatedByNavigation)
            .Include(p => p.Plan)
            .Include(p => p.Request)
            .Where(p => (p.Status == "success" || p.Status == "paid") && p.RequestId == null && (p.Type == null || p.Type.ToLower() == "scrape_order"))
            .AsNoTracking()
            .ToListAsync();

        var existingPaymentIds = new HashSet<int>(allSuccessfulPayments.Select(p => p.PaymentId));

        // Add paid ScrapeOrders if payment record was not in allSuccessfulPayments
        foreach (var order in paidScrapeOrders)
        {
            if (!order.PaymentId.HasValue || !existingPaymentIds.Contains(order.PaymentId.Value))
            {
                var paymentId = order.PaymentId ?? (100000 + order.OrderId);
                if (existingPaymentIds.Add(paymentId))
                {
                    allSuccessfulPayments.Add(new Payment
                    {
                        PaymentId = paymentId,
                        TransactionRef = $"SCRAPE-{order.OrderId}",
                        Amount = order.QuotedPrice,
                        Status = "success",
                        Type = "scrape_order",
                        CreatedBy = order.UserId,
                        CreatedByNavigation = order.User,
                        CreatedAt = order.PaidAt ?? order.CreatedAt,
                        PaidAt = order.PaidAt ?? order.CreatedAt
                    });
                }
            }
        }

        var totalRevenue = allSuccessfulPayments.Sum(p => p.Amount);

        var monthlyRevenue = allSuccessfulPayments
            .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value.Year == now.Year && p.CreatedAt.Value.Month == now.Month)
            .Sum(p => p.Amount);

        var prevMonth = now.AddMonths(-1);
        var prevMonthlyRevenue = allSuccessfulPayments
            .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value.Year == prevMonth.Year && p.CreatedAt.Value.Month == prevMonth.Month)
            .Sum(p => p.Amount);

        var revenueGrowthRate = prevMonthlyRevenue > 0
            ? Math.Round((double)((monthlyRevenue - prevMonthlyRevenue) / prevMonthlyRevenue * 100), 1)
            : 0;

        // Build revenue breakdown by feature (Only Scrape Order has real revenue; Bespoke is set to 0 until officially implemented)
        var scrapePayments = allSuccessfulPayments.Where(p => (p.Type ?? "scrape_order").ToLower() == "scrape_order").ToList();
        var scrapeTotal = scrapePayments.Sum(p => p.Amount);
        var scrapeCount = scrapePayments.Count;
        var scrapeAvg = scrapeCount > 0 ? scrapeTotal / scrapeCount : 0;

        var revenueByTypeGroup = new List<AdminRevenueByTypeDto>
        {
            new AdminRevenueByTypeDto
            {
                Type = "scrape_order",
                TypeName = "Tạo Dự Án Mới (Scrape Order)",
                TotalAmount = scrapeTotal,
                TransactionCount = scrapeCount,
                AverageOrderValue = scrapeAvg,
                Percentage = totalRevenue > 0 ? Math.Round((double)(scrapeTotal / totalRevenue * 100), 1) : (scrapeTotal > 0 ? 100 : 0),
                IsTopFeature = scrapeTotal > 0
            },
            new AdminRevenueByTypeDto
            {
                Type = "bespoke",
                TypeName = "Tạo Báo Cáo Chuyên Sâu (Bespoke)",
                TotalAmount = 0,
                TransactionCount = 0,
                AverageOrderValue = 0,
                Percentage = 0,
                IsTopFeature = false
            }
        };

        var revenueByPlanGroup = allSuccessfulPayments
            .GroupBy(p =>
            {
                if (p.Plan != null) return p.Plan.Name;
                var t = (p.Type ?? "").ToLower();
                if (t == "scrape_order") return "Đơn Cào Dữ Liệu Custom";
                if (t == "bespoke") return "Báo Cáo Bespoke";
                return "Gói Tiêu Chuẩn";
            })
            .Select(g => new AdminRevenueByPlanDto
            {
                Name = g.Key,
                TotalAmount = g.Sum(x => x.Amount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        var recentRevenueTransactions = allSuccessfulPayments
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .Take(10)
            .Select(p =>
            {
                var typeKey = (p.Type ?? "").ToLower();
                var featureName = typeKey switch
                {
                    "subscription" => $"Gói {p.Plan?.Name ?? "Đăng Ký System"}",
                    "scrape_order" => "Đơn cào dữ liệu custom",
                    "bespoke" => $"Báo cáo Bespoke #{p.RequestId}",
                    _ => "Tính năng hệ thống"
                };

                return new AdminRecentRevenueTransactionDto
                {
                    PaymentId = p.PaymentId,
                    TransactionRef = !string.IsNullOrEmpty(p.TransactionRef) 
                        ? p.TransactionRef 
                        : (p.OrderCode.HasValue ? $"PAYOS-{p.OrderCode}" : $"PAY-{p.PaymentId}"),
                    UserName = p.CreatedByNavigation?.FullName ?? "User Hệ Thống",
                    UserEmail = p.CreatedByNavigation?.Email ?? "N/A",
                    FeatureName = featureName,
                    Type = typeKey,
                    Amount = p.Amount,
                    Status = "Thành công",
                    PaidAt = p.PaidAt ?? p.CreatedAt
                };
            })
            .ToList();

        var revenueQuery = allSuccessfulPayments
            .Where(p => p.CreatedAt >= startDate)
            .GroupBy(p => new { Year = p.CreatedAt!.Value.Year, Month = p.CreatedAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
            .ToList();

        var userQuery = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => new { Year = u.CreatedAt!.Value.Year, Month = u.CreatedAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        var revenueGrowth = new List<AdminRevenueChartDto>();
        for (int i = 0; i < 8; i++)
        {
            var d = startDate.AddMonths(i);
            var rev = revenueQuery.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Total ?? 0;
            var usrs = userQuery.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0;
            revenueGrowth.Add(new AdminRevenueChartDto
            {
                Month = $"Tháng {d.Month}",
                Revenue = (int)rev,
                Users = usrs
            });
        }

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
            }).ToList(),
            RevenueGrowth = revenueGrowth,
            SubscriptionData = subscriptionData,
            RecentJobs = recentJobs,
            ProxyHealthOverview = proxyOverview,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            RevenueGrowthRate = revenueGrowthRate,
            RevenueByType = revenueByTypeGroup,
            RevenueByPlan = revenueByPlanGroup,
            RecentRevenueTransactions = recentRevenueTransactions
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

        var settings = await _context.SystemSettings
            .OrderBy(s => s.SettingKey)
            .ToListAsync();

        return settings.Select(s => new SystemSettingDto
        {
            SettingId = s.SettingId,
            SettingKey = s.SettingKey,
            SettingValue = s.IsEncrypted == true ? "********" : s.SettingValue,
            IsEncrypted = s.IsEncrypted == true,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }

    public async Task<List<SystemSettingDto>> UpdateSettingsAsync(
        int adminUserId, UpdateSystemSettingsDto dto)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        foreach (var (key, value) in dto.Settings)
        {
            if (value == "********") continue;

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);
                
            bool isEncryptedKey = key.Contains("KEY", StringComparison.OrdinalIgnoreCase) ||
                                  key.Contains("SECRET", StringComparison.OrdinalIgnoreCase);
                                  
            string finalValue = isEncryptedKey ? _encryption.Encrypt(value) : value;

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = finalValue,
                    IsEncrypted = isEncryptedKey,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = adminUserId
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = finalValue;
                setting.IsEncrypted = isEncryptedKey;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = adminUserId;
            }
        }

        await _context.SaveChangesAsync();
        return await ListSettingsAsync(adminUserId);
    }

    public async Task<List<AdminAuditLogDto>> GetAuditLogsAsync(int adminUserId, int limit = 50)
    {
        if (!await IsAdminAsync(adminUserId)) return new();

        var logs = new List<AdminAuditLogDto>();

        // 1. Payment audit logs
        var payments = await _context.Payments
            .Include(p => p.CreatedByNavigation)
            .Where(p => p.Status == "success" || p.Status == "paid")
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        foreach (var p in payments)
        {
            var user = p.CreatedByNavigation;
            var refText = !string.IsNullOrEmpty(p.TransactionRef) 
                ? p.TransactionRef 
                : (p.OrderCode.HasValue ? $"PAYOS-{p.OrderCode}" : $"PAY-{p.PaymentId}");

            logs.Add(new AdminAuditLogDto
            {
                Action = "Thanh toán đơn cào thành công",
                Description = $"Xác nhận biến động số dư +{p.Amount:N0} ₫ (Mã GD: {refText})",
                ActorName = user?.FullName ?? "Hệ thống PayOS",
                ActorEmail = user?.Email ?? "system@payos.vn",
                Category = "PAYMENT",
                Severity = "success",
                Timestamp = p.PaidAt ?? p.CreatedAt ?? DateTime.Now
            });
        }

        // 2. User registration audit logs
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        foreach (var u in users)
        {
            logs.Add(new AdminAuditLogDto
            {
                Action = "Tài khoản mới đăng ký",
                Description = $"Thành viên [{u.FullName}] ({u.Email}) gia nhập hệ thống với vai trò [{u.SystemRole}]",
                ActorName = u.FullName,
                ActorEmail = u.Email,
                Category = "USER",
                Severity = "info",
                Timestamp = u.CreatedAt ?? DateTime.Now
            });
        }

        // 3. Scrape orders audit logs
        var orders = await _context.ScrapeOrders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        foreach (var o in orders)
        {
            var user = o.User;
            logs.Add(new AdminAuditLogDto
            {
                Action = "Khởi tạo dự án cào dữ liệu",
                Description = $"Đơn cào [Từ khóa: {o.Keyword ?? "Mặc định"}] (Giá: {o.QuotedPrice:N0} ₫, Trạng thái: {o.Status})",
                ActorName = user?.FullName ?? "Khách hàng",
                ActorEmail = user?.Email ?? "N/A",
                Category = "PROJECT",
                Severity = o.Status == "completed" ? "success" : "info",
                Timestamp = o.CreatedAt
            });
        }

        // 4. System settings audit logs
        var settings = await _context.SystemSettings
            .Where(s => s.UpdatedAt.HasValue)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        foreach (var s in settings)
        {
            logs.Add(new AdminAuditLogDto
            {
                Action = "Cấu hình tham số hệ thống",
                Description = $"Khóa cấu hình [{s.SettingKey}] vừa được cập nhật giá trị bảo mật mới",
                ActorName = "System Administrator",
                ActorEmail = "admin@mcfh.com",
                Category = "SYSTEM",
                Severity = "warning",
                Timestamp = s.UpdatedAt ?? DateTime.Now
            });
        }

        var result = logs
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .Select((l, index) =>
            {
                l.LogId = index + 1;
                return l;
            })
            .ToList();

        return result;
    }

    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null &&
               user.SystemRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}

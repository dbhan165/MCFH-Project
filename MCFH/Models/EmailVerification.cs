using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class EmailVerification
{
    public int VerificationId { get; set; }

    public int UserId { get; set; }

    public string? OtpCode { get; set; }

    public string? VerificationToken { get; set; }

    public DateTime ExpiredAt { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class PasswordResetToken
{
    public int TokenId { get; set; }

    public int UserId { get; set; }

    public string ResetToken { get; set; } = null!;

    public DateTime ExpiredAt { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

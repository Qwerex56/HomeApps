using System.ComponentModel.DataAnnotations;
using AccountManagement.Enums;

namespace AccountManagement.Models;

public class Account {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [MaxLength(128)]
    public string ServiceName { get; set; } = string.Empty;
    public SyncStatusEnum SyncStatus { get; set; }
    public DateTime LastSync { get; set; }
    
    public required Guid UserId { get; set; } // foreign key
    public User User { get; set; } = null!;
}
using AccountManagement.Enums;

namespace AccountManagement.Models;

public class Account {
    public int AccountId { get; set; }
    public required string ServiceName { get; set; }
    public SyncStatusEnum SyncStatus { get; set; }
    public DateTime LastSync { get; set; }
    
    public required int UserId { get; set; } // foreign key
    public required User User { get; set; } 
}
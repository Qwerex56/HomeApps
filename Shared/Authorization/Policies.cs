namespace Shared.Authorization;

public static class Policies {
    public const string RequireSystemAdmin = "RequireSystemAdmin";
    public const string RequireFamilyAdmin = "RequireFamilyAdmin";
    public const string RequireFamilyMember = "RequireFamilyMember";
    public const string RequireGuest = "RequireGuest";
}
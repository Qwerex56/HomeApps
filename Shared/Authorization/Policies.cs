namespace Shared.Authorization;

public static class Policies {
    public const string RequireSystemOwner = "RequireSystemOwner";
    public const string RequireSystemAdmin = "RequireSystemAdmin";
    public const string RequireSystemMember = "RequireSystemMember";
}
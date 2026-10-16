namespace Shared.Authorization;

public enum UserFamilyRoleEnum {
    FamilyOwner,
    FamilyAdmin,
    FamilyMember,
    Guest
}

public static class UserFamilyRoleExtension {
    public static bool IsMorePrivilegedRole(this UserFamilyRoleEnum userFamilyRole, UserFamilyRoleEnum other) {
        return userFamilyRole < other;
    }

    public static bool IsAtLeastRole(this UserFamilyRoleEnum userFamilyRole, UserFamilyRoleEnum isAtLeastRole) {
        return userFamilyRole <= isAtLeastRole;
    }
}
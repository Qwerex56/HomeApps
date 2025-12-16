using Microsoft.AspNetCore.Authorization;

namespace Shared.Authorization.Extensions;

public static class AuthorizationExtensions {
    public static void AddAllCustomPolicies(this AuthorizationOptions options) {
        options.AddSystemAdminPolicy();
        options.AddFamilyAdminPolicy();
        options.AddMemberPolicy();
        options.AddGuestPolicy();
    }
    
    public static void AddSystemAdminPolicy(this AuthorizationOptions options) {}
    
    public static void AddFamilyAdminPolicy(this AuthorizationOptions options) {}
    
    public static void AddMemberPolicy(this AuthorizationOptions options) {}

    public static void AddGuestPolicy(this AuthorizationOptions options) {
        // probably for logged users
    }
}
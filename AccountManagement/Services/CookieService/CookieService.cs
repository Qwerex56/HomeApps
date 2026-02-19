using AccountManagement.Options;

namespace AccountManagement.Services.CookieService;

public class CookieService : ICookieService {
    private readonly CookieOptionsConfig _cookieOptionsConfig;

    public CookieService(CookieOptionsConfig cookieOptionsConfig) {
        _cookieOptionsConfig = cookieOptionsConfig;
    }

    public void SetRefreshToken(HttpResponse response, string refreshToken, DateTime refreshTokenExpires) {
        var cookieOptions = new CookieOptions {
            HttpOnly = _cookieOptionsConfig.HttpOnly,
            Secure = _cookieOptionsConfig.Secure,
            Path = _cookieOptionsConfig.Path,
            Expires = refreshTokenExpires,
            Domain = _cookieOptionsConfig.Domain,
            SameSite = _cookieOptionsConfig.SameSite switch {
                "Lax" => SameSiteMode.Lax,
                "Strict" => SameSiteMode.Strict,
                "None" => SameSiteMode.None,
                _ => SameSiteMode.Unspecified
            }
        };
        
        response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
    
    public void RemoveRefreshToken(HttpResponse response) {
        var cookieOptions = new CookieOptions {
            HttpOnly = _cookieOptionsConfig.HttpOnly,
            Secure = _cookieOptionsConfig.Secure,
            Path = _cookieOptionsConfig.Path,
            Domain = _cookieOptionsConfig.Domain,
            SameSite = _cookieOptionsConfig.SameSite switch {
                "Lax" => SameSiteMode.Lax,
                "Strict" => SameSiteMode.Strict,
                "None" => SameSiteMode.None,
                _ => SameSiteMode.Unspecified
            }
        };
        
        response.Cookies.Delete("refreshToken");
    }
}
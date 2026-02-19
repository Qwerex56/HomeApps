namespace AccountManagement.Services.CookieService;

public interface ICookieService {
    public void SetRefreshToken(HttpResponse response, string refreshToken, DateTime refreshTokenExpires);
    public void RemoveRefreshToken(HttpResponse response);
}
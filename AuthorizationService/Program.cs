using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Auth__Google__ClientId"];
        options.ClientSecret = builder.Configuration["Auth__Google__ClientSecret"];
        options.CallbackPath = "/signin-google";
    });

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidIssuer = builder.Configuration["Auth__Jwt__Issuer"],
//             
//             ValidateAudience = true,
//             ValidAudience = builder.Configuration["Auth__Jwt__Audience"],
//             
//             ValidateLifetime = true,
//             IssuerSigningKey = new SymmetricSecurityKey(
//                 Encoding.UTF8.GetBytes(builder.Configuration["Auth__Jwt__Key"])),
//         };
//     });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


/*
 * 1. Setup google sign-in in Services
 * 2. Add login scheme in one endpoint e.g. .../login
 * 3. Add api end-point to which user will be redirected after successful login
 *
 */
// Endpoint do wywołania logowania
// app.MapGet("/login", async (HttpContext context) =>
// {
//     await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
//         new AuthenticationProperties { RedirectUri = "/me" });
// });
//
// // Endpoint callbacku — pokaże dane użytkownika
// app.MapGet("/me", (HttpContext context) =>
// {
//     var user = context.User;
//     return Results.Json(new
//     {
//         Name = user.Identity?.Name,
//         Email = user.FindFirst(ClaimTypes.Email)?.Value
//     });
// });

app.MapControllers();

app.Run();
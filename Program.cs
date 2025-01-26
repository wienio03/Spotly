using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spotly.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 5001;
});
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient<SpotifyAuthService>();
builder.Services.AddHttpClient<SpotifyAuthService>();
builder.Services.AddSingleton<SpotifyApiService>();
builder.Services.AddSingleton<SpotifyAuthService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

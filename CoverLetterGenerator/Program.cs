// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CoverLetterApp.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using System.Net.Http.Headers;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IOpenAiService, OpenAiService>();

var app = builder.Build();

builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    Env.Load();  // Load environment variables from .env
    builder.Configuration.AddEnvironmentVariables();  // Add environment variables to the configuration
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CoverLetter}/{action=Index}/{id?}");

app.Run();

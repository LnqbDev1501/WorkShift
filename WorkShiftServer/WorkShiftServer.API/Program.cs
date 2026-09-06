using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Services.Auth;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Repositories.Admin;
using WorkShiftServer.API.Hubs;
using WorkShiftServer.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MyCnn")));



builder.Services.AddHttpClient<AuthService>(client =>
{
    var supabaseUrl = builder.Configuration["Supabase:Url"]
        ?? throw new InvalidOperationException("Missing Supabase:Url configuration.");
    var serviceRoleKey = builder.Configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Missing Supabase:ServiceRoleKey configuration.");

    client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/");
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceRoleKey);
}).AddTypedClient<IAuthService>((client, _) =>
{
    var setPasswordUrl = builder.Configuration["AdminUi:SetPasswordUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:SetPasswordUrl configuration.");
    return new AuthService(client, setPasswordUrl);
});




builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IAccountSetupEmailSender, ResendAccountSetupEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<IRegionRepo, RegionRepo>();
builder.Services.AddScoped<IRegionService, RegionService>();
builder.Services.AddScoped<IBranchRepo, BranchRepo>();
builder.Services.AddScoped<IBranchService, BranchService>();

builder.Services.AddScoped<ShiftTemplateService>();
builder.Services.AddScoped<IDashboardRepo, DashboardRepo>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationRepo, NotificationRepo>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient<INotificationEmailSender, ResendNotificationEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

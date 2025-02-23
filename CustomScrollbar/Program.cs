/*var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost8080",
        builder => builder.WithOrigins("http://localhost:8080")
                            .AllowCredentials()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS middleware
app.UseCors("AllowLocalhost8080");

// Use session middleware
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();*/

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔧 Allow multiple front-end origins (adjust if necessary)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy => policy.WithOrigins("http://localhost:8080", "http://localhost:3000") // Allow both ports
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// 🔧 Configure session settings (Ensure `SameSite=None` and `Secure=true` for cross-origin support)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None; // Required for cross-origin cookies
    //options.Cookie.Secure = true; // Must be true for `SameSite=None` to work
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 🔧 Apply CORS before session
app.UseCors("AllowLocalhost");

// 🔧 Ensure session middleware is executed early
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();

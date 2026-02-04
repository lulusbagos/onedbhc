using FluentFTP;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using one_db.Data;
using one_db.Models; // ✅ untuk FtpConfig

var builder = WebApplication.CreateBuilder(args);

// =============================
// 🧠 1. Database Connections
// =============================

// ✅ MSSQL
builder.Services.AddDbContext<AppDBContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ MySQL
builder.Services.AddDbContext<MySqlDBContext>(options =>
	options.UseMySql(
		builder.Configuration.GetConnectionString("OneDbConnection"),
		new MySqlServerVersion(new Version(8, 0, 21))
	)
);

// =============================
// 🧠 2. FTP Configuration ✅
// =============================
builder.Services.Configure<FtpConfig>(builder.Configuration.GetSection("FtpSettings"));

// =============================
// 🧠 3. Authentication (Cookie)
// =============================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Login/Index";
		options.AccessDeniedPath = "/Login/Index";
		options.Cookie.Name = "OneDBAuth";
		options.ExpireTimeSpan = TimeSpan.FromHours(4); // ⏱ Cookie aktif 4 jam
		options.SlidingExpiration = true; // 🔁 Perpanjang otomatis kalau aktif
	});

// =============================
// 🧠 4. Session
// =============================
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromHours(4); // ⏱ Sama dengan cookie biar gak mismatch
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// =============================
// 🧠 5. MVC + View
// =============================
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(); // Pastikan Anda juga punya ini
builder.Services.AddHttpClient();
var app = builder.Build();

// =============================
// 🌐 6. Middleware Pipeline
// =============================
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ 1️⃣ Jalankan session DULU sebelum auth
app.UseSession();

// ✅ 2️⃣ Baru auth
app.UseAuthentication();

// ✅ 3️⃣ Baru authorization
app.UseAuthorization();

// =============================
// 🧩 7. Optional: Debug Middleware (hapus nanti)
// =============================
// Bisa bantu pantau session yang aktif
app.Use(async (context, next) =>
{
	var sessionId = context.Session.Id;
	var user = context.User?.Identity?.Name;
	var kategori = context.Session.GetString("kategori_user_id");
	Console.WriteLine($"[DEBUG] SessionId={sessionId} | User={user} | Kategori={kategori}");
	await next();
});

// =============================
// 📌 8. Routing
// =============================
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Login}/{action=Index}/{id?}");

// Jalankan aplikasi
app.Run();

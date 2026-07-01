using Microsoft.EntityFrameworkCore;
using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using EliraFashionBoutique.Repositories.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EliraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IColorRepository, ColorRepository>();
builder.Services.AddScoped<ISizeRepository, SizeRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context => 
            !context.User.IsInRole("Customer") && !context.User.IsInRole("Supplier"))
        .Build();

    options.AddPolicy("AdminAccess", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  !context.User.IsInRole("Customer") && 
                  !context.User.IsInRole("Supplier") && 
                  !context.User.IsInRole("Sales Manager")));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EliraDbContext>();
    context.Database.EnsureCreated();

    if (!context.Users.Any(u => u.Email == "admin@elira.com"))
    {
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

        var admin = new User
        {
            Email = "admin@elira.com",
            RoleName = "Admin",
            IsEmailVerified = true,
            CreatedAt = DateTime.Now
        };
        admin.Password = passwordHasher.HashPassword(admin, "password123");
        context.Users.Add(admin);

        var customerUser = new User
        {
            Email = "customer@elira.com",
            RoleName = "Customer",
            IsEmailVerified = true,
            CreatedAt = DateTime.Now
        };
        customerUser.Password = passwordHasher.HashPassword(customerUser, "password123");
        customerUser.Customer = new Customer
        {
            FullName = "Test Customer",
            PhoneNo = "03001234567",
            Address = "123 Customer Lane",
            City = "Lahore",
            Country = "Pakistan"
        };
        context.Users.Add(customerUser);

        var supplierUser = new User
        {
            Email = "supplier@elira.com",
            RoleName = "Supplier",
            IsEmailVerified = true,
            CreatedAt = DateTime.Now
        };
        supplierUser.Password = passwordHasher.HashPassword(supplierUser, "password123");
        supplierUser.Supplier = new Supplier
        {
            SupplierName = "Boutique Fabrics Ltd",
            ContactPerson = "John Doe",
            Phone = "03217654321",
            Address = "456 Silk Road",
            Category = "Fabrics"
        };
        context.Users.Add(supplierUser);

        context.SaveChanges();
    }

    if (!context.Users.Any(u => u.Email == "salesmanager@elira.com"))
    {
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var salesManager = new User
        {
            Email = "salesmanager@elira.com",
            RoleName = "Sales Manager",
            IsEmailVerified = true,
            CreatedAt = DateTime.Now
        };
        salesManager.Password = passwordHasher.HashPassword(salesManager, "password123");
        context.Users.Add(salesManager);
        context.SaveChanges();
    }
}

app.Run();

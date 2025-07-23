using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RotAndRuin.Application.Services;
using RotAndRuin.Infrastructure.Data;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Infrastructure.Services;
using DotNetEnv;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RotAndRuin.Application.Configuration;
using Stripe;
using File = System.IO.File;
using ICloudStorageService = RotAndRuin.Application.Interfaces.ICloudStorageService;
using ProductService = RotAndRuin.Application.Services.ProductService;


// AWS and ImageSharp.Web imports
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Providers.AWS;
using SixLabors.ImageSharp.Web; 
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Resolvers;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

Env.Load();

// Add JWT Authentication configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")))
        };
    });

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
Console.WriteLine($"JWT Key length: {jwtKey?.Length ?? 0}"); // Debugging
Console.WriteLine($"JWT Key: {jwtKey}"); // Debugging

// Services
builder.Services.AddControllers();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "RotAndRuin API", 
        Version = "v1" 
    });
    c.EnableAnnotations();
    // Add JWT authentication support to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGciOiJIUzI1NiIsIn....'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddHealthChecks();
builder.Services.AddScoped<ICloudStorageService, AwsS3Service>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddAutoMapper(typeof(RotAndRuin.Application.DTOs.ProductDto).Assembly);
builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAWSService<IAmazonS3>(new AWSOptions
{
    Credentials = new BasicAWSCredentials(
        Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"),
        Environment.GetEnvironmentVariable("AWS_SECRET_KEY")
    ),
    Region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1")
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddImageSharp()
    .Configure<ImageSharpMiddlewareOptions>(options =>
    {
        options.BrowserMaxAge = TimeSpan.FromDays(7);
        options.CacheMaxAge = TimeSpan.FromDays(30);
    })
    .RemoveProvider<PhysicalFileSystemProvider>()
    .AddProvider<AWSS3StorageImageProvider>()
    .Configure<AWSS3StorageImageProviderOptions>(options =>
    {
        var bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

        options.S3Buckets = new[]
        {
            new AWSS3BucketClientOptions
            {
                BucketName = bucketName,
                Region = region,
                AccessKey = accessKey,
                AccessSecret = secretKey,
                Endpoint = $"https://s3.{region}.amazonaws.com"
            }
        };
    });
builder.Services.RemoveAll<IImageCache>();
builder.Services.AddSingleton<IImageCache, CustomMemoryCache>();

builder.Services.AddScoped<IImageUrlService, ImageUrlService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    var origins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',') ?? Array.Empty<string>();
    
    options.AddPolicy("ImagePolicy", builder =>
    {
        builder
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromDays(1));
        // builder
        //     .AllowAnyOrigin()
        //     .AllowAnyMethod()
        //     .AllowAnyHeader()
        //     .SetPreflightMaxAge(TimeSpan.FromDays(1));
    });
    
    options.AddPolicy("FrontendPolicy", builder =>
    {
        builder
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromDays(1));
        
        // builder
        //     .WithOrigins(
        //         "https://rotandruin.com",
        //         "https://rot-and-ruin-fe-git-main-kris-projects-5da34323.vercel.app",
        //         "http://localhost:5173")
        //     .AllowAnyMethod()
        //     .AllowAnyHeader()
        //     .AllowCredentials()
        //     .SetPreflightMaxAge(TimeSpan.FromDays(1));
    });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// builder.Services.AddDataProtection()
//     .SetApplicationName("RotAndRuin")
//     .PersistKeysToFileSystem(new DirectoryInfo("/tmp/keys"));
builder.Services.AddDataProtection()
    .SetApplicationName("RotAndRuin")
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddHealthChecks();

// Configure Stripe
// Configure Stripe
builder.Services.Configure<StripeSettings>(options => 
{
    options.PublishableKey = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY");
    options.SecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
    options.WebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
    // options.Currency = Environment.GetEnvironmentVariable("STRIPE_CURRENCY");
});
StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RotAndRuin API V1");
        options.DefaultModelsExpandDepth(2);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.IndexStream = () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>RotAndRuin API - Swagger UI</title>
                <link rel='stylesheet' href='https://unpkg.com/swagger-ui-dist@3.52.5/swagger-ui.css'>
            </head>
            <body>
                <div id='swagger-ui'></div>
                <script src='https://unpkg.com/swagger-ui-dist@3.52.5/swagger-ui-bundle.js'></script>
                <script src='https://unpkg.com/swagger-ui-dist@3.52.5/swagger-ui-standalone-preset.js'></script>
                <script>
                    window.onload = function() {
                        const ui = SwaggerUIBundle({
                            url: '/swagger/v1/swagger.json',
                            dom_id: '#swagger-ui',
                            deepLinking: true,
                            presets: [
                                SwaggerUIBundle.presets.apis,
                                SwaggerUIStandalonePreset
                            ],
                            plugins: [
                                SwaggerUIBundle.plugins.DownloadUrl
                            ],
                            layout: 'StandaloneLayout'
                        });
                        window.ui = ui;
                    };
                </script>
            </body>
            </html>
        "));
    });
}

// Add ImageSharp middleware early in the pipeline


// Keep your other middleware configurations
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseImageSharp();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseCors("ImagePolicy");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
}

app.Run();

public class NullResolver : IImageCacheResolver
{
    public static readonly NullResolver Instance = new();

    public Task<Stream> OpenReadAsync() => Task.FromResult<Stream>(Stream.Null);

    public Task<ImageCacheMetadata> GetMetaDataAsync()
    {
        return Task.FromResult(new ImageCacheMetadata(
            sourceLastWriteTimeUtc: DateTime.UtcNow,
            cacheLastWriteTimeUtc: DateTime.UtcNow,
            contentType: "application/octet-stream",
            cacheControlMaxAge: TimeSpan.FromDays(7),
            contentLength: 0));
    }
}

public class MemoryCacheResolver : IImageCacheResolver
{
    private readonly CachedImage _cached;

    public MemoryCacheResolver(CachedImage cached)
    {
        _cached = cached;
    }

    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(new MemoryStream(_cached.Data));
    }

    public Task<ImageCacheMetadata> GetMetaDataAsync()
    {
        return Task.FromResult(_cached.Metadata);
    }
}

public class CachedImage
{
    public byte[] Data { get; set; }
    public ImageCacheMetadata Metadata { get; set; }

    public CachedImage()
    {
        Data = Array.Empty<byte>();
        Metadata = new ImageCacheMetadata(
            sourceLastWriteTimeUtc: DateTime.UtcNow,
            cacheLastWriteTimeUtc: DateTime.UtcNow,
            contentType: "image/jpeg", // Default content type
            cacheControlMaxAge: TimeSpan.FromDays(30),
            contentLength: 0);
    }
}

public class CustomMemoryCache : IImageCache
{
    private readonly IMemoryCache _memoryCache;

    public CustomMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<IImageCacheResolver> GetAsync(string key)
    {
        if (_memoryCache.TryGetValue(key, out CachedImage cached))
        {
            return Task.FromResult<IImageCacheResolver>(
                new MemoryCacheResolver(cached));
        }

        return Task.FromResult<IImageCacheResolver>(NullResolver.Instance);
    }

    public Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        
        var cached = new CachedImage
        {
            Data = ms.ToArray(),
            Metadata = metadata
        };

        _memoryCache.Set(key, cached, 
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromDays(30)
            });
        
        return Task.CompletedTask;
    }
}

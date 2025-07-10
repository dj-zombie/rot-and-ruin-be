using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RotAndRuin.Application.Services;
using RotAndRuin.Infrastructure.Data;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Infrastructure.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using DotNetEnv;
using ICloudStorageService = RotAndRuin.Application.Interfaces.ICloudStorageService;


var builder = WebApplication.CreateBuilder(args);

Env.Load();

// Add services
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
    // c.OperationFilter<AddFileUploadParams>();
    c.EnableAnnotations();
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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
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

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();



public class AddFileUploadParams : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath.Contains("products") && context.ApiDescription.HttpMethod == "POST")
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "multipart/form-data", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    { 
                                        "Product", new OpenApiSchema 
                                        { 
                                            Type = "object",
                                            Properties = new Dictionary<string, OpenApiSchema>
                                            {
                                                { "Name", new OpenApiSchema { Type = "string" } },
                                                { "Brand", new OpenApiSchema { Type = "string" } },
                                                { "Description", new OpenApiSchema { Type = "string" } },
                                                { "Price", new OpenApiSchema { Type = "number", Format = "decimal" } },
                                                { "ShippingPrice", new OpenApiSchema { Type = "number", Format = "decimal" } },
                                                { "ProductVisible", new OpenApiSchema { Type = "boolean" } },
                                                { "StockLevel", new OpenApiSchema { Type = "integer" } },
                                                { "CategoryDetails", new OpenApiSchema { Type = "string" } },
                                                { "MetaKeywords", new OpenApiSchema { Type = "string" } },
                                                { "MetaDescription", new OpenApiSchema { Type = "string" } },
                                                { "ProductUrl", new OpenApiSchema { Type = "string" } }
                                            }
                                        } 
                                    },
                                    { "Files", new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string", Format = "binary" } } },
                                    { "IsFeaturedFileNames", new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string" } } }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
global using FileUploadShared;
using FileUploadApi;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AzureStorageHelper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Handle uploadFile API POST calls
app.MapPost("/uploadFile", async (HttpRequest httpRequest) =>
{
    if (!httpRequest.HasFormContentType)
    {
        return Results.BadRequest();
    }

    try
    {
        // Get form collection from the HttpRequest
        var formCollection = await httpRequest.ReadFormAsync();

        // Get the iFormFile
        var iFormFile = formCollection.Files["fileContent"];

        // Make sure is not null and the lenght is not zero
        if (iFormFile is null || iFormFile.Length == 0)
        {
            return Results.BadRequest();
        }

        // Get a stream for reading
        using var stream = iFormFile.OpenReadStream();

        // Create file path
        var localFilePath = Path.Combine("Files", iFormFile.FileName);

        // Get a stream for writing
        using var localFileStream = File.OpenWrite(localFilePath);

        // Write file to disk
        await stream.CopyToAsync(localFileStream);

        // Return result
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex.Message);
        return Results.BadRequest();
    }
})
.Produces(StatusCodes.Status201Created)
.WithName("UploadFile")
.WithOpenApi();

app.MapControllers();

app.Run();
using FileUploadShared;
using System.Net.Http.Headers;

namespace FileUploadInMaui;

public partial class MainPage : ContentPage
{
    public bool UploadingFile = false;
    public long UploadedBytes;
    public long TotalBytes;
    private FilesManager filesManager;
    private HttpClient httpClient;
    private bool UploadToAzure = true;
    private string ContainerName = "mauiuploads";

    public MainPage()
    {
        InitializeComponent();
        this.Loaded += MainPage_Loaded;
        // Create a HttpClient to upload the file to the server
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
            //BaseAddress = new Uri("https://[YOUR-APP-NAME].azurewebsites.net/")
            BaseAddress = new Uri("https://localhost:7094/") 

            // To test locally on Windows, change the base address to localhost with your
            // local https port number from Properties/launchSettings.json
        };
        filesManager = new FilesManager(httpClient);
    }

    private void MainPage_Loaded(object sender, EventArgs e)
    {
#if WINDOWS
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        // Center the window
        Window.X = (displayInfo.Width / displayInfo.Density - Window.Width) / 2;
        Window.Y = (displayInfo.Height / displayInfo.Density - Window.Height) / 2;
#endif
    }

    /// <summary>
    /// Called by the user clicking on the upload button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnFilePickerClicked(object sender, EventArgs e)
    {
        try
        {
            // Setup pick options
            PickOptions options = new()
            {
                PickerTitle = "Please select a file to upload"
            };

            // Display file picker
            var fileResult = await FilePicker.Default.PickAsync(options);

            // If a file is select it process the file
            if (fileResult != null)
            {
                // Proccess file
                //var result = await ProcessFile(fileResult);

                // Upload Large File
                var result = await UploadLargeFile(fileResult.FullPath);

                // Display result
                if (result)
                    await DisplayAlert("File Uploaded Successfully.", "The file has been uploaded successfully.", "OK");
                else
                    await DisplayAlert("An error has occurred.", "The file has not been uploaded successfully.", "OK");
            }
        }
        catch
        {
            // The user canceled or something went wrong
        }
    }

    private async Task<bool> ProcessFile(FileResult fileResult)
    {
        if (fileResult == null)
        {
            return false;
        }

        // Open selected file
        using var fileStream = File.OpenRead(fileResult.FullPath);

        byte[] bytes;

        // Create a memory stream to get the file's bytes
        using (var memoryStream = new MemoryStream())
        {
            // NOTE: It is not a good idea in general to load an entire file into memory.
            //       This demo will only work with smaller files. 
            //       In another episode, we will create a more efficient demo that
            //       will upload files of any size and show a progress bar.
            await fileStream.CopyToAsync(memoryStream);
            bytes = memoryStream.ToArray();
        }

        // Create a ByteArrayContent and set the header's content type to multipart/form-data
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

        // Create a MultipartFormDataContent and set the fileContent
        using var form = new MultipartFormDataContent
        {
            { fileContent, "fileContent", Path.GetFileName(fileResult.FullPath) }
        };

        // Upload file to the server
        return await UploadFile(form);
    }

    public async Task<bool> UploadLargeFile(string fullPath)
    {
        UploadedBytes = 0;

        // Disable the file input field
        UploadingFile = true;

        // calculate the chunks we have to send
        var info = new FileInfo(fullPath);
        TotalBytes = info.Length;
        long percent = 0;
        long chunkSize = 400000;
        long numChunks = TotalBytes / chunkSize;
        long remainder = TotalBytes % chunkSize;

        // get new filename with a bit of entropy
        string justFileName = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);
        string newFileNameWithoutPath = $"{justFileName}-{DateTime.Now.Ticks}{extension}";

        bool firstChunk = true;

        // Open the input and output file streams
        using (var inStream = File.OpenRead(fullPath))
        {
            while (UploadedBytes < TotalBytes)
            {
                var whatsLeft = TotalBytes - UploadedBytes;
                if (whatsLeft < chunkSize)
                    chunkSize = remainder;
                // Read the next chunk
                var bytes = new byte[chunkSize];
                var buffer = new Memory<byte>(bytes);
                var read = await inStream.ReadAsync(buffer);

                // create the FileChunk object
                var chunk = new FileChunk
                {
                    Data = bytes,
                    FileNameNoPath = newFileNameWithoutPath,
                    Offset = UploadedBytes,
                    FirstChunk = firstChunk
                };

                // upload this chunk
                await filesManager.UploadFileChunk(chunk);

                firstChunk = false; // no longer the first chunk

                // Update our progress data and UI
                UploadedBytes += read;
                percent = UploadedBytes * 100 / TotalBytes;
                // Report progress with a string
                UploadMessage.Text = $"Uploading {justFileName}{extension} {percent}%";
                double percentDouble = (double)percent / 100;
                UploadProgressBar.Progress = percentDouble;
            }
        }

        if (!UploadToAzure)
        {
            UploadMessage.Text = "Upload Complete.";
        }
        else
        {
            UploadMessage.Text = "Upload Complete. Sending to Azure...";

            // Copy to Azure
            string url = await filesManager.CopyFileToContainer(ContainerName, newFileNameWithoutPath);
            if (url != "")
            {
                UploadMessage.Text = "Upload Complete. Sending to Azure...Done";
                // delete server file
                await filesManager.DeleteFileOnServer(newFileNameWithoutPath);
            }
            else
            {
                UploadMessage.Text = "Sorry. The file could not be sent to Azure.";
            }
        }

        UploadingFile = false;
        return true;
    }

    private async Task<bool> UploadFile(MultipartFormDataContent form)
    {
        // Check if there is Internet connectivity
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return false;
        }

        try
        {
            // Upload file to the server
            var response = await httpClient.PostAsync("uploadFile", form);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
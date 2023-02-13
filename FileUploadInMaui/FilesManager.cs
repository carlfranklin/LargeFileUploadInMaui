using System.Net.Http.Json;
using Newtonsoft.Json;
using FileUploadShared;

namespace FileUploadInMaui;

public class FilesManager
{
    HttpClient http;

    public FilesManager(HttpClient _http)
    {
        http = _http;
    }

    public async Task<List<string>> GetFileNames()
    {
        try
        {
            var result = await http.GetAsync("files");
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(responseBody);
        }
        catch (Exception ex)
        {
            return new List<string>();
        }
    }

    public async Task<List<string>> GetBlobUrls(string ContainerName)
    {
        try
        {
            var result = await http.GetAsync($"files/{ContainerName}/blobs");
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(responseBody);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> DeleteFileOnServer(string FileNameNoPath)
    {
        try
        {
            var result = await http.GetAsync($"files/{FileNameNoPath}/delete");
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return Convert.ToBoolean(responseBody);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }

    public async Task<string> CopyFileToContainer(string ContainerName, string FileNameNoPath)
    {
        try
        {
            var result = await http.GetAsync($"files/{FileNameNoPath}/{ContainerName}/copy");
            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return "";
        }
    }

    public async Task<bool> UploadFileChunk(FileChunk FileChunk)
    {
        try
        {
            var result = await http.PostAsJsonAsync("files", FileChunk);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return Convert.ToBoolean(responseBody);
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}

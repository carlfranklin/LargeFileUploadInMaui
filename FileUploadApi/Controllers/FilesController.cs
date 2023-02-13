using Microsoft.AspNetCore.Mvc;

namespace FileUploadApi.Controllers;

[ApiController]
[Route("[controller]")]
public class FilesController : ControllerBase
{
    AzureStorageHelper azureStorageHelper;

    public FilesController(AzureStorageHelper _azureStorageHelper)
    {
        azureStorageHelper = _azureStorageHelper;
    }

    [HttpGet("{ContainerName}/blobs")]
    public async Task<List<string>> GetBlobFiles(string ContainerName)
    {
        return await azureStorageHelper.GetFileList(ContainerName);
    }

    [HttpGet("{Filename}/delete")]
    public bool DeleteLocalFile(string FileName)
    {
        // get the local filename
        string filePath = Environment.CurrentDirectory + "\\Files\\";
        string fileName = filePath + FileName;
        if (!System.IO.File.Exists(fileName))
            return true; // not there = deleted
        else
        {
            try
            {
                System.IO.File.Delete(fileName);
                return true;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return false;
            }
        }
    }

    [HttpGet("{Filename}/{ContainerName}/copy")]
    public async Task<string> CopyToContainer(string FileName, string ContainerName)
    {
        // get the local filename
        string filePath = Environment.CurrentDirectory + "\\Files\\";
        string fileName = filePath + FileName;
        if (!System.IO.File.Exists(fileName))
            return "";

        return await azureStorageHelper.UploadFile(ContainerName, fileName, FileName, true);
    }

    [HttpGet]
    public List<string> GetFiles()
    {
        var result = new List<string>();
        var files = Directory.GetFiles(Environment.CurrentDirectory + "\\Files", "*.*");
        foreach (var file in files)
        {
            var justTheFileName = Path.GetFileName(file);
            result.Add($"files/{justTheFileName}");
        }

        return result;
    }

    [HttpPost]
    public bool UploadFileChunk([FromBody] FileChunk FileChunk)
    {
        try
        {
            // get the local filename
            string filePath = Environment.CurrentDirectory + "\\Files\\";
            string fileName = filePath + FileChunk.FileNameNoPath;

            // delete the file if necessary
            if (FileChunk.FirstChunk && System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }

            // open for writing
            using (var stream = System.IO.File.OpenWrite(fileName))
            {
                stream.Seek(FileChunk.Offset, SeekOrigin.Begin);
                stream.Write(FileChunk.Data, 0, FileChunk.Data.Length);
            }

            return true;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }
}

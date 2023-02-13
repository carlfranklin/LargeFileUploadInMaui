namespace FileUploadShared;

public class FileChunk
{
    public string FileNameNoPath { get; set; } = "";
    public long Offset { get; set; }
    public byte[] Data { get; set; }
    public bool FirstChunk = false;
}

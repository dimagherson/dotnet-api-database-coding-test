namespace Storage.Entities
{
    public class Image
    {
        public Guid ImageId { get; set; }
        public string? FileName { get; set; }
        public string? ImageFormat { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[]? Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Hash { get; set; }
    }
}

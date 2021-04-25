using System;

namespace GeneratorApiLibrary.Model
{
    public class Folder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long ExpiresIn { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEncrypted { get; set; }
    }
}

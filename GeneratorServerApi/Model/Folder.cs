using System;

namespace GeneratorApiLibrary.Model
{
    public class Folder
    {
        public string id { get; set; }
        public string name { get; set; }
        public long expiresIn { get; set; }
        public DateTime createdAt { get; set; }
    }
}

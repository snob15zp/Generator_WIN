using System;
using System.Collections.Generic;
using System.Text;

namespace GeneratorApiLibrary.Model
{
    class Folder
    {
        public string id { get; set; }
        public string name { get; set; }
        public long expiresIn { get; set; }
        public DateTime createdAt { get; set; }
    }
}

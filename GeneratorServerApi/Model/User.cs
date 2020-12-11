using System;
using System.Collections.Generic;
using System.Text;

namespace GeneratorApiLibrary.Model
{
    public class User
    {
        public string id { get; set; }
        public string token { get; set; }
        public string[] privileges { get; set; }
        public UserProfile profile { get; set; }
    }
}

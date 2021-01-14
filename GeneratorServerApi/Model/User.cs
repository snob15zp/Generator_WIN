using System.Collections.Generic;

namespace GeneratorApiLibrary.Model
{
    public class User
    {
        public string id { get; set; }
        public string token { get; set; }
        public List<string> privileges { get; set; }
        public UserProfile profile { get; set; }
    }
}

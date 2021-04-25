using System.Collections.Generic;

namespace GeneratorApiLibrary.Model
{
    public class User
    {
        public string Id { get; set; }
        public string Token { get; set; }
        public List<string> Privileges { get; set; }
        public UserProfile Profile { get; set; }
    }
}

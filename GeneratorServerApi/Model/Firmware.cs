using System;
using System.Collections.Generic;

namespace GeneratorServerApi.Model
{
    public class Firmware
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public Boolean Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<FirmwareFile> Files { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace GeneratorServerApi.Model
{
    public class Firmware
    {
        public string id { get; set; }
        public string version { get; set; }
        public Boolean active { get; set; }
        public DateTime createdAt { get; set; }
        public List<FirmwareFile> files { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace GeneratorServerApi
{
    [Serializable]
    public class ApiException : Exception
    {
        public int Status { get; private set; }

        public ApiException(int status, string message) : base(message)
        {
            this.Status = status;
        }
    }
}

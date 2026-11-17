using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SonoffConnect
{
    class Config
    {
        public string Ip { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public int Timeout { get; set; } = 5;
    }
}

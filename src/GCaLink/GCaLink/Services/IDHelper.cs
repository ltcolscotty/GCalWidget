using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace GCaLink.Services
{
    public interface IDHelper
    {
        public readonly record struct EventID(string Value);

        public static EventID GetEventID
            (
                string eId="", 
                string source="",
                string owner=""
            )
        {
            byte[] input = Encoding.UTF8.GetBytes(eId + owner + source);
            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(input);
            return new EventID(Convert.ToHexString(hash).ToLowerInvariant());
        }
    }
}

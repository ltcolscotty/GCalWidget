using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using MessagePack;

namespace GCaLink.Services
{
    public interface IDHelper
    {
        [MessagePackObject]
        public readonly record struct EventID( [property: Key(0)] string Value );

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

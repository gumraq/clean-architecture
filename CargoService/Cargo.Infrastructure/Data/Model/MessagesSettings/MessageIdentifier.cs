using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Cargo.Infrastructure.Data.Model.MessageSettings
{
    public class MessageIdentifier
    {
        public uint id { get; set; }
        public string Identifier { get; set; }
        public byte ch { get; set; }
        public bool cc { get; set; }
        public bool hh { get; set; }

        public ICollection<MessageProperty> MessageProperties { get; set; }
    }
}

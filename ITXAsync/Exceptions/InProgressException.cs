using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITXAsync
{
    public class InProgressException : Exception
    {
        public InProgressException() : base() { }
        public InProgressException(string message) : base(message) { }
        public InProgressException(string message, Exception innerException) : base(message, innerException) { }
    }

}

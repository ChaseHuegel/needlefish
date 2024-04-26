using System;
using System.Collections.Generic;

namespace Needlefish.Schema;

public class NsdException : AggregateException
{
    public NsdException(string message, IEnumerable<Exception> exceptions) : base(message, exceptions) { }

    public NsdException(string message, params Exception[] exceptions) : base(message, exceptions) { }
}
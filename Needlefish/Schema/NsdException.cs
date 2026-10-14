using System;
using System.Collections.Generic;

namespace Needlefish.Schema;

public class NsdException : AggregateException
{
    /// <summary>
    /// 0-based character offset of the problem in the source, or -1 when unknown.
    /// </summary>
    public int Position { get; }

    public int Length { get; }

    public NsdException(string message, int position = -1, int length = 0) : base(message)
    {
        Position = position;
        Length = length;
    }

    public NsdException(string message, IEnumerable<Exception> exceptions) : base(message, exceptions) { }

    public NsdException(string message, params Exception[] exceptions) : base(message, exceptions) { }
}

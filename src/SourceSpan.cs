using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

class SourceSpan
{
    public string FileName { get; private set; }

    public int FromLineNumber { get; set; }
    public int FromColumnNumber { get; set; }

    public int ToLineNumber { get; set; }
    public int ToColumnNumber { get; set; }

    public SourceSpan(string fileName, int lineNumber, int columnNumber)
    {
        this.FileName = fileName;

        this.FromLineNumber = lineNumber;
        this.FromColumnNumber = columnNumber;

        this.ToLineNumber = lineNumber;
        this.ToColumnNumber = columnNumber;
    }

    public SourceSpan(string fileName, int fromLineNumber, int fromLineColumn, int toLineNumber, int toColumnNumber)
    {
        this.FileName = fileName;

        this.FromLineNumber = fromLineNumber;
        this.FromColumnNumber = fromLineColumn;

        this.ToLineNumber = toLineNumber;
        this.ToColumnNumber = toColumnNumber;
    }

    public SourceSpan Clone()
    {
        return new SourceSpan(this.FileName, this.FromLineNumber, this.FromColumnNumber, this.ToLineNumber, this.ToColumnNumber);
    }

    public override string ToString()
    {
        return string.Format("({0}:{1}, {2}:{3})", this.FromLineNumber, this.FromColumnNumber, this.ToLineNumber, this.ToColumnNumber);
    }

    public static SourceSpan FromTo(SourceSpan from, SourceSpan to)
    {
        Debug.Assert(from.FileName == to.FileName);

        return new SourceSpan(from.FileName, from.FromLineNumber, from.FromColumnNumber, to.ToLineNumber, to.ToColumnNumber);
    }
}

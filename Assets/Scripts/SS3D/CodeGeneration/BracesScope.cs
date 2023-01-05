using System;

namespace SS3D.CodeGeneration
{
    public class BracesScope : IDisposable
    {
        private readonly SourceFile _source;

        public BracesScope(SourceFile source)
        {
            _source = source;
            _source.AppendLine("{");
            _source.Indentator.Add();
        }

        public void Dispose()
        {
            _source.Indentator.Sub();
            _source.AppendLine("}");
        }
    }
}
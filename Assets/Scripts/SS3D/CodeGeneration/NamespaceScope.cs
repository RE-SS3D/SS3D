using System;

namespace SS3D.CodeGeneration
{
    public class NamespaceScope : IDisposable
    {
        private readonly SourceFile _source;

        public NamespaceScope(SourceFile source, string @namespace)
        {
            _source = source;
            _source.AppendLine($"namespace {@namespace}");
            _source.AppendLine("{");
            _source.Indentator.Add();
        }

        public void Dispose()
        {
            _source.Indentator.Sub();
            _source.AppendLine("}");
            _source.AppendLine();
        }
    }
}
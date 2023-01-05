using System.Text;

namespace SS3D.CodeGeneration
{
    public class SourceFile
    {
        public readonly Indentator Indentator = new();
        private readonly StringBuilder _builder = new();

        public void AppendLine(string content = "")
        {
            _builder.AppendLine($"{Indentator}{content}");
        }

        public void Append(string content)
        {
            _builder.Append($"{Indentator}{content}");
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
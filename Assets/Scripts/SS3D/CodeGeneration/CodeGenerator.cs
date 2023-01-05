using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SS3D.Data
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

    public static class CodeWriter
    {
        public static void WriteEnum(string filePath, string enumName, IEnumerable<string> items)
        {
            IEnumerable<string> filtered = items.GroupBy(name => name).Select(nameGroup => nameGroup.Key);

            SourceFile sourceFile = new();
            using (new NamespaceScope(sourceFile, "SS3D.Data"))
            {
                sourceFile.AppendLine($"public enum {enumName}");

                using (new BracesScope(sourceFile))
                {
                    for (int index = 0; index < filtered.ToList().Count; index++)
                    {
                        string member = filtered.ToList()[index];
                        if (string.IsNullOrWhiteSpace(member))
                        {
                            continue;
                        }

                        char[] corrected = member.ToCharArray();
                        corrected[0] = corrected[0].ToString().ToUpper()[0];

                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.Append(corrected);

                        sourceFile.AppendLine($"{stringBuilder.ToString()} = {index}, ");
                    }
                }
            }

            File.WriteAllText(filePath + "/" + enumName + ".cs", sourceFile.ToString());
        }
    }

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

    public class Indentator
    {
        private StringBuilder _builder;

        private int _lastAmount = 0;
        private int _amount = 0;

        public Indentator()
        {
            _builder = new StringBuilder();
        }

        public void Add()
        {
            _amount++;
        }

        public void Sub()
        {
            _amount--;
        }

        public override string ToString()
        {
            if (_lastAmount == _amount)
            {
                return _builder.ToString();
            }

            _builder.Clear();
            for (int i = 0; i < _amount; i++)
            {
                _builder.Append("    ");
            }

            _lastAmount = _amount;
            return _builder.ToString();
        }
    }

    public class SourceFile
    {
        public readonly Indentator Indentator = new Indentator();
        private readonly StringBuilder _builder = new StringBuilder();

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
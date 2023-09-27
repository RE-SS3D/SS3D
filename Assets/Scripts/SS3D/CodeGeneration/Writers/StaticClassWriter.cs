using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SS3D.CodeGeneration
{
    public static class StaticClassWriter
    {
        public static void WriteStaticClass(string filePath, string className, IEnumerable<string> items, string namespaceName = "SS3D.Data.Enums")
        {
            IEnumerable<string> filtered = items.GroupBy(name => name).Select(nameGroup => nameGroup.Key);

            SourceFile sourceFile = new();
            using (new NamespaceScope(sourceFile, namespaceName))
            {
                sourceFile.AppendLine($"public class {className}");

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

                        StringBuilder stringBuilder = new();
                        stringBuilder.Append(corrected);

                        sourceFile.AppendLine($"public static string {stringBuilder} = \"{stringBuilder}\";\n");
                    }
                }
            }

            File.WriteAllText(filePath + "/" + className + ".cs", sourceFile.ToString());
        }
    }
}
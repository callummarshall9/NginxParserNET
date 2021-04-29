using System.Text;

namespace NginxParser.Entities
{
    public class PropertyEntry: IBuildable
    {
        public string Key;
        public string Value;
        public void Build(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(Key + " " + Value + ";");
        }
    }
}
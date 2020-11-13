using System.Text;

namespace NginxParser
{
    public interface IBuildable
    {
        public void Build(StringBuilder stringBuilder);
    }
}
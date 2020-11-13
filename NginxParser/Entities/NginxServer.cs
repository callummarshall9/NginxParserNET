using System.Collections.Generic;
using System.Text;

namespace NginxParser.Entities
{
    public class NginxServer: NginxPropertyable
    {
        public List<NginxLocation> Locations;
        public NginxLimitExcept LimitExcept;
        public List<TokenEntry> UnsupportedTokens;

        public NginxServer() : base()
        {
            Locations = new List<NginxLocation>();
            UnsupportedTokens = new List<TokenEntry>();
        }
        public override void Build(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("server {");
            Properties.ForEach(p => p.Build(stringBuilder));
            Locations.ForEach(l => l.Build(stringBuilder));
            UnsupportedTokens.ForEach(l => l.Build(stringBuilder));
            LimitExcept.Build(stringBuilder);
            stringBuilder.AppendLine("}");
        }
    }
}
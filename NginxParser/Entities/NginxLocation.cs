using System.Collections.Generic;
using System.Text;

namespace NginxParser.Entities
{
    public class NginxLocation : NginxPropertyable
    {
        public List<NginxLocation> Locations;
        public NginxLimitExcept LimitExcept;
        public NginxLocation()
        {
            Locations = new List<NginxLocation>();
        }
        public override void Build(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"location {Name} {{");
            Properties.ForEach(p => p.Build(stringBuilder));
            Locations.ForEach(p => p.Build(stringBuilder));
            stringBuilder.AppendLine("}");
        }
    }

}
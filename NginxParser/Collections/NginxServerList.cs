using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using NginxParser.Entities;

namespace NginxParser.Collections
{
    public class NginxServerList: List<NginxServer>, IBuildable
    {
        public void Build(StringBuilder stringBuilder)
        {
            ForEach(k => k.Build(stringBuilder));
            stringBuilder.AppendLine("");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NginxParser.Entities
{
    public abstract class NginxPropertyable : IBuildable
    {
        public List<PropertyEntry> Properties;
        public string Name;

        public NginxPropertyable()
        {
            Properties = new List<PropertyEntry>();
        }
        public void ParseProperties(string configBlock)
        {
            var configBlockChild = configBlock.Replace("[token]", "").Split("{")[1].Replace("}", "").Split(";");
            var childProperties = new List<PropertyEntry>();
            foreach (var childProperty in configBlockChild)
            {
                var values = childProperty.Trim().Split(" ");
                var childKey = values[0];
                var childValue = String.Join(' ', values.ToList().Where(k => k != values[0]).ToArray());
                if (childKey != "")
                {
                    childProperties.Add(new PropertyEntry
                    {
                        Key = childKey,
                        Value = childValue
                    });   
                }
            }
            Properties = childProperties;
        }

        public virtual void Build(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"{Name} {{");
            Properties.ForEach(p => p.Build(stringBuilder));
            stringBuilder.AppendLine("}");
        }
    }
}
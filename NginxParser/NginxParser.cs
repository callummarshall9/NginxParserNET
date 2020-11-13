using System;
using System.Collections.Generic;
using System.Linq;
using NginxParser.Collections;
using NginxParser.Entities;

namespace NginxParser
{
    public static class NginxParser
    {
        static NginxLocation ParseLocationToken(TokenEntry locationToken)
        {
            var location = new NginxLocation {Name = locationToken.Name};
            location.ParseProperties(locationToken.Content);
            if (locationToken.Tokens.Any())
            {
                location.Locations = locationToken.Tokens.Where(k => k.Name.Contains("location")).Select(ParseLocationToken).ToList();
                location.LimitExcept = locationToken.Tokens.Where(k => k.Name.Contains("limit_except")).Select(ParseLimitExceptToken).FirstOrDefault();
            }
            else
            {
                location.Locations = new List<NginxLocation>();
            }
            return location;
        }
        static NginxLimitExcept ParseLimitExceptToken(TokenEntry limitToken)
        {
            var limitExcept = new NginxLimitExcept {Name = limitToken.Name};
            limitExcept.ParseProperties(limitToken.Content);
            return limitExcept;
        }
        public static NginxServerList Parse(string config)
        {
            var serverEntries = new NginxServerList();
            var tokens = NginxTokenizer.Tokenize(config);
            foreach (var entry in tokens)
            {
                if (!entry.Name.Contains("server")) continue;
                var newServer = new NginxServer();
                newServer.ParseProperties(entry.Content);
                newServer.Locations = new List<NginxLocation>();
                foreach (var childToken in entry.Tokens)
                {
                    if (childToken.Name.Contains("location"))
                    {
                        newServer.Locations.Add(ParseLocationToken(childToken));
                    } else if (childToken.Name.Contains("limit_except"))
                    {
                        newServer.LimitExcept = ParseLimitExceptToken(childToken);
                    }
                    else
                    {
                        newServer.UnsupportedTokens.Add(childToken);
                    }
                }
                serverEntries.Add(newServer);
            }

            return serverEntries;

        }
    }
}
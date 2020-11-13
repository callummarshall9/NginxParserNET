using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NginxParser
{
    
    public class TokenEntry: IBuildable
    {
        public string Name;
        public string Content;
        public int ControlFlowLevel;
        public List<TokenEntry> Tokens;
        public string FlattenContent()
        {
	        string newContent = Content;
	        var regex = new Regex(Regex.Escape("[token]"));
	        foreach (var entry in Tokens)
	        {
		        newContent = regex.Replace(newContent, entry.FlattenContent(), 1);
	        }
	        return newContent;
        }

        public void Build(StringBuilder stringBuilder)
        {
	        stringBuilder.AppendLine(Content + Environment.NewLine);
        }
    }

    public static class NginxTokenizer
    {
	    public static List<TokenEntry> ParseTokenFlows(Stack<TokenEntry> tokenFlows)
	    {
		    List<TokenEntry> tokens = new List<TokenEntry>();
		    Queue<TokenEntry> sameControlLevels = new Queue<TokenEntry>();
		    while (tokenFlows.Count > 0)
		    {
			    TokenEntry popped = tokenFlows.Pop();
			    if (popped.ControlFlowLevel == 1)
			    {
				    while (sameControlLevels.Count > 0)
				    {
					    var sameControlLevelPDequeue = sameControlLevels.Dequeue();
					    if (sameControlLevelPDequeue.ControlFlowLevel > 1)
					    {
						    popped.Content = popped.Content.Replace(sameControlLevelPDequeue.Content, "[token]");
					    }

					    popped.Tokens.Add(sameControlLevelPDequeue);
				    }

				    popped.Tokens.Reverse();
				    tokens.Add(popped);
			    }
			    else if (tokenFlows.Count > 0)
			    {
				    var previous = tokenFlows?.Peek();
				    if (previous.ControlFlowLevel == popped.ControlFlowLevel - 1)
				    {
					    if (sameControlLevels.Count > 0 &&
					        sameControlLevels.Peek().ControlFlowLevel == popped.ControlFlowLevel)
					    {
						    sameControlLevels.Enqueue(popped);
					    }
					    else
					    {
						    if (previous.ControlFlowLevel == 1)
						    {
							    previous.Content = previous.Content.Replace(popped.Content, "[token]");
						    }
						    else
						    {
							    int currentControlLevel = popped.ControlFlowLevel - 1;
							    while (currentControlLevel > 0)
							    {
								    var lowerControlLevel =
									    tokenFlows.First(k => k.ControlFlowLevel == currentControlLevel);
								    lowerControlLevel.Content =
									    lowerControlLevel.Content.Replace(popped.Content, "[token]");
								    currentControlLevel--;
							    }
						    }

						    previous.Tokens.Add(popped);
					    }
				    }
				    else
				    {
					    sameControlLevels.Enqueue(popped);
				    }
			    }
		    }

		    return tokens;
	    }

	    public static List<TokenEntry> Tokenize(string thingToParse)
	    {
		    Stack<TokenEntry> tokenFlows = new Stack<TokenEntry>();
		    int currentFlowLevel = 0;
		    string buffer = "";
		    var lines = thingToParse.Split("\n");
		    List<TokenEntry> tokens = new List<TokenEntry>();

		    foreach (var line in lines)
		    {
			    if (line.Contains("{"))
			    {
				    if (currentFlowLevel == 0 && tokenFlows.Count > 0)
				    {
					    tokens.AddRange(ParseTokenFlows(tokenFlows));
					    tokenFlows.Clear();
				    }
				    else if (tokenFlows.Count > 0)
				    {
					    tokenFlows.Peek().Content += buffer;
					    foreach (var flow in tokenFlows.Where(k => k.ControlFlowLevel < currentFlowLevel))
					    {
						    flow.Content += buffer;
					    }
				    }
				    buffer = line.Trim().Split("{")[0] + "{ " + Environment.NewLine;
				    currentFlowLevel++;
				    tokenFlows.Push(new TokenEntry
				    {
					    Name = line.Trim().Split("{")[0],
					    ControlFlowLevel = currentFlowLevel,
					    Tokens = new List<TokenEntry>()
				    });
			    }
			    else if (line.Contains("}"))
			    {
				    buffer += line.Trim() + Environment.NewLine;
				    int currentControlLevel = tokenFlows.Peek().ControlFlowLevel;
				    while (currentControlLevel > 0)
				    {
					    var lowerControlLevel = tokenFlows.First(k => k.ControlFlowLevel == currentControlLevel);
					    lowerControlLevel.Content += buffer;
					    currentControlLevel--;
				    }
				    buffer = "";
				    currentFlowLevel--;
			    }
			    else
			    {
				    buffer += line.Trim() + Environment.NewLine;
			    }
		    }
		    if (currentFlowLevel == 0 && tokenFlows.Count > 0)
		    {
			    tokens.AddRange(ParseTokenFlows(tokenFlows));
			    tokenFlows.Clear();
		    }
		    return tokens.Where(k => k.ControlFlowLevel == 1).ToList();
	    }
    }
}
    


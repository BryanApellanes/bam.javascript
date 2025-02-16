/*
	Copyright © Bryan Apellanes 2015  
*/

using Bam.CommandLine;

namespace Bam.Javascript.NodeJs
{
	public class NodeScriptRunner
	{
        public NodeScriptRunner()
        {
            this.NodePath = OSInfo.Current == OSNames.Windows ? @"C:\Program Files\nodejs\node.exe": "/usr/local/bin/node";
        }

        public string NodePath { get; set; }

        public ProcessOutput Run(string nodeScriptPath)
        {
            return "{0} {1}".Format(NodePath, nodeScriptPath).Run();
        }
	}
}

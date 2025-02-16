/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Javascript
{
    public class CliProvider
    {
        public CliProvider(string varName, object provider)
        {
            this.VarName = varName;
            this.Provider = provider;
        }

        public string VarName { get; set; }
        public object Provider { get; set; }
    }
}

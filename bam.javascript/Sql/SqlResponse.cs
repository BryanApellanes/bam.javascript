/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Javascript.Sql
{
	public class SqlResponse
	{
		public SqlResponse() { }
		public int Count { get; set; }
		public bool Success { get; set; }
		public string Message { get; set; }

		public object[] Results { get; set; }
	}
}

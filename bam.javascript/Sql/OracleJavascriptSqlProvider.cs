/*
	Copyright © Bryan Apellanes 2015  
*/

using Bam.Data;
using Bam.Data.Oracle;

namespace Bam.Javascript.Sql
{
	public class OracleJavaScriptSqlProvider: JavaScriptSqlProvider
	{
		public OracleJavaScriptSqlProvider()
		{
		}

		public string OracleUserId { get; set; } = null!;
		public string OraclePassword { get; set; } = null!;
		public string OracleServerName { get; set; } = null!;
		public string OraclePort { get; set; } = null!;
		public string OracleInstanceName { get; set; } = null!;

		protected override void Initialize()
		{
			OracleDatabase database = new OracleDatabase();
			OracleCredentials creds = new OracleCredentials { UserId = OracleUserId, Password = OraclePassword };
			OracleConnectionStringResolver conn = new OracleConnectionStringResolver { ServerName = OracleServerName, InstanceName = OracleInstanceName, Port = OraclePort, Credentials = creds };
			database.ConnectionStringResolver = conn;
			Database = database;
		}

		#region IHasRequiredProperties Members

		public override string[] RequiredProperties
		{
			get
			{
				return new string[] { "OracleUserId", "OraclePassword", "OracleServerName", "OraclePort", "OracleInstanceName" };
			}
		}

		#endregion
	}
}

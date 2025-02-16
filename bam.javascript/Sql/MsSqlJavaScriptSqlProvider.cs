/*
	Copyright © Bryan Apellanes 2015  
*/

using Bam.Data.MsSql;

namespace Bam.Javascript.Sql
{
	public class MsSqlJavaScriptSqlProvider: JavaScriptSqlProvider
	{
		public MsSqlJavaScriptSqlProvider()
		{
		}

		public string MsSqlUserId { get; set; }
		public string MsSqlPassword { get; set; }
		public string MsSqlServerName { get; set; }
		public string MsSqlDatabaseName { get; set; }

		protected override void Initialize()
		{
			MsSqlDatabase database = new MsSqlDatabase();
			MsSqlCredentials creds = new MsSqlCredentials { UserId = MsSqlUserId, Password = MsSqlPassword };
            MsSqlConnectionStringResolver conn = new MsSqlConnectionStringResolver(MsSqlServerName, MsSqlDatabaseName, creds);
			database.ConnectionStringResolver = conn;
			Database = database;
		}

		#region IHasRequiredProperties Members

		public override string[] RequiredProperties
		{
			get
			{
                return new string[] { "MsSqlServerName", "MsSqlDatabaseName" };
			}
		}

		#endregion
	}
}

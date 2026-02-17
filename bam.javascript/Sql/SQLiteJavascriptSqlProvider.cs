/*
	Copyright © Bryan Apellanes 2015  
*/

using Bam.Data.SQLite;

namespace Bam.Javascript.Sql
{
	public class SQLiteJavaScriptSqlProvider: JavaScriptSqlProvider
	{
		public SQLiteJavaScriptSqlProvider() { }

		public string SQLiteDirectoryPath { get; set; } = null!;
		public string SQLiteFileName { get; set; } = null!;

		protected override void Initialize()
		{
			this.Database = new SQLiteDatabase(SQLiteDirectoryPath, SQLiteFileName);
		}

		public override string[] RequiredProperties
		{
			get { return new string[] { "SQLiteDirectoryPath", "SQLiteFileName" }; }
		}
	}
}

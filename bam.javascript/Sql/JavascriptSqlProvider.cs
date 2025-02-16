/*
	Copyright © Bryan Apellanes 2015  
*/

using Bam.Data;
using System.Data;
using Bam.Configuration;
using Bam.Javascript.Sql;

namespace Bam.Javascript
{
    [Proxy("sql")]
	public abstract partial class JavaScriptSqlProvider : IConfigurable
	{
		public JavaScriptSqlProvider() { }

		public Database Database
		{
			get;
			set;
		}

		bool _initialized;
		public void EnsureInitialized()
		{
			if(!_initialized)
			{
				_initialized = true;
				Initialize();				
			}
		}
		
		/// <summary>
		/// Initialize the database by instantiating it and setting the connection string.
		/// </summary>
		protected abstract void Initialize();
        public SqlResponse Execute(string sql)
        {
            EnsureInitialized();
            SqlResponse result = new SqlResponse();
            try
            {
                DataTable results = Database.GetDataTable(sql, CommandType.Text);
                List<object> rows = new List<object>();
                foreach (DataRow row in results.Rows)
                {
                    rows.Add(row);
                }

                result.Results = rows.ToArray();
                result.Count = results.Rows.Count;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                result.Results = new object[] { };
            }

            return result;
        }

        #region IConfigurable Members

        [Exclude]
        public virtual void Configure(IConfigurer configurer)
        {
            configurer.Configure(this);
            this.CheckRequiredProperties();
        }

        [Exclude]
        public virtual void Configure(object configuration)
        {
            this.CopyProperties(configuration);
            this.CheckRequiredProperties();
        }
		#endregion

		#region IHasRequiredProperties Members

		public abstract string[] RequiredProperties { get; }
		#endregion
	}
}

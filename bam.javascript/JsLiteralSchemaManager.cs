using Bam.Net;
using Bam.Net.Data.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Javascript
{
    public class JsLiteralSchemaManager : DaoSchemaManager
    {
        public void ProcessTables(dynamic rehydrated, List<dynamic> foreignKeys)
        {
            foreach (dynamic table in rehydrated["tables"])
            {
                string tableName = (string)table["name"];
                Args.ThrowIfNullOrEmpty(tableName, "Table.name");
                this.AddTable(tableName);

                ExecutePreColumnAugmentations(tableName, this);

                AddColumns(table, tableName);

                AddForeignKeys(foreignKeys, table, tableName);

                ExecutePostColumnAugmentations(tableName, this);
            }
        }

        public void ProcessXrefs(dynamic rehydrated, List<dynamic> foreignKeys)
        {
            ProcessXrefs(this, rehydrated, foreignKeys);
        }

        protected static void ProcessXrefs(DaoSchemaManager manager, dynamic rehydrated, List<dynamic> foreignKeys)
        {
            if (rehydrated["xrefs"] != null)
            {
                foreach (dynamic xref in rehydrated["xrefs"])
                {
                    string leftTableName = (string)xref[0];
                    string rightTableName = (string)xref[1];

                    Args.ThrowIfNullOrEmpty(leftTableName, "xref[0]");
                    Args.ThrowIfNullOrEmpty(rightTableName, "xref[1]");

                    SetXref(manager, foreignKeys, leftTableName, rightTableName);
                }

            }
        }
    }
}

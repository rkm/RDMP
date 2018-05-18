﻿using System.Collections.Generic;
using System.Data.Common;

namespace ReusableLibraryCode.DatabaseHelpers.Discovery
{
    public abstract class DiscoveredTableHelper :IDiscoveredTableHelper
    {
        

        public abstract string GetTopXSqlForTable(IHasFullyQualifiedNameToo table, int topX);

        public abstract DiscoveredColumn[] DiscoverColumns(DiscoveredTable discoveredTable, IManagedConnection connection, string database,
            string tableName);

        public abstract DiscoveredColumn[] DiscoverColumns(DiscoveredTableValuedFunction discoveredTableValuedFunction,
            IManagedConnection connection, string database, string tableName);

        public abstract IDiscoveredColumnHelper GetColumnHelper();
        public abstract void DropTable(DbConnection connection, DiscoveredTable tableToDrop);
        public abstract void DropFunction(DbConnection connection, DiscoveredTableValuedFunction functionToDrop);
        public abstract void DropColumn(DbConnection connection, DiscoveredColumn columnToDrop);

        public virtual void AddColumn(DiscoveredTable table, DbConnection connection, string name, string dataType, bool allowNulls)
        {
            table.Database.Server.GetCommand("ALTER TABLE " + table + " ADD " + name + " " + dataType + " " + (allowNulls ? "NULL" : "NOT NULL"), connection).ExecuteNonQuery();
        }

        public abstract int GetRowCount(DbConnection connection, IHasFullyQualifiedNameToo table, DbTransaction dbTransaction = null);

        public abstract DiscoveredParameter[] DiscoverTableValuedFunctionParameters(DbConnection connection, DiscoveredTableValuedFunction discoveredTableValuedFunction, DbTransaction transaction);

        public abstract IBulkCopy BeginBulkInsert(DiscoveredTable discoveredTable, IManagedConnection connection);

        public virtual void TruncateTable(DiscoveredTable discoveredTable)
        {
            var server = discoveredTable.Database.Server;
            using (var con = server.GetConnection())
            {
                con.Open();
                server.GetCommand("TRUNCATE TABLE " + discoveredTable.GetFullyQualifiedName(), con).ExecuteNonQuery();
            }
        }

        public string ScriptTableCreation(DiscoveredTable table, bool withPrimaryKeys, bool withConstraints)
        {
            List<DatabaseColumnRequest> columns = new List<DatabaseColumnRequest>();

            foreach (DiscoveredColumn c in table.DiscoverColumns())
                columns.Add(
                    new DatabaseColumnRequest(
                        c.GetRuntimeName(),
                        c.DataType.SQLType,
                        c.AllowNulls || !withConstraints) { IsPrimaryKey = c.IsPrimaryKey && withPrimaryKeys });

            return table.Database.Helper.GetCreateTableSql(table.Database, table.GetRuntimeName(), columns.ToArray());
        }

        public virtual bool IsEmpty(DbConnection connection, DiscoveredTable discoveredTable, DbTransaction transaction)
        {
            return GetRowCount(connection, discoveredTable, transaction) == 0;
        }

        public virtual void RenameTable(DiscoveredTable discoveredTable, string newName, IManagedConnection connection)
        {
            DbCommand cmd = DatabaseCommandHelper.GetCommand(GetRenameTableSql(discoveredTable, newName), connection.Connection, connection.Transaction);
            cmd.ExecuteNonQuery();
        }

        protected abstract string GetRenameTableSql(DiscoveredTable discoveredTable, string newName);

        public abstract void MakeDistinct(DiscoveredTable discoveredTable);
    }
}
﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using ReusableLibraryCode.Checks;

namespace ReusableLibraryCode.DatabaseHelpers.Discovery.Microsoft
{
    public class MicrosoftSQLTableHelper : DiscoveredTableHelper
    {
        public override DiscoveredColumn[] DiscoverColumns(DiscoveredTable discoveredTable, IManagedConnection connection, string database, string tableName)
        {
            tableName = SqlSyntaxHelper.GetRuntimeName(tableName);

            List<DiscoveredColumn> columns = new List<DiscoveredColumn>();

      
            DbCommand cmd = DatabaseCommandHelper.GetCommand("use [" + database + @"];  exec sp_columns @table_name", connection.Connection);
            cmd.Transaction = connection.Transaction;

            DbParameter p = new SqlParameter("@table_name",SqlDbType.VarChar);
            p.Value = tableName;
            cmd.Parameters.Add(p);

            using(var r = cmd.ExecuteReader())
            {

                if (!r.HasRows)
                    throw new Exception("Could not find any columns using sp_columns for table " + tableName +
                                        " in database " + database);

                while (r.Read())
                {
                    DiscoveredColumn toAdd = new DiscoveredColumn(discoveredTable, (string)r["COLUMN_NAME"], Convert.ToBoolean(r["NULLABLE"]));
                    toAdd.DataType = new DiscoveredDataType(r, GetSQLType_FromSpColumnsResult(r),toAdd);

                    columns.Add(toAdd);
                }
                r.Close();
            }
            var pks = ListPrimaryKeys(connection, tableName);

            foreach (DiscoveredColumn c in columns)
                if (pks.Any(pk=>pk.Equals(c.GetRuntimeName())))
                    c.IsPrimaryKey = true;


            return columns.ToArray();
            
        }

        public override DiscoveredColumn[] DiscoverColumns(DiscoveredTableValuedFunction discoveredTableValuedFunction, IManagedConnection connection, string database, string tableName)
        {
            string tableValuedFunctionName = discoveredTableValuedFunction.GetRuntimeName();

            DbCommand cmd = DatabaseCommandHelper.GetCommand("use [" + database + @"];
SELECT  
sys.columns.name AS COLUMN_NAME,
 sys.types.name AS TYPE_NAME,
  sys.columns.collation_name AS COLLATION_NAME,
   sys.columns.max_length as LENGTH,
   sys.columns.scale as SCALE,
   sys.columns.precision as PRECISION
from sys.columns 
join 
sys.types on sys.columns.user_type_id = sys.types.user_type_id
where object_id =OBJECT_ID('" + tableValuedFunctionName+"')", connection.Connection,connection.Transaction);

            List<DiscoveredColumn> toReturn = new List<DiscoveredColumn>();


            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    var toAdd = new DiscoveredColumn(discoveredTableValuedFunction,
                        tableValuedFunctionName + "." + r["COLUMN_NAME"], false);
                    toAdd.DataType = new DiscoveredDataType(r, GetSQLType_FromSpColumnsResult(r), toAdd);
                    toReturn.Add(toAdd);
                }
            return toReturn.ToArray();
        }

        public override IDiscoveredColumnHelper GetColumnHelper()
        {
            return new MicrosoftSQLColumnHelper();
        }

        public override void DropTable(DbConnection connection, DiscoveredTable tableToDrop)
        {
            
            SqlCommand cmd;

            switch (tableToDrop.TableType)
            {
                case TableType.View:
                    if (connection.Database != tableToDrop.Database.GetRuntimeName())
                        connection.ChangeDatabase(tableToDrop.GetRuntimeName());

                    if(!connection.Database.ToLower().Equals(tableToDrop.Database.GetRuntimeName().ToLower()))
                        throw new NotSupportedException("Cannot drop view "+tableToDrop +" because it exists in database "+ tableToDrop.Database.GetRuntimeName() +" while the current current database connection is pointed at database:" + connection.Database + " (use .ChangeDatabase on the connection first) - SQL Server does not support cross database view dropping");

                    cmd = new SqlCommand("DROP VIEW " + tableToDrop.GetRuntimeName(), (SqlConnection)connection);
                    break;
                case TableType.Table:
                    cmd = new SqlCommand("DROP TABLE " + tableToDrop.GetFullyQualifiedName(), (SqlConnection)connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            cmd.ExecuteNonQuery();
        }

        public override void DropFunction(DbConnection connection, DiscoveredTableValuedFunction functionToDrop)
        {
            SqlCommand cmd = new SqlCommand("DROP FUNCTION " + functionToDrop.GetRuntimeName(), (SqlConnection)connection);
            cmd.ExecuteNonQuery();
        }

        public override void DropColumn(DbConnection connection, DiscoveredColumn columnToDrop)
        {
            SqlCommand cmd = new SqlCommand("ALTER TABLE " + columnToDrop.Table.GetFullyQualifiedName() + " DROP column " + columnToDrop.GetRuntimeName(), (SqlConnection)connection);
            cmd.ExecuteNonQuery();
        }

        public override int GetRowCount(DbConnection connection, IHasFullyQualifiedNameToo table, DbTransaction dbTransaction = null)
        {
                    SqlCommand cmdCount = new SqlCommand(@"/*Do not lock anything, and do not get held up by any locks.*/
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
 
-- Quickly get row counts.
declare @rowcount int = (SELECT distinct max(p.rows) AS [Row Count]
FROM sys.partitions p
INNER JOIN sys.indexes i ON p.object_id = i.object_id
                         AND p.index_id = i.index_id
WHERE OBJECT_NAME(p.object_id) = @tableName)

-- if we could not get it quickly then it is probably a view or something so have to return the slow count
if @rowcount is null 
	set @rowcount = (select count(*) from "+table.GetFullyQualifiedName()+@")

select @rowcount", (SqlConnection) connection);

                    cmdCount.Transaction = dbTransaction as SqlTransaction;
                    cmdCount.Parameters.Add(new SqlParameter("@tableName",SqlDbType.VarChar));
                    cmdCount.Parameters["@tableName"].Value = table.GetRuntimeName();

                    return Convert.ToInt32(cmdCount.ExecuteScalar());
        }

        public override string WrapStatementWithIfTableExistanceMatches(bool existanceDesiredForExecution, StringLiteralSqlInContext bodySql, string tableName)
        {
            if(bodySql.IsDynamic)
                throw new NotSupportedException("Cannot wrap dynamic SQL im afraid");

            //get the runtime name of it
            tableName = new MicrosoftQuerySyntaxHelper().GetRuntimeName(tableName);

            return string.Format(@"
--If the table does not yet exist then create it
IF {0} (EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE 
                 TABLE_NAME = '{1}'))
BEGIN

{2}

END
", 
 
 existanceDesiredForExecution?"":"NOT",tableName,bodySql.Sql);


        }
        
        public override DiscoveredParameter[] DiscoverTableValuedFunctionParameters(DbConnection connection,DiscoveredTableValuedFunction discoveredTableValuedFunction, DbTransaction transaction)
        {
            string query =
                @"select 
sys.parameters.name AS name,
sys.types.name AS TYPE_NAME,
sys.parameters.max_length AS LENGTH,
sys.types.collation_name AS COLLATION_NAME,
sys.parameters.scale AS SCALE,
sys.parameters.precision AS PRECISION
 from 
sys.parameters 
join
sys.types on sys.parameters.user_type_id = sys.types.user_type_id
where object_id = OBJECT_ID('"+discoveredTableValuedFunction.GetRuntimeName()+"')";

            DbCommand cmd = DatabaseCommandHelper.GetCommand(query, connection);
            cmd.Transaction = transaction;

            List<DiscoveredParameter> toReturn = new List<DiscoveredParameter>();

            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    DiscoveredParameter toAdd = new DiscoveredParameter(r["name"].ToString());
                    toAdd.DataType = new DiscoveredDataType(r, GetSQLType_FromSpColumnsResult(r),null);
                    toReturn.Add(toAdd);
                }
            }
            
            return toReturn.ToArray();
        }

        public override IBulkCopy BeginBulkInsert(DiscoveredTable discoveredTable,IManagedConnection connection)
        {
            return new MicrosoftSQLBulkCopy(discoveredTable,connection);
        }

        public override string GetTopXSqlForTable(IHasFullyQualifiedNameToo table, int topX)
        {
            return "SELECT TOP " + topX + " * FROM " + table.GetFullyQualifiedName();
        }
        
        private string GetSQLType_FromSpColumnsResult(DbDataReader r)
        {
            string columnType = r["TYPE_NAME"] as string;
            string lengthQualifier = "";

            if (UsefulStuff.HasPrecisionAndScale(columnType))
                lengthQualifier = "(" + r["PRECISION"] + "," + r["SCALE"] + ")";
            else
                if (UsefulStuff.RequiresLength(columnType))
                    lengthQualifier = "(" + r["LENGTH"] + ")";

            return columnType + lengthQualifier;
        }


        private string[] ListPrimaryKeys(IManagedConnection con, string tableName)
        {
            List<string> toReturn = new List<string>();

            string query = String.Format(@"SELECT i.name AS IndexName, 
OBJECT_NAME(ic.OBJECT_ID) AS TableName, 
COL_NAME(ic.OBJECT_ID,ic.column_id) AS ColumnName, 
c.is_identity
FROM sys.indexes AS i 
INNER JOIN sys.index_columns AS ic 
INNER JOIN sys.columns AS c ON ic.object_id = c.object_id AND ic.column_id = c.column_id 
ON i.OBJECT_ID = ic.OBJECT_ID 
AND i.index_id = ic.index_id 
WHERE (i.is_primary_key = 1) AND ic.OBJECT_ID = OBJECT_ID('dbo.{0}')
ORDER BY OBJECT_NAME(ic.OBJECT_ID), ic.key_ordinal", tableName);

            DbCommand cmd = DatabaseCommandHelper.GetCommand(query, con.Connection);
            cmd.Transaction = con.Transaction;

            using(DbDataReader r = cmd.ExecuteReader())
            {

                while (r.Read())
                    toReturn.Add((string) r["ColumnName"]);

                r.Close();
            }
            return toReturn.ToArray();
        }

    }
}
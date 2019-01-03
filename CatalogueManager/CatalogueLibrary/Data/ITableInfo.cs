using System;
using System.Collections.Generic;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Data.EntityNaming;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;

namespace CatalogueLibrary.Data
{
    /// <summary>
    /// A persistent reference to an existing Database Table (See TableInfo).
    /// </summary>
    public interface ITableInfo : IComparable, IHasRuntimeName, IDataAccessPoint, IHasDependencies,
        ICollectSqlParameters, INamed
    {
        /// <summary>
        /// The Schema scope of the table (or blank if dbo / default / not supported by dbms).  This scope exists below Database and Above Table.  Not all database management
        /// engines support the concept of Schema (e.g. MySql).
        /// </summary>
        string Schema { get; }

        /// <summary>
        /// True if the table referenced is an sql server table valued function (which probably takes parameters)
        /// </summary>
        bool IsTableValuedFunction { get; }

        /// <summary>
        /// Gets the name of the table in the given RAW=>STAGING=>LIVE section of a DLE run using the provided <paramref name="tableNamingScheme"/>
        /// </summary>
        /// <param name="bubble"></param>
        /// <param name="tableNamingScheme"></param>
        /// <returns></returns>
        string GetRuntimeName(LoadBubble bubble, INameDatabasesAndTablesDuringLoads tableNamingScheme = null);

        /// <inheritdoc cref="GetRuntimeName(LoadBubble,INameDatabasesAndTablesDuringLoads)"/>
        string GetRuntimeName(LoadStage stage, INameDatabasesAndTablesDuringLoads tableNamingScheme = null);

        /// <summary>
        /// Fetches all the ColumnInfos associated with this TableInfo (This is refreshed every time you call this property)
        /// </summary>
        [NoMappingToDatabase]
        ColumnInfo[] ColumnInfos { get; }

        /// <summary>
        /// Gets all the <see cref="PreLoadDiscardedColumn"/> declared against this table reference.  These are virtual columns which 
        /// do not exist in the LIVE table schema (Unless <see cref="DiscardedColumnDestination.Dilute"/>) but which appear in the RAW 
        /// stage of the data load.  
        /// 
        /// <para>See <see cref="PreLoadDiscardedColumn"/> for more information</para>
        /// </summary>
        [NoMappingToDatabase]
        PreLoadDiscardedColumn[] PreLoadDiscardedColumns { get; }
        
        /// <summary>
        /// True if the <see cref="TableInfo"/> has <see cref="Lookup"/> relationships declared which make it a linkable lookup table in queries.
        /// </summary>
        /// <returns></returns>
        bool IsLookupTable();

        /// <summary>
        /// Returns the <see cref="IDataAccessPoint.Database"/> name at the given <paramref name="loadStage"/> of a DLE run (RAW=>STAGING=>LIVE)
        /// </summary>
        /// <param name="loadStage"></param>
        /// <param name="namer"></param>
        /// <returns></returns>
        string GetDatabaseRuntimeName(LoadStage loadStage, INameDatabasesAndTablesDuringLoads namer = null);

        /// <summary>
        /// Returns all column names for the given <see cref="LoadStage"/> (RAW=>STAGING=>LIVE) of a data load
        /// </summary>
        /// <param name="loadStage"></param>
        /// <returns></returns>
        IEnumerable<IHasStageSpecificRuntimeName> GetColumnsAtStage(LoadStage loadStage);

        /// <summary>
        /// Creates an object for interacting with the table as it exists on the live server referenced by this <see cref="TableInfo"/>
        /// <para>This will not throw if the table doesn't exist, instead you should use <see cref="DiscoveredTable.Exists"/> on the
        /// returned value</para>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        DiscoveredTable Discover(DataAccessContext context);
    }
}
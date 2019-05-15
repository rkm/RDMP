// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Text.RegularExpressions;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTable.Attributes;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.Curation.DataHelper;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.QueryBuilding.SyntaxChecking;
using ReusableLibraryCode.Checks;

namespace Rdmp.Core.QueryBuilding
{
    /// <summary>
    /// Records how (via SQL) replace the private patient identifier column (e.g. CHI) with the release identifier (e.g. swap [biochemistry]..[chi] for 
    /// [cohort]..[ReleaseId]).  Also includes the Join SQL string for linking the cohort table (which contains the ReleaseId e.g. [cohort]) with the dataset
    /// table (e.g. [biochemistry]). 
    /// 
    /// <para>This class is an IColumn and is designed to be added as a new Column to a QueryBuilder as normal (See ExtractionQueryBuilder)</para>
    /// </summary>
    public class ReleaseIdentifierSubstitution :SpontaneousObject, IColumn
    {
        public string JoinSQL { get; private set; }

        /// <inheritdoc/>
        public IColumn OriginalDatasetColumn;

        [Sql]
        public string SelectSQL { get; set; }

        public string Alias { get; private set; }
        
        //all these are hard coded to null or false really
        public ColumnInfo ColumnInfo
        {
            get { return OriginalDatasetColumn.ColumnInfo; }
        }
        public int Order
        {
            get { return OriginalDatasetColumn.Order; }
            set { }
        }
        public bool HashOnDataRelease { get { return false; } }
        public bool IsExtractionIdentifier { get { return OriginalDatasetColumn.IsExtractionIdentifier; } }
        public bool IsPrimaryKey { get { return OriginalDatasetColumn.IsPrimaryKey; } }

        public ReleaseIdentifierSubstitution(MemoryRepository repo,IColumn extractionIdentifierToSubFor, IExtractableCohort extractableCohort, bool isPartOfMultiCHISubstitution):base(repo)
        {
            if(!extractionIdentifierToSubFor.IsExtractionIdentifier)
                throw new Exception("Column " + extractionIdentifierToSubFor + " is not marked IsExtractionIdentifier so cannot be substituted for a ReleaseIdentifier");
            
            OriginalDatasetColumn = extractionIdentifierToSubFor;
            
            if(OriginalDatasetColumn.ColumnInfo == null)
                throw new Exception("The column " + OriginalDatasetColumn.GetRuntimeName() + " references a ColumnInfo that has been deleted");

            var syntaxHelper = extractableCohort.GetQuerySyntaxHelper();

            //the externally referenced Cohort table
            var externalCohortTable = extractableCohort.ExternalCohortTable;
            
            var privateIdentifierFieldDiscovered = externalCohortTable.Discover().ExpectTable(externalCohortTable.TableName).DiscoverColumn(externalCohortTable.PrivateIdentifierField);

            string collateStatement = "";
            
            //the release identifier join might require collation

            //if the private has a collation
            if (!string.IsNullOrWhiteSpace(privateIdentifierFieldDiscovered.Collation))
            {
                var cohortCollation = privateIdentifierFieldDiscovered.Collation;
                var otherTableCollation = OriginalDatasetColumn.ColumnInfo.Collation;


                //only collate if the server types match and if the collations differ
                if(privateIdentifierFieldDiscovered.Table.Database.Server.DatabaseType == OriginalDatasetColumn.ColumnInfo.TableInfo.DatabaseType)
                    if (!string.IsNullOrWhiteSpace(otherTableCollation) && !string.Equals(cohortCollation, otherTableCollation))
                        collateStatement = " collate " + cohortCollation;
            }


            if (!isPartOfMultiCHISubstitution)
            {
                SelectSQL = extractableCohort.GetReleaseIdentifier();
                Alias = syntaxHelper.GetRuntimeName(SelectSQL);
            }
            else
            {
                SelectSQL = "(SELECT DISTINCT " +
                    extractableCohort.GetReleaseIdentifier() + 
                    " FROM " +
                    externalCohortTable.TableName + " WHERE " + extractableCohort.WhereSQL() + " AND " + externalCohortTable.PrivateIdentifierField + "=" + OriginalDatasetColumn.SelectSQL + collateStatement +")";
                
                if(!string.IsNullOrWhiteSpace(OriginalDatasetColumn.Alias))
                {
                    string toReplace = extractableCohort.GetPrivateIdentifier(true);
                    string toReplaceWith = extractableCohort.GetReleaseIdentifier(true);

                    //take the same name as the underlying column
                    Alias = OriginalDatasetColumn.Alias;

                    //but replace all instances of CHI with PROCHI (or Barcode, or whatever)
                    if(!Alias.Contains(toReplace) || Regex.Matches(Alias,Regex.Escape(toReplace)).Count > 1)
                        throw new Exception("Expected OriginalDatasetColumn " + OriginalDatasetColumn.Alias + " to have the text \"" + toReplace + "\" appearing once (and only once in it's name)," +
                                            "we planned to replace that text with:" + toReplaceWith);

                   
                   Alias = Alias.Replace(toReplace,toReplaceWith);
                }
                else
                    throw new Exception("In cases where you have multiple columns marked IsExtractionIdentifier, they must all have Aliases, the column " + OriginalDatasetColumn.SelectSQL + " does not have one");
            }

            JoinSQL = OriginalDatasetColumn.SelectSQL + "=" + externalCohortTable.PrivateIdentifierField + collateStatement;

        }

        public string GetRuntimeName()
        {
            return RDMPQuerySyntaxHelper.GetRuntimeName(this);
        }

        public void Check(ICheckNotifier notifier)
        {
            new ColumnSyntaxChecker(this).Check(notifier);
        }
    }
}
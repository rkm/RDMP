using System;
using System.Web.UI.WebControls;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.DatabaseHelpers.Discovery.QuerySyntax;

namespace CatalogueLibrary.FilterImporting.Construction
{
    /// <summary>
    /// Constructs IFilters etc for main Catalogue database filter (ExtractionFilter).  These are the master filters which are copied out as needed for cohort identification,
    /// extraction etc and therefore do not have any IContainer type (AND/OR).
    /// </summary>
    public class ExtractionFilterFactory : IFilterFactory
    {
        private readonly ICatalogueRepository _repository;
        private readonly ExtractionInformation _extractionInformation;

        public ExtractionFilterFactory(ExtractionInformation extractionInformation)
        {
            _repository = (ICatalogueRepository)extractionInformation.Repository;
            _extractionInformation = extractionInformation;
        }

        public IFilter CreateNewFilter(string name)
        {
            return new ExtractionFilter(_repository, name, _extractionInformation);
        }

        public ISqlParameter CreateNewParameter(IFilter filter, string parameterSQL)
        {
            return new ExtractionFilterParameter(_repository, parameterSQL, (ExtractionFilter)filter);
        }

        public Type GetRootOwnerType()
        {
            return typeof(ExtractionInformation);
        }

        public Type GetIContainerTypeIfAny()
        {
            return null;
        }
    }
}
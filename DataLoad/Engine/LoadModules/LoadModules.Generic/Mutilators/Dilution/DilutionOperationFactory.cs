﻿using System;
using ANOStore.ANOEngineering;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Repositories;

namespace LoadModules.Generic.Mutilators.Dilution
{
    /// <summary>
    /// Creates IDilutionOperations by reflection based on Type name and hydrates with the target IPreLoadDiscardedColumn.  See Dilution.
    /// </summary>
    public class DilutionOperationFactory
    {
        private readonly IPreLoadDiscardedColumn _targetColumn;
        private MEF _mef;

        public DilutionOperationFactory(IPreLoadDiscardedColumn targetColumn)
        {
            if(targetColumn == null)
                throw new ArgumentNullException("targetColumn");

            _targetColumn = targetColumn;
            _mef = ((CatalogueRepository)_targetColumn.Repository).MEF;
        }

        public IDilutionOperation Create(Type operation)
        {
            if(operation == null)
                throw new ArgumentNullException("operation");

            if(!typeof(IDilutionOperation).IsAssignableFrom(operation))
                throw new ArgumentException("Requested operation Type " + operation + " did was not an IDilutionOperation");

            var instance = _mef.FactoryCreateA<IDilutionOperation>(operation.FullName);
            instance.ColumnToDilute = _targetColumn;
            
            return instance;
        }
    }
}

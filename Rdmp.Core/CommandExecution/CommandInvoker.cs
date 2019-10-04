﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MapsDirectlyToDatabaseTable;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Repositories.Construction;
using ReusableLibraryCode.Checks;

namespace Rdmp.Core.CommandExecution
{
    public class CommandInvoker
    {
        private readonly IBasicActivateItems _argumentProvider;
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        
        /// <summary>
        /// Delegates provided by <see cref="_argumentProvider"/> for fulfilling constructor arguments of the key Type
        /// </summary>
        private Dictionary<Type, Func<object>> _argumentDelegates;

        /// <summary>
        /// Called when the user attempts to run a command marked <see cref="ICommandExecution.IsImpossible"/>
        /// </summary>
        public event EventHandler<CommandEventArgs> CommandImpossible;
        
        /// <summary>
        /// Called when a command completes successfully
        /// </summary>
        public event EventHandler<CommandEventArgs> CommandCompleted;

        public CommandInvoker(IBasicActivateItems argumentProvider, IRDMPPlatformRepositoryServiceLocator repositoryLocator)
        {
            _argumentProvider = argumentProvider;
            _repositoryLocator = repositoryLocator;

            _argumentDelegates = _argumentProvider.GetDelegates();
        }

        public IEnumerable<Type> GetSupportedCommands(MEF mef)
        {
            return mef.GetAllTypes().Where(IsSupported);
        }

        public void ExecuteCommand(Type type)
        {
            ExecuteCommand(GetConstructor(type));
        }
        private void ExecuteCommand(ConstructorInfo constructorInfo)
        {
            List<object> parameterValues = new List<object>();

            foreach (var parameterInfo in constructorInfo.GetParameters())
            {
                var paramType = parameterInfo.ParameterType;

                var value = GetValueForParameterOfType(parameterInfo,paramType);
                
                //if it's a null and not a default null
                if(value == null && !parameterInfo.HasDefaultValue)
                    throw new OperationCanceledException("Could not figure out a value for property '" + parameterInfo + "' for constructor '" + constructorInfo + "'.  Parameter Type was '" + paramType + "'");

                parameterValues.Add(value);
            }

            var instance = (IAtomicCommand)constructorInfo.Invoke(parameterValues.ToArray());
        
            if (instance.IsImpossible)
            {
                CommandImpossible?.Invoke(this,new CommandEventArgs(instance));
                return;
            }

            instance.Execute();
            CommandCompleted?.Invoke(this,new CommandEventArgs(instance));
        }
        private object GetValueForParameterOfType(ParameterInfo parameterInfo, Type paramType)
        {
            var key = _argumentDelegates.Keys.FirstOrDefault(k => k.IsAssignableFrom(paramType));

            if (key != null)
                return _argumentDelegates[key]();

            if (typeof(ICatalogueRepository).IsAssignableFrom(paramType))
                return _repositoryLocator.CatalogueRepository;

            if (typeof(IBasicActivateItems).IsAssignableFrom(paramType))
                return _argumentProvider;

            if (typeof(IDataExportRepository).IsAssignableFrom(paramType))
                return _repositoryLocator.DataExportRepository;

            if (typeof(IRDMPPlatformRepositoryServiceLocator).IsAssignableFrom(paramType))
                return _repositoryLocator;

            if (typeof(DirectoryInfo).IsAssignableFrom(paramType))
                return _argumentProvider.PickDirectory(parameterInfo, paramType);

            //it's an array of DatabaseEntities
            if(paramType.IsArray && typeof(DatabaseEntity).IsAssignableFrom(paramType.GetElementType()))
            {
                IMapsDirectlyToDatabaseTable[] available = GetAllObjectsOfType(paramType.GetElementType());
                return _argumentProvider.PickMany(parameterInfo,paramType.GetElementType(), available);
            }

            if (typeof(DatabaseEntity).IsAssignableFrom(paramType))
            {
                IMapsDirectlyToDatabaseTable[] available = GetAllObjectsOfType(paramType);
                return _argumentProvider.SelectOne(parameterInfo.Name, available);
            }

            if (typeof(IMightBeDeprecated).IsAssignableFrom(paramType))
                return SelectOne<IMightBeDeprecated>(parameterInfo);

            if (typeof (IDisableable) == paramType)
                return SelectOne<IDisableable>(parameterInfo);

            if (typeof (INamed) == paramType)
                return SelectOne<INamed>(parameterInfo);
            
            if (typeof (IDeleteable) == paramType)
                return SelectOne<IDeleteable>(parameterInfo);
            
            if (typeof(ICheckable) == paramType)
                return _argumentProvider.SelectOne(parameterInfo.Name, 
                    _argumentProvider.GetAll<ICheckable>()
                    .Where(paramType.IsInstanceOfType)
                    .ToArray());
            
            if (parameterInfo.HasDefaultValue)
                return parameterInfo.DefaultValue;

            if (paramType.IsValueType && !typeof(Enum).IsAssignableFrom(paramType))
                return _argumentProvider.PickValueType(parameterInfo,paramType);
            
            return null;
        }

        private object SelectOne<T>(ParameterInfo parameterInfo)
        {
            return _argumentProvider.SelectOne(parameterInfo.Name,_argumentProvider.GetAll<T>().ToArray());
        }

        public bool IsSupported(ConstructorInfo c)
        {
            return c.GetParameters().All(
                p =>
                    typeof (ICatalogueRepository).IsAssignableFrom(p.ParameterType) ||
                    typeof (IDataExportRepository).IsAssignableFrom(p.ParameterType) ||
                    typeof(IBasicActivateItems).IsAssignableFrom(p.ParameterType) ||
                    typeof (IRDMPPlatformRepositoryServiceLocator).IsAssignableFrom(p.ParameterType) ||
                    _argumentDelegates.Keys.Any(k=>k.IsAssignableFrom(p.ParameterType)) ||
                    typeof(DirectoryInfo).IsAssignableFrom(p.ParameterType) ||
                    p.ParameterType.IsArray && typeof(DatabaseEntity).IsAssignableFrom(p.ParameterType.GetElementType()) ||
                    typeof(DatabaseEntity).IsAssignableFrom(p.ParameterType) ||
                    typeof(ICheckable).IsAssignableFrom(p.ParameterType) ||
                    typeof(IMightBeDeprecated).IsAssignableFrom(p.ParameterType)||
                    typeof(IDeleteable).IsAssignableFrom(p.ParameterType)||
                    typeof(INamed).IsAssignableFrom(p.ParameterType)||
                    p.HasDefaultValue ||
                    (p.ParameterType.IsValueType && !typeof(Enum).IsAssignableFrom(p.ParameterType))
                );
        }

        public bool IsSupported(Type t)
        {
            bool acceptableType = typeof(IAtomicCommand).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface;

            if (!acceptableType)
                return false;

            if (_argumentProvider.GetIgnoredCommands().Contains(t))
                return false;

            try
            {
                var constructor = GetConstructor(t);

                if (constructor == null)
                    return false;

                return IsSupported(constructor);

            }
            catch (Exception)
            {
                return false;
            }
        }

        private static ConstructorInfo GetConstructor(Type type)
        {
            var constructors = type.GetConstructors();

            if (constructors.Length == 0)
                return null;

            var importDecorated = constructors.Where(c => Attribute.IsDefined(c, typeof(UseWithObjectConstructorAttribute))).ToArray();

            if (importDecorated.Any())
                return importDecorated[0];

            return constructors[0];
        }
        private IMapsDirectlyToDatabaseTable[] GetAllObjectsOfType(Type type)
        {
            if (_repositoryLocator.CatalogueRepository.SupportsObjectType(type))
                return  _repositoryLocator.CatalogueRepository.GetAllObjects(type).ToArray();
            if (_repositoryLocator.DataExportRepository.SupportsObjectType(type))
                return _repositoryLocator.DataExportRepository.GetAllObjects(type).ToArray();
            
            return null;
        }
    }
}

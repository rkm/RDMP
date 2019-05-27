// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories.Construction;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;

namespace Rdmp.Core.Repositories
{
    /// <summary>
    /// MEF stands for Managed Extensibility Framework which is a Microsoft library for building Extensions (Plugins) into programs.  It involves decoarting classes as
    /// [Export] or [InheritedExport] and defining contracts, importing constructors, paramters etc.  RDMP makes use of MEF in a limited fashion, it processes all 
    /// Exported classes into a SafeDirectoryCatalog (a collection of MEF AssemblyCatalogs/AggregateCatalog).
    /// 
    /// <para>This class provides support for downloading Plugins out of the Catalogue Database, identifying Exports and building the SafeDirectoryCatalog.  It also includes
    /// methods for creating instances of the exported Types.  Because MEF only gets you so far it also has some generally helpful reflection based methods such as 
    /// GetAllTypesFromAllKnownAssemblies.</para>
    /// </summary>
    public class MEF
    {
        public DirectoryInfo DownloadDirectory { get; private set; }

        public bool HaveDownloadedAllAssemblies = false;
        public SafeDirectoryCatalog SafeDirectoryCatalog;

        ObjectConstructor o = new ObjectConstructor();
                
        private readonly string _localPath = null;

        public MEF()
        {
            //try to use the app data folder to download MEF but also evaluate everything in _localPath
            _localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);

            string _MEFPathAsString;

            try
            {
                _MEFPathAsString = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MEF");
            }
            catch (Exception)//couldnt get the AppData/MEF directory so instead go to .\MEF\
            {
                if (_localPath == null)
                    throw new Exception("ApplicationData was not available to download MEF and neither apparently was Assembly.GetExecutingAssembly().GetName().CodeBase");

                _MEFPathAsString = Path.Combine(_localPath, "MEF");
            }
            DownloadDirectory = new DirectoryInfo(_MEFPathAsString);
        }

        

        public Type GetType(string type)
        {            
            if(!SafeDirectoryCatalog.TypesByName.ContainsKey(type))
                throw new KeyNotFoundException("Could not find a type called "+ type);
            
            return SafeDirectoryCatalog.TypesByName[type];
        }
        public Type GetType(string type, Type expectedBaseClass)
        {
            var t = GetType(type);

            if(!expectedBaseClass.IsAssignableFrom(t))
                throw new Exception("Found Type '" + type + "' did not implement expected base class/interface '" + expectedBaseClass +"'" );


            return t;
        }
        public void Setup(SafeDirectoryCatalog result)
        {
            SafeDirectoryCatalog = result;
            HaveDownloadedAllAssemblies = true;
        }
        
        public void SetupMEFIfRequired()
        {
            if (!HaveDownloadedAllAssemblies)
                throw new NotSupportedException("MEF was not loaded by Startup?!!");
        }
        
        /// <summary>
        /// Makes the given Type appear as a MEF exported class.  Can be used to test your types without 
        /// building and committing an actual <see cref="Plugin"/>
        /// </summary>
        /// <param name="type"></param>
        public void AddTypeToCatalogForTesting(Type type)
        {
            SetupMEFIfRequired();

            SafeDirectoryCatalog.AddType(type);
        }

        public Dictionary<string, Exception> ListBadAssemblies()
        {
            SetupMEFIfRequired();

            return SafeDirectoryCatalog.BadAssembliesDictionary;
        }

        /// <summary>
        /// Turns the legit C# name:
        /// DataLoadEngine.DataFlowPipeline.IDataFlowSource`1[[System.Data.DataTable, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
        /// 
        /// <para>Into a freaky MEF name:
        /// DataLoadEngine.DataFlowPipeline.IDataFlowSource(System.Data.DataTable)</para>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetMEFNameForType(Type t)
        {
            if (t.IsGenericType)
            {
                if (t.GenericTypeArguments.Count() != 1)
                    throw new NotSupportedException("Generic type has more than 1 token (e.g. T1,T2) so no idea what MEF would call it");
                string genericTypeName = t.GetGenericTypeDefinition().FullName;

                Debug.Assert(genericTypeName.EndsWith("`1"));
                genericTypeName = genericTypeName.Substring(0, genericTypeName.Length - "`1".Length);

                string underlyingType = t.GenericTypeArguments.Single().FullName;
                return genericTypeName + "(" + underlyingType + ")";
            }

            return t.FullName;
        }
        
        /// <summary>
        /// 
        /// <para>Turns the legit C# name: 
        /// DataLoadEngine.DataFlowPipeline.IDataFlowSource`1[[System.Data.DataTable, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]</para>
        /// 
        /// <para>Into a proper C# code:
        /// IDataFlowSource&lt;DataTable&gt;</para>
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetCSharpNameForType(Type t)
        {
            if (t.IsGenericType)
            {
                if (t.GenericTypeArguments.Count() != 1)
                    throw new NotSupportedException("Generic type has more than 1 token (e.g. T1,T2) so no idea what MEF would call it");
                string genericTypeName = t.GetGenericTypeDefinition().Name;

                Debug.Assert(genericTypeName.EndsWith("`1"));
                genericTypeName = genericTypeName.Substring(0, genericTypeName.Length - "`1".Length);

                string underlyingType = t.GenericTypeArguments.Single().Name;
                return genericTypeName + "<" + underlyingType + ">";
            }

            return t.Name;
        }

        public void CheckForVersionMismatches(ICheckNotifier notifier)
        {
            SetupMEFIfRequired();

            DirectoryInfo root = new DirectoryInfo(".");

            var binDirectoryFiles = root.EnumerateFiles().ToArray();

            foreach (FileInfo dllInMEFFolder in DownloadDirectory.GetFiles())
            {
                FileInfo dllInBinFolder = binDirectoryFiles.FirstOrDefault(f => f.Name.Equals(dllInMEFFolder.Name));

                if (dllInBinFolder != null)
                {
                    string md5Bin = UsefulStuff.HashFile(dllInBinFolder.FullName);
                    string md5Mef = UsefulStuff.HashFile(dllInMEFFolder.FullName);

                    if (!md5Bin.Equals(md5Mef))
                    {
                        notifier.OnCheckPerformed(new CheckEventArgs("Different versions of the dll exist in MEF and BIN directory:" + Environment.NewLine +
                             dllInBinFolder.FullName + " (MD5=" + md5Bin + ")" + Environment.NewLine +
                             "Version:" + FileVersionInfo.GetVersionInfo(dllInBinFolder.FullName).FileVersion + Environment.NewLine +
                             "and" + Environment.NewLine +
                             dllInMEFFolder.FullName + " (MD5=" + md5Mef + ")" + Environment.NewLine +
                             "Version:" + FileVersionInfo.GetVersionInfo(dllInMEFFolder.FullName).FileVersion + Environment.NewLine
                        , CheckResult.Warning, null));

                    }
                }
            }
        }
        public IEnumerable<Type> GetTypes<T>()
        {
            SetupMEFIfRequired();

            return GetTypes(typeof(T));
        }

        object _cachedImplementationsLock = new object();
        Dictionary<Type,Type[]> _cachedImplementations = new Dictionary<Type, Type[]>();

        /// <summary>
        /// Returns MEF exported Types which inherit or implement <paramref name="type"/>.  E.g. pass IAttacher to see
        /// all exported implementers
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetTypes(Type type)
        {
            SetupMEFIfRequired();

            lock(_cachedImplementationsLock)
            {
                if(_cachedImplementations.ContainsKey(type))
                return _cachedImplementations[type];

                Type[] results = SafeDirectoryCatalog.GetAllTypes().Where(t=>type.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).ToArray();
                _cachedImplementations.Add(type,results);
                return results;
            }
        }

        /// <summary>
        /// Returns all MEF exported classes decorated with the specified generic export e.g. 
        /// </summary>
        /// <param name="genericType"></param>
        /// <param name="typeOfT"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetGenericTypes(Type genericType, Type typeOfT)
        {
            return GetTypes(genericType.MakeGenericType(typeOfT));
        }

        public IEnumerable<Type> GetAllTypes()
        {
            SetupMEFIfRequired();

            return SafeDirectoryCatalog.GetAllTypes();
        }
                
        /// <summary>
        /// Creates an instance of the named class with the provided constructor args
        /// 
        /// <para>IMPORTANT: this will create classes from the MEF Exports ONLY i.e. not any loaded type but has to be an explicitly labled Export of a LoadModuleAssembly</para>
        /// </summary>
        /// <typeparam name="T">The base/interface of the Type you want to create e.g. IAttacher</typeparam>
        /// <returns></returns>
        public T CreateA<T>(string typeToCreate, params object[] args)
        {
            Type typeToCreateAsType = GetType(typeToCreate);
            
            T instance = (T)o.ConstructIfPossible(typeToCreateAsType,args);

            if(instance == null)
                throw new ObjectLacksCompatibleConstructorException("Could not construct a " + typeof(T) + " using the " + args.Length + " constructor arguments" );

            return instance;
        }

    }
}

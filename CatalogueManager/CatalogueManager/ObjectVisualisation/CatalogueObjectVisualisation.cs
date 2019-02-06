// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cohort;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Reports;
using CatalogueManager.Collections.Providers;
using CatalogueManager.Icons.IconProvision;
using DataExportLibrary.Data.DataTables;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Comments;
using ReusableUIComponents.Dependencies.Models;
using Sharing.Dependency.Gathering;

namespace CatalogueManager.ObjectVisualisation
{
    public class CatalogueObjectVisualisation : IObjectVisualisation
    {
        private readonly ICoreIconProvider _coreIconProvider;
        private Dictionary<Type, string> _summaries;

        public CatalogueObjectVisualisation(CommentStore commentStore, ICoreIconProvider coreIconProvider)
        {
            _coreIconProvider = coreIconProvider;

            var documentation = new DocumentationReportMapsDirectlyToDatabase(commentStore,typeof(Catalogue).Assembly, typeof(ExtractionConfiguration).Assembly);
            documentation.Check(new IgnoreAllErrorsCheckNotifier());
            _summaries = documentation.Summaries;
        }

        public string[] GetNameAndType(object toRender)
        {
            toRender = CollapseObjectIfNessesary(toRender);
            
            var idPropertyInfo = toRender.GetType().GetProperty("ID");
            string idAsString = null;
            if (idPropertyInfo != null)
                idAsString = idPropertyInfo.GetValue(toRender).ToString();

            string[] nameAndType = new string[3];
            nameAndType[0] = toRender.ToString();
            nameAndType[1] = " (" + toRender.GetType().Name + (idAsString != null ? " ID=" + idAsString : "") + ")";
            nameAndType[2] = toRender.GetType().Name;
            return nameAndType;

        }

        private object CollapseObjectIfNessesary(object toRender)
        {
            var masquerade = toRender as IMasqueradeAs;
            
            return masquerade!= null? masquerade.MasqueradingAs():toRender;
        }

        //The dictionary has space for 3 segments of information. The first entry in the dictionary
        //is placed in the Rich Textbox (hence why description is almost always the first entry)
        public OrderedDictionary EntityInformation(object toRender)
        {
            toRender = CollapseObjectIfNessesary(toRender);
            
            OrderedDictionary informationToReturn = new OrderedDictionary();

            if (_summaries != null && _summaries.ContainsKey(toRender.GetType()))
                informationToReturn.Add("Type Purpose: ", _summaries[toRender.GetType()]);
            else
                informationToReturn.Add("Type Purpose: ", "Unknown");


            if (toRender.GetType() == typeof(Catalogue))
            {
                informationToReturn.Add("Description: ", ((Catalogue)toRender).Description);
            }
            if (toRender.GetType() == typeof(CatalogueItem))
            {
                informationToReturn.Add("Description: ", ((CatalogueItem)toRender).Description);
                informationToReturn.Add("Comments: ", ((CatalogueItem)toRender).Comments);
            }
            if (toRender.GetType() == typeof(TableInfo))
            {
                informationToReturn.Add("Store Type:  ", ((TableInfo)toRender).DatabaseType);
            }

            if (toRender.GetType() == typeof(ColumnInfo))
            {
                informationToReturn.Add("Description: ", ((ColumnInfo)toRender).Description);
            }

            if (toRender.GetType() == typeof(ExtractionInformation))
            {
                informationToReturn.Add("Select SQL: ", ((ExtractionInformation)toRender).SelectSQL);
                informationToReturn.Add("Extraction Category: ", ((ExtractionInformation)toRender).ExtractionCategory.ToString());
            }

            if (toRender.GetType() == typeof(LoadMetadata))
            {
                informationToReturn.Add("Description: ", ((LoadMetadata)toRender).Description);
            }

            if (toRender.GetType() == typeof(ExtractionFilter))
            {
                informationToReturn.Add("Description: ", ((ExtractionFilter)toRender).Description);
            }

            if (toRender.GetType() == typeof(ExtractionFilterParameter))
            {
                informationToReturn.Add("Parameter SQL: ", ((ExtractionFilterParameter)toRender).ParameterSQL);
                informationToReturn.Add("Parameter Name: ", ((ExtractionFilterParameter)toRender).ParameterName);
            }

            if (toRender.GetType() == typeof(Lookup))
            {
                informationToReturn.Add("Description: ", ((Lookup)toRender).Description.ToString());
                informationToReturn.Add("Primary Key: ", ((Lookup)toRender).PrimaryKey.ToString());
                informationToReturn.Add("Foreign Key: ", ((Lookup)toRender).ForeignKey.ToString());
            }

            return informationToReturn;
        }

        public ColorResponse GetColor(object toRender, ColorRequest request)
        {
            toRender = CollapseObjectIfNessesary(toRender);

            if (request.IsHighlighted)
                return new ColorResponse(KnownColor.LightPink, KnownColor.White);

            if (toRender is ExtractionInformation)
                if (((ExtractionInformation)toRender).IsProperTransform())
                    return new ColorResponse(KnownColor.LawnGreen, KnownColor.White);

            return new ColorResponse(KnownColor.LightBlue, KnownColor.White);
        }


        public Bitmap GetImage(object toRender)
        {
            toRender = CollapseObjectIfNessesary(toRender);

            var img = (Bitmap)_coreIconProvider.GetImage(toRender);
            
            if (img == null)
                throw new NotSupportedException("Did not know what image to serve for object of type " + toRender.GetType().FullName + " (Icon provider was of Type '" + _coreIconProvider.GetType().Name +"')");

            return img;
        }
    }


}

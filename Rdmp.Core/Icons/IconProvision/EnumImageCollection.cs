// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using System.Linq;
using System.Resources;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace Rdmp.Core.Icons.IconProvision
{
    public class EnumImageCollection<T> where T : struct, IConvertible
    {
        readonly Dictionary<T,Image> _images = new Dictionary<T, Image>();

        public EnumImageCollection(ResourceManager resourceManager)
        {
            if (!typeof (T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            List<string> missingImages = new List<string>();

            foreach (var enumValue in Enum.GetValues(typeof(T)))
            {
                var bmp = (Image)resourceManager.GetObject(enumValue.ToString());
                if(bmp == null)
                    missingImages.Add(enumValue.ToString());

                _images.Add((T) enumValue,bmp);
            }

            if(missingImages.Any())
                throw new IconProvisionException(
                    $"The following expected images were missing from {resourceManager.BaseName}.resx{Environment.NewLine}{string.Join("," + Environment.NewLine, missingImages)}");
        }

        public Image<Argb32> this[T index]
        {
            get { return _images[index]; }
        }

        public Dictionary<string, Image> ToStringDictionary(int newSizeInPixels = -1)
        {
            var toReturn = _images.ToDictionary(k => k.Key.ToString(), v => v.Value);

            if (newSizeInPixels != -1)
                toReturn = Resize(toReturn,newSizeInPixels);

            return toReturn;
        }

        private Dictionary<string, Image> Resize(Dictionary<string, Image> dictionary, int newSizeInPixels)
        {
            foreach (var k in dictionary.Keys.ToArray())
            {
                var img = dictionary[k].CloneAs<Argb32>();
                img.Mutate(x=>x.Resize(newSizeInPixels,newSizeInPixels));
                dictionary[k] = img;
            }

            return dictionary;
        }
    }
}
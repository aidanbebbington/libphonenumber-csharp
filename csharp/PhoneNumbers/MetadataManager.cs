/*
 * Copyright (C) 2012 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;

namespace PhoneNumbers
{
    /**
    * Class encapsulating loading of PhoneNumber Metadata information. Currently this is used only for
    * additional data files such as PhoneNumberAlternateFormats, but in the future it is envisaged it
    * would handle the main metadata file (PhoneNumberMetaData.xml) as well.
    *
    * @author Lara Rennie
    */
    public static class MetadataManager
    {
        private static class AlternateFormats
        {
            public static readonly Dictionary<int, PhoneMetadata> Map =
                BuildMetadataFromXml.BuildPhoneMetadataCollection("PhoneNumberAlternateFormats.xml", isAlternateFormatsMetadata: true).MetadataList.ToDictionary(m => m.CountryCode);
        }

        private static class ShortNumber
        {
            // A mapping from a region code to the short number metadata for that region code.
            public static readonly Dictionary<string, PhoneMetadata> MetadataMap =
                BuildMetadataFromXml.BuildPhoneMetadataCollection("ShortNumberMetadata.xml", isShortNumberMetadata: true).MetadataList.ToDictionary(m => m.Id);
        }

        public static PhoneMetadata GetAlternateFormatsForCountry(int countryCallingCode)
        {
            return AlternateFormats.Map.TryGetValue(countryCallingCode, out var metadata) ? metadata : null;
        }

        internal static PhoneMetadata GetShortNumberMetadataForRegion(string regionCode)
        {
            if (!ShortNumbersRegionCodeSet.RegionCodeSet.Contains(regionCode))
                return null;
            return ShortNumber.MetadataMap.TryGetValue(regionCode, out var metadata) ? metadata : null;
        }
    }
}

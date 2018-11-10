/*
 * Copyright (C) 2016 The Libphonenumber Authors
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

using System;
using System.Linq;
using System.Collections.Generic;
using PhoneNumbers;

namespace Tools
{

    /**
     * Class to encapsulate the metadata filtering logic and restrict visibility into raw data
     * structures.
     *
     * <p>
     * WARNING: This is an internal API which is under development and subject to backwards-incompatible
     * changes without notice. Any changes are not guaranteed to be reflected in the versioning scheme
     * of the public API, nor in release notes.
     */
    public class MetadataFilter
    {
        // The following 3 sets comprise all the PhoneMetadata fields as defined at phonemetadata.proto
        // which may be excluded from customized serializations of the binary metadata. Fields that are
        // core to the library functionality may not be listed here.
        // ExcludableParentFields are PhoneMetadata fields of type PhoneNumberDesc.
        // ExcludableChildFields are PhoneNumberDesc fields of primitive type.
        // ExcludableChildlessFields are PhoneMetadata fields of primitive type.
        // Currently we support only one non-primitive type and the depth of the "family tree" is 2,
        // meaning a field may have only direct descendants, who may not have descendants of their own. If
        // this changes, the blacklist handling in this class should also change.
        internal static readonly SortedSet<string> ExcludableParentFields = new SortedSet<string>
        {
            "fixedLine",
            "mobile",
            "tollFree",
            "premiumRate",
            "sharedCost",
            "personalNumber",
            "voip",
            "pager",
            "uan",
            "emergency",
            "voicemail",
            "shortCode",
            "standardRate",
            "carrierSpecific",
            "smsServices",
            "noInternationalDialling"
        };

        // Note: If this set changes, the descHasData implementation must change in PhoneNumberUtil.
        // The current implementation assumes that all PhoneNumberDesc fields are present here, since it
        // "clears" a PhoneNumberDesc field by simply clearing all of the fields under it. See the comment
        // above, about all 3 sets, for more about these fields.
        internal static readonly SortedSet<string> ExcludableChildFields = new SortedSet<string>
        {
            "nationalNumberPattern",
            "possibleLength",
            "possibleLengthLocalOnly",
            "exampleNumber"
        };

        internal static readonly SortedSet<string> ExcludableChildlessFields = new SortedSet<string>
        {
            "preferredInternationalPrefix",
            "nationalPrefix",
            "preferredExtnPrefix",
            "nationalPrefixTransformRule",
            "sameMobileAndFixedLinePattern",
            "mainCountryForCode",
            "leadingZeroPossible",
            "mobileNumberPortableRegion"
        };

        private readonly Dictionary<string, SortedSet<string>> blacklist;

        internal MetadataFilter(Dictionary<string, SortedSet<string>> blacklist)
        {
            this.blacklist = blacklist;
        }

        // Note: If changing the blacklist here or the name of the method, update documentation about
        // affected methods at the same time:
        // https://github.com/googlei18n/libphonenumber/blob/master/FAQ.md#what-is-the-metadatalitejsmetadata_lite-option
        internal static MetadataFilter ForLiteBuild()
        {
            // "exampleNumber" is a blacklist.
            return new MetadataFilter(ParseFieldMapFromString("exampleNumber"));
        }

        internal static MetadataFilter ForSpecialBuild()
        {
            // "mobile" is a whitelist.
            return new MetadataFilter(ComputeComplement(ParseFieldMapFromString("mobile")));
        }

        internal static MetadataFilter EmptyFilter()
        {
            // Empty blacklist, meaning we filter nothing.
            return new MetadataFilter(new Dictionary<string, SortedSet<string>>());
        }

        public override bool Equals(object obj)
            => blacklist.Count == ((MetadataFilter) obj)?.blacklist?.Count &&
               blacklist.All(kvp =>
                   ((MetadataFilter) obj).blacklist.TryGetValue(kvp.Key, out var value2) && kvp.Value.SetEquals(value2));

        public override int GetHashCode()
        {
            unchecked
            {
                return blacklist.GetType().GetHashCode() ^ blacklist
                           .Select(kvp => kvp.Key.GetHashCode() * 17 ^ kvp.Value.GetHashCode() * 23)
                           .Aggregate((a, b) => a ^ b);
            }
        }

        /**
         * Clears certain fields in {@code metadata} as defined by the {@code MetadataFilter} instance.
         * Note that this changes the mutable {@code metadata} object, and is not thread-safe. If this
         * method does not return successfully, do not assume {@code metadata} has not changed.
         *
         * @param metadata  The {@code PhoneMetadata} object to be filtered
         */
        internal void FilterMetadata(PhoneMetadata.Builder metadata)
        {
            // TODO: Consider clearing if the filtered PhoneNumberDesc is empty.
            if (metadata.HasFixedLine)
            {
                metadata.SetFixedLine(GetFiltered("fixedLine", metadata.FixedLine));
            }
            if (metadata.HasMobile)
            {
                metadata.SetMobile(GetFiltered("mobile", metadata.Mobile));
            }
            if (metadata.HasTollFree)
            {
                metadata.SetTollFree(GetFiltered("tollFree", metadata.TollFree));
            }
            if (metadata.HasPremiumRate)
            {
                metadata.SetPremiumRate(GetFiltered("premiumRate", metadata.PremiumRate));
            }
            if (metadata.HasSharedCost)
            {
                metadata.SetSharedCost(GetFiltered("sharedCost", metadata.SharedCost));
            }
            if (metadata.HasPersonalNumber)
            {
                metadata.SetPersonalNumber(GetFiltered("personalNumber", metadata.PersonalNumber));
            }
            if (metadata.HasVoip)
            {
                metadata.SetVoip(GetFiltered("voip", metadata.Voip));
            }
            if (metadata.HasPager)
            {
                metadata.SetPager(GetFiltered("pager", metadata.Pager));
            }
            if (metadata.HasUan)
            {
                metadata.SetUan(GetFiltered("uan", metadata.Uan));
            }
            if (metadata.HasEmergency)
            {
                metadata.SetEmergency(GetFiltered("emergency", metadata.Emergency));
            }
            if (metadata.HasVoicemail)
            {
                metadata.SetVoicemail(GetFiltered("voicemail", metadata.Voicemail));
            }
            if (metadata.HasShortCode)
            {
                metadata.SetShortCode(GetFiltered("shortCode", metadata.ShortCode));
            }
            if (metadata.HasStandardRate)
            {
                metadata.SetStandardRate(GetFiltered("standardRate", metadata.StandardRate));
            }
            if (metadata.HasCarrierSpecific)
            {
                metadata.SetCarrierSpecific(GetFiltered("carrierSpecific", metadata.CarrierSpecific));
            }
            if (metadata.HasSmsServices)
            {
                metadata.SetSmsServices(GetFiltered("smsServices", metadata.SmsServices));
            }
            if (metadata.HasNoInternationalDialling)
            {
                metadata.SetNoInternationalDialling(GetFiltered("noInternationalDialling",
                    metadata.NoInternationalDialling));
            }

            if (ShouldDrop("preferredInternationalPrefix"))
            {
                metadata.ClearPreferredInternationalPrefix();
            }
            if (ShouldDrop("nationalPrefix"))
            {
                metadata.ClearNationalPrefix();
            }
            if (ShouldDrop("preferredExtnPrefix"))
            {
                metadata.ClearPreferredExtnPrefix();
            }
            if (ShouldDrop("nationalPrefixTransformRule"))
            {
                metadata.ClearNationalPrefixTransformRule();
            }
            if (ShouldDrop("sameMobileAndFixedLinePattern"))
            {
                metadata.ClearSameMobileAndFixedLinePattern();
            }
            if (ShouldDrop("mainCountryForCode"))
            {
                metadata.ClearMainCountryForCode();
            }
            if (ShouldDrop("leadingZeroPossible"))
            {
                metadata.ClearLeadingZeroPossible();
            }
            if (ShouldDrop("mobileNumberPortableRegion"))
            {
                metadata.ClearMobileNumberPortableRegion();
            }
        }

        /**
         * The input blacklist or whitelist string is expected to be of the form "a(b,c):d(e):f", where
         * b and c are children of a, e is a child of d, and f is either a parent field, a child field, or
         * a childless field. Order and whitespace don't matter. We throw Exception for any
         * duplicates, malformed strings, or strings where field tokens do not correspond to strings in
         * the sets of excludable fields. We also throw Exception for empty strings since such
         * strings should be treated as a special case by the flag checking code and not passed here.
         */
        internal static Dictionary<string, SortedSet<string>> ParseFieldMapFromString(string str)
        {
            if (str == null)
            {
                throw new Exception("Null string should not be passed to ParseFieldMapFromString");
#if NET35
            if (string.IsNullOrEmpty(str) || str.All(char.IsWhiteSpace))
#else
            if (string.IsNullOrWhiteSpace(str))
#endif
                throw new Exception("Null nor empty string should not be passed to ParseFieldMapFromString");
            }

            var fieldMap = new Dictionary<string, SortedSet<string>>();
            var wildcardChildren = new SortedSet<string>();
            foreach (var group in str.Split(':').Select(s => s.Trim()))
            {
                var leftParenIndex = group.IndexOf('(');
                var rightParenIndex = group.IndexOf(')');
                if (leftParenIndex < 0 && rightParenIndex < 0)
                {
                    if (ExcludableParentFields.Contains(group))
                    {
                        if (fieldMap.ContainsKey(group))
                        {
                            throw new Exception(group + " given more than once in " + str);
                        }
                        fieldMap.Add(group, new SortedSet<string>(ExcludableChildFields));
                    }
                    else if (ExcludableChildlessFields.Contains(group))
                    {
                        if (fieldMap.ContainsKey(group))
                        {
                            throw new Exception(group + " given more than once in " + str);
                        }
                        fieldMap.Add(group, new SortedSet<string>());
                    }
                    else if (ExcludableChildFields.Contains(group))
                    {
                        if (wildcardChildren.Contains(group))
                        {
                            throw new Exception(group + " given more than once in " + str);
                        }
                        wildcardChildren.Add(group);
                    }
                    else
                    {
                        throw new Exception(group + " is not a valid token");
                    }
                }
                else if (leftParenIndex > 0 && rightParenIndex == group.Length - 1)
                {
                    // We don't check for duplicate parentheses or illegal characters since these will be caught
                    // as not being part of valid field tokens.
                    var parent = group.Substring(0, leftParenIndex).Trim();
                    if (!ExcludableParentFields.Contains(parent))
                    {
                        throw new Exception(parent + " is not a valid parent token");
                    }
                    if (fieldMap.ContainsKey(parent))
                    {
                        throw new Exception(parent + " given more than once in " + str);
                    }
                    var children = new SortedSet<string>();
                    foreach (var child in group.Substring(leftParenIndex + 1, rightParenIndex - leftParenIndex - 1)
                        .Split(',').Select(s => s.Trim()))
                    {
                        if (!ExcludableChildFields.Contains(child))
                        {
                            throw new Exception(child + " is not a valid child token");
                        }
                        if (!children.Add(child))
                        {
                            throw new Exception(child + " given more than once in " + group);
                        }
                    }
                    fieldMap.Add(parent, children);
                }
                else
                {
                    throw new Exception("Incorrect location of parantheses in " + group);
                }
            }
            foreach (var wildcardChild in wildcardChildren)
            {
                foreach (var parent in ExcludableParentFields)
                {
                    var children = fieldMap[parent];
                    if (children == null)
                    {
                        children = new SortedSet<string>();
                        fieldMap.Add(parent, children);
                    }
                    if (!children.Add(wildcardChild)
                        && fieldMap[parent].Count != ExcludableChildFields.Count)
                    {
                        // The map already Contains parent -> wildcardChild but not all possible children.
                        // So wildcardChild was given explicitly as a child of parent, which is a duplication
                        // since it's also given as a wildcard child.
                        throw new Exception(
                            wildcardChild + " is present by itself so remove it from " + parent + "'s group");
                    }
                }
            }
            return fieldMap;
        }

        // Does not check that legal tokens are used, assuming that fieldMap is constructed using
        // ParseFieldMapFromString(String) which does check. If fieldMap Contains illegal tokens or parent
        // fields with no children or other unexpected state, the behavior of this function is undefined.
        internal static Dictionary<string, SortedSet<string>> ComputeComplement(
            IDictionary<string, SortedSet<string>> fieldMap)
        {
            var complement = new Dictionary<string, SortedSet<string>>();
            foreach (var parent in ExcludableParentFields)
            {
                if (!fieldMap.ContainsKey(parent))
                {
                    complement.Add(parent, new SortedSet<string>(ExcludableChildFields));
                }
                else
                {
                    var otherChildren = fieldMap[parent];
                    // If the other map has all the children for this parent then we don't want to include the
                    // parent as a key.
                    if (otherChildren.Count != ExcludableChildFields.Count)
                    {
                        var children = new SortedSet<string>();
                        foreach (var child in ExcludableChildFields)
                        {
                            if (!otherChildren.Contains(child))
                            {
                                children.Add(child);
                            }
                        }
                        complement.Add(parent, children);
                    }
                }
            }
            foreach (var childlessField in ExcludableChildlessFields)
            {
                if (!fieldMap.ContainsKey(childlessField))
                {
                    complement.Add(childlessField, new SortedSet<string>());
                }
            }
            return complement;
        }

        internal bool ShouldDrop(string parent, string child)
        {
            if (!ExcludableParentFields.Contains(parent))
            {
                throw new Exception(parent + " is not an excludable parent field");
            }
            if (!ExcludableChildFields.Contains(child))
            {
                throw new Exception(child + " is not an excludable child field");
            }
            return blacklist.ContainsKey(parent) && blacklist[parent].Contains(child);
        }

        internal bool ShouldDrop(string childlessField)
        {
            if (!ExcludableChildlessFields.Contains(childlessField))
            {
                throw new Exception(childlessField + " is not an excludable childless field");
            }
            return blacklist.ContainsKey(childlessField);
        }

        private PhoneNumberDesc GetFiltered(string type, PhoneNumberDesc desc)
        {
            var builder = new PhoneNumberDesc.Builder().MergeFrom(desc);
            if (ShouldDrop(type, "nationalNumberPattern"))
            {
                builder.ClearNationalNumberPattern();
            }
            if (ShouldDrop(type, "possibleLength"))
            {
                builder.ClearPossibleLength();
            }
            if (ShouldDrop(type, "possibleLengthLocalOnly"))
            {
                builder.ClearPossibleLengthLocalOnly();
            }
            if (ShouldDrop(type, "exampleNumber"))
            {
                builder.ClearExampleNumber();
            }
            return builder.Build();
        }
    }
}
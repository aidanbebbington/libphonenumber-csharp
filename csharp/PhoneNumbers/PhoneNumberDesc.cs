using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PhoneNumbers
{
    [DebuggerNonUserCode]
    [CompilerGenerated]
    [GeneratedCode("ProtoGen", "2.3.0.277")]
    public class PhoneNumberDesc
    {
        public const int NationalNumberPatternFieldNumber = 2;

        public const int PossibleLengthFieldNumber = 9;

        public const int PossibleLengthLocalOnlyFieldNumber = 10;

        public const int ExampleNumberFieldNumber = 6;
        private readonly List<int> possibleLength_ = new();
        private readonly List<int> possibleLengthLocalOnly_ = new();

        public static PhoneNumberDesc DefaultInstance { get; } = new Builder().BuildPartial();

        public PhoneNumberDesc DefaultInstanceForType => DefaultInstance;

        protected PhoneNumberDesc ThisMessage => this;

        public bool HasNationalNumberPattern { get; private set; }

        public string NationalNumberPattern { get; private set; } = "";

        public IList<int> PossibleLengthList => possibleLength_;

        public int PossibleLengthCount => possibleLength_.Count;

        public IList<int> PossibleLengthLocalOnlyList => possibleLengthLocalOnly_;

        public int PossibleLengthLocalOnlyCount => possibleLengthLocalOnly_.Count;

        public bool HasExampleNumber { get; private set; }

        public string ExampleNumber { get; private set; } = "";

        public bool IsInitialized => true;

        public int GetPossibleLength(int index) => possibleLength_[index];

        public int GetPossibleLengthLocalOnly(int index) => possibleLengthLocalOnly_[index];

        public static Builder CreateBuilder() => new();

        public Builder ToBuilder() => CreateBuilder(this);

        public Builder CreateBuilderForType() => new();

        public static Builder CreateBuilder(PhoneNumberDesc prototype)
        {
            return new Builder().MergeFrom(prototype);
        }

        [DebuggerNonUserCode]
        [CompilerGenerated]
        [GeneratedCode("ProtoGen", "2.3.0.277")]
        public class Builder
        {
            protected Builder ThisBuilder => this;

            protected PhoneNumberDesc MessageBeingBuilt { get; private set; } = new();

            public PhoneNumberDesc DefaultInstanceForType => DefaultInstance;


            public bool HasNationalNumberPattern => MessageBeingBuilt.HasNationalNumberPattern;

            public string NationalNumberPattern
            {
                get => MessageBeingBuilt.NationalNumberPattern;
                set => SetNationalNumberPattern(value);
            }

            public IList<int> PossibleLengthList => MessageBeingBuilt.possibleLength_;

            public int PossibleLengthCount => MessageBeingBuilt.PossibleLengthCount;

            public IList<int> PossibleLengthLocalOnlyList => MessageBeingBuilt.possibleLengthLocalOnly_;

            public int PossibleLengthLocalOnlyCount => MessageBeingBuilt.PossibleLengthLocalOnlyCount;

            public bool HasExampleNumber => MessageBeingBuilt.HasExampleNumber;

            public string ExampleNumber
            {
                get => MessageBeingBuilt.ExampleNumber;
                set => SetExampleNumber(value);
            }

            public Builder Clear()
            {
                MessageBeingBuilt = new();
                return this;
            }

            public Builder Clone()
            {
                return new Builder().MergeFrom(MessageBeingBuilt);
            }

            public PhoneNumberDesc Build()
            {
                return BuildPartial();
            }

            public PhoneNumberDesc BuildPartial()
            {
                if (MessageBeingBuilt == null)
                    throw new InvalidOperationException("build() has already been called on this Builder");


                var returnMe = MessageBeingBuilt;
                MessageBeingBuilt = null;
                return returnMe;
            }


            public Builder MergeFrom(PhoneNumberDesc other)
            {
                if (other == DefaultInstance) return this;
                if (other.HasNationalNumberPattern) NationalNumberPattern = other.NationalNumberPattern;
                if (other.possibleLength_.Count != 0) MessageBeingBuilt.possibleLength_.AddRange(other.possibleLength_);
                if (other.possibleLengthLocalOnly_.Count != 0)
                    MessageBeingBuilt.possibleLengthLocalOnly_.AddRange(other.possibleLengthLocalOnly_);
                if (other.HasExampleNumber) ExampleNumber = other.ExampleNumber;
                return this;
            }

            public Builder SetNationalNumberPattern(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.HasNationalNumberPattern = true;
                MessageBeingBuilt.NationalNumberPattern = value;
                return this;
            }

            public Builder ClearNationalNumberPattern()
            {
                MessageBeingBuilt.HasNationalNumberPattern = false;
                MessageBeingBuilt.NationalNumberPattern = "";
                return this;
            }

            public int GetPossibleLength(int index)
            {
                return MessageBeingBuilt.GetPossibleLength(index);
            }

            public Builder SetPossibleLength(int index, int value)
            {
                MessageBeingBuilt.possibleLength_[index] = value;
                return this;
            }

            public Builder AddPossibleLength(int value)
            {
                MessageBeingBuilt.possibleLength_.Add(value);
                return this;
            }

            public Builder AddRangePossibleLength(IEnumerable<int> values)
            {
                MessageBeingBuilt.possibleLength_.AddRange(values);
                return this;
            }

            public Builder ClearPossibleLength()
            {
                MessageBeingBuilt.possibleLength_.Clear();
                return this;
            }

            public int GetPossibleLengthLocalOnly(int index)
            {
                return MessageBeingBuilt.GetPossibleLengthLocalOnly(index);
            }

            public Builder SetPossibleLengthLocalOnly(int index, int value)
            {
                MessageBeingBuilt.possibleLengthLocalOnly_[index] = value;
                return this;
            }

            public Builder AddPossibleLengthLocalOnly(int value)
            {
                MessageBeingBuilt.possibleLengthLocalOnly_.Add(value);
                return this;
            }

            public Builder AddRangePossibleLengthLocalOnly(IEnumerable<int> values)
            {
                MessageBeingBuilt.possibleLengthLocalOnly_.AddRange(values);
                return this;
            }

            public Builder ClearPossibleLengthLocalOnly()
            {
                MessageBeingBuilt.possibleLengthLocalOnly_.Clear();
                return this;
            }

            public Builder SetExampleNumber(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.HasExampleNumber = true;
                MessageBeingBuilt.ExampleNumber = value;
                return this;
            }

            public Builder ClearExampleNumber()
            {
                MessageBeingBuilt.HasExampleNumber = false;
                MessageBeingBuilt.ExampleNumber = "";
                return this;
            }
        }


        #region Lite runtime methods

        public override int GetHashCode()
        {
            var hash = GetType().GetHashCode();
            if (HasNationalNumberPattern) hash ^= NationalNumberPattern.GetHashCode();

            hash = possibleLength_.Aggregate(hash, (current, i) => current ^ i.GetHashCode());
            hash = possibleLengthLocalOnly_.Aggregate(hash, (current, i) => current ^ i.GetHashCode());

            if (HasExampleNumber) hash ^= ExampleNumber.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as PhoneNumberDesc;
            if (HasNationalNumberPattern != other?.HasNationalNumberPattern || HasNationalNumberPattern &&
                !NationalNumberPattern.Equals(other.NationalNumberPattern)) return false;
            if (possibleLength_.Count != other.possibleLength_.Count) return false;
            for (var ix = 0; ix < possibleLength_.Count; ix++)
                if (!possibleLength_[ix].Equals(other.possibleLength_[ix])) return false;
            if (possibleLengthLocalOnly_.Count != other.possibleLengthLocalOnly_.Count) return false;
            for (var ix = 0; ix < possibleLengthLocalOnly_.Count; ix++)
                if (!possibleLengthLocalOnly_[ix].Equals(other.possibleLengthLocalOnly_[ix])) return false;
            if (HasExampleNumber != other.HasExampleNumber ||
                HasExampleNumber && !ExampleNumber.Equals(other.ExampleNumber)) return false;
            return true;
        }

        #endregion
    }
}

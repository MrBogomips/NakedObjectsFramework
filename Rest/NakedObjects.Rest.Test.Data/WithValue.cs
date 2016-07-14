﻿// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NakedObjects;
using NakedObjects.Security;

namespace RestfulObjects.Test.Data {
    [PresentationHint("class1 class2")]
    public class WithValue {
        private DateTime aDateTimeValue = new DateTime(2012, 2, 10);
        private TimeSpan aTimeSpanValue = new TimeSpan(1, 2, 3, 4, 5);

        [Key, Title, ConcurrencyCheck, DefaultValue(0)]
        public virtual int Id { get; set; }

        [PresentationHint("class3 class4")]
        public virtual int AValue { get; set; }

        [Disabled]
        public virtual int ADisabledValue { get; set; }

        [Hidden(WhenTo.Always)]
        public virtual int AHiddenValue { get; set; }

        public virtual int AChoicesValue { get; set; }

        [MaxLength(101)]
        [RegEx(Validation = @"[A-Z]")]
        [Optionally]
        [DescribedAs("A string value for testing")]
        [MemberOrder(Sequence = "3")]
        public virtual string AStringValue { get; set; }

        [Optionally]
        [DescribedAs("A datetime value for testing")]
        [Mask("d")]
        [MemberOrder(Sequence = "4")]
        public virtual DateTime ADateTimeValue {
            get { return aDateTimeValue; }
            set { aDateTimeValue = value; }
        }

        [Optionally]
        [DescribedAs("A timespan value for testing")]
        [Mask("d")]
        [NotMapped]
        [MemberOrder(Sequence = "5")]
        public virtual TimeSpan ATimeSpanValue
        {
            get { return aTimeSpanValue; }
            set { aTimeSpanValue = value; }
        }


        [AuthorizeProperty(ViewUsers = "viewUser")]
        public virtual int AUserHiddenValue { get; set; }

        [AuthorizeProperty(EditUsers = "editUser")]
        public virtual int AUserDisabledValue { get; set; }

        public virtual int AConditionalChoicesValue { get; set; }

        public virtual int[] ChoicesAChoicesValue() {
            return new[] {1, 2, 3};
        }

        public virtual string Validate(int aValue, int aChoicesValue) {
            if (aValue == 101 && aChoicesValue == 3) {
                return "Cross validation failed";
            }
            return "";
        }

        public virtual int[] ChoicesAConditionalChoicesValue(int aValue, string aStringValue) {
            return new[] {aValue, aStringValue == null ? 0 : int.Parse(aStringValue)};
        }
    }
}
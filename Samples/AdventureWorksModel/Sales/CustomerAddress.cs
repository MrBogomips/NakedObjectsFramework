// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.ComponentModel;
using NakedObjects;

namespace AdventureWorksModel {
    [IconName("house.png"), Description("Use Action menu to add")]
    public class CustomerAddress : AWDomainObject, IAddressRole {
        #region AddressType

        [MemberOrder(1)]
        public virtual AddressType AddressType { get; set; }

        #endregion

        #region Address

        [MemberOrder(2)]
        [Disabled]
        public virtual Address Address { get; set; }

        #endregion

        #region Customer

        [Hidden(WhenTo.Always)]
        public virtual Customer Customer { get; set; }

        #endregion

        public override string ToString() {
            var t = Container.NewTitleBuilder();
            t.Append(AddressType).Append(":", Address);
            return t.ToString();
        }

        #region ID

        [Hidden(WhenTo.Always)]
        public virtual int CustomerID { get; set; }

        [Hidden(WhenTo.Always)]
        public virtual int AddressID { get; set; }

        #endregion

        #region ModifiedDate and rowguid

        #region ModifiedDate

        [MemberOrder(99)]
        [Disabled]
        public override DateTime ModifiedDate { get; set; }

        #endregion

        #region rowguid

        [Hidden(WhenTo.Always)]
        public override Guid rowguid { get; set; }

        #endregion

        #endregion
    }
}
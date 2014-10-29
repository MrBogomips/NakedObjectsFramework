// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using NakedObjects.Architecture.Adapter;
using NakedObjects.Architecture.Facet;
using NakedObjects.Architecture.Spec;
using NakedObjects.Meta.Facet;

namespace NakedObjects.Reflect.I18n {
    public class DescribedAsFacetDynamicWrapI18n : FacetAbstract, IDescribedAsFacet {
        private readonly IDescribedAsFacet describedAsFacet;
        private readonly IIdentifier identifier;
        private readonly int index;
        private readonly II18nManager manager;


        public DescribedAsFacetDynamicWrapI18n(II18nManager manager, ISpecification holder, IIdentifier identifier, IDescribedAsFacet describedAsFacet, int index = -1)
            : base(Type, holder) {
            this.manager = manager;
            this.identifier = identifier;
            this.describedAsFacet = describedAsFacet;
            this.index = index;
        }

        public static Type Type {
            get { return typeof (IDescribedAsFacet); }
        }

        #region IDescribedAsFacet Members

        public string Value {
            get {
                if (index >= 0) {
                    return manager.GetParameterDescription(identifier, index, null) ?? describedAsFacet.Value;
                }
                return manager.GetDescription(identifier, null) ?? describedAsFacet.Value;
            }
        }

        #endregion
    }

    // Copyright (c) Naked Objects Group Ltd.
}
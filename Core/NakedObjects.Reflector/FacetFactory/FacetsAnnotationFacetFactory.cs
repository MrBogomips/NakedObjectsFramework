// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using NakedObjects.Architecture.Component;
using NakedObjects.Architecture.Facet;
using NakedObjects.Architecture.FacetFactory;
using NakedObjects.Architecture.Reflect;
using NakedObjects.Architecture.Spec;
using NakedObjects.Meta.Facet;
using NakedObjects.Meta.Utils;

using NakedObjects.Util;

namespace NakedObjects.Reflect.FacetFactory {
    public class FacetsAnnotationFacetFactory : AnnotationBasedFacetFactoryAbstract {
        public FacetsAnnotationFacetFactory(IReflector reflector)
            : base(reflector,FeatureType.Objects) {}

        public override bool Process(Type type, IMethodRemover methodRemover, ISpecificationBuilder specification) {
            var attribute = type.GetCustomAttributeByReflection<FacetsAttribute>();
            return FacetUtils.AddFacet(Create(attribute, specification));
        }

        /// <summary>
        ///     Returns a <see cref="IFacetsFacet" /> impl provided that at least one valid
        ///     factory <see cref="IFacetsFacet.FacetFactories" /> was specified.
        /// </summary>
        private static IFacetsFacet Create(FacetsAttribute attribute, ISpecification holder) {
            if (attribute == null) {
                return null;
            }
            var facetsFacetAnnotation = new FacetsFacetAnnotation(attribute, holder);
            return facetsFacetAnnotation.FacetFactories.Length > 0 ? facetsFacetAnnotation : null;
        }
    }
}
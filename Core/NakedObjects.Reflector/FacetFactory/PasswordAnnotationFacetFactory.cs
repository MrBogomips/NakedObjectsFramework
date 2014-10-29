// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using NakedObjects.Architecture.Component;
using NakedObjects.Architecture.Facet;
using NakedObjects.Architecture.FacetFactory;
using NakedObjects.Architecture.Reflect;
using NakedObjects.Architecture.Spec;
using NakedObjects.Meta.Facet;
using NakedObjects.Meta.Utils;

using NakedObjects.Util;

namespace NakedObjects.Reflect.FacetFactory {
    public class PasswordAnnotationFacetFactory : AnnotationBasedFacetFactoryAbstract {
        public PasswordAnnotationFacetFactory(IReflector reflector)
            : base(reflector, FeatureType.PropertiesAndParameters) {}


        private static bool Process(MemberInfo member, ISpecification holder) {
            var attribute = AttributeUtils.GetCustomAttribute<DataTypeAttribute>(member);
            return FacetUtils.AddFacet(Create(attribute, holder));
        }


        public override bool Process(PropertyInfo property, IMethodRemover methodRemover, ISpecificationBuilder specification) {
            return Process(property, specification);
        }

        public override bool ProcessParams(MethodInfo method, int paramNum, ISpecificationBuilder holder) {
            ParameterInfo parameter = method.GetParameters()[paramNum];
            var attribute = parameter.GetCustomAttributeByReflection<DataTypeAttribute>();
            return FacetUtils.AddFacet(Create(attribute, holder));
        }

        private static IPasswordFacet Create(DataTypeAttribute attribute, ISpecification holder) {
            return attribute != null && attribute.DataType == DataType.Password ? new PasswordFacet(holder) : null;
        }
    }
}
// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NakedObjects.Architecture.Component;
using NakedObjects.Architecture.Facet;
using NakedObjects.Architecture.FacetFactory;
using NakedObjects.Architecture.Reflect;
using NakedObjects.Architecture.Spec;
using NakedObjects.Meta.Facet;
using NakedObjects.Meta.Utils;
using NakedObjects.Reflect.Peer;
using NakedObjects.Util;

namespace NakedObjects.Reflect.FacetFactory {
    public abstract class MethodPrefixBasedFacetFactoryAbstract : FacetFactoryAbstract, IMethodPrefixBasedFacetFactory {
        protected MethodPrefixBasedFacetFactoryAbstract(IReflector reflector, FeatureType featureTypes)
            : base(reflector, featureTypes) {}

        #region IMethodPrefixBasedFacetFactory Members

        public abstract string[] Prefixes { get; }

        #endregion

        protected MethodInfo FindMethodWithOrWithoutParameters(Type type, MethodType methodType, string name, Type returnType, Type[] parms) {
            return FindMethod(type, methodType, name, returnType, parms) ??
                   FindMethod(type, methodType, name, returnType, Type.EmptyTypes);
        }

        /// <summary>
        ///     Returns  specific public methods that: have the specified prefix; have the specified return Type, or
        ///     void, and has the specified number of parameters. If the returnType is specified as null then the return
        ///     Type is ignored.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodType"></param>
        /// <param name="name"></param>
        /// <param name="returnType"></param>
        protected MethodInfo[] FindMethods(Type type,
                                           MethodType methodType,
                                           string name,
                                           Type returnType) {
            return type.GetMethods(GetBindingFlagsForMethodType(methodType)).
                Where(m => m.Name == name).
                Where(m => (m.IsStatic && methodType == MethodType.Class) || (!m.IsStatic && methodType == MethodType.Object)).
                Where(m => AttributeUtils.GetCustomAttribute<NakedObjectsIgnoreAttribute>(m) == null).
                Where(m => returnType == null || returnType.IsAssignableFrom(m.ReturnType)).ToArray();
        }


        /// <summary>
        ///     Returns  specific public methods that: have the specified prefix; have the specified return Type, or
        ///     void, and has the specified number of parameters. If the returnType is specified as null then the return
        ///     Type is ignored.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodType"></param>
        /// <param name="name"></param>
        /// <param name="returnType"></param>
        /// <param name="paramTypes">the set of parameters the method should have, if null then is ignored</param>
        /// <param name="paramNames">the names of the parameters the method should have, if null then is ignored</param>
        protected MethodInfo FindMethod(Type type,
                                        MethodType methodType,
                                        string name,
                                        Type returnType,
                                        Type[] paramTypes,
                                        string[] paramNames = null) {
            try {
                MethodInfo method = paramTypes == null
                    ? type.GetMethod(name, GetBindingFlagsForMethodType(methodType))
                    : type.GetMethod(name, GetBindingFlagsForMethodType(methodType), null, paramTypes, null);

                if (method == null) {
                    return null;
                }

                // check for static modifier
                if (method.IsStatic && methodType == MethodType.Object) {
                    return null;
                }

                if (!method.IsStatic && methodType == MethodType.Class) {
                    return null;
                }

                if (AttributeUtils.GetCustomAttribute<NakedObjectsIgnoreAttribute>(method) != null) {
                    return null;
                }

                // check for return Type
                if (returnType != null && !returnType.IsAssignableFrom(method.ReturnType)) {
                    return null;
                }

                if (paramNames != null) {
                    string[] methodParamNames = method.GetParameters().Select(p => p.Name).ToArray();

                    if (!paramNames.SequenceEqual(methodParamNames)) {
                        return null;
                    }
                }

                return method;
            }
            catch (AmbiguousMatchException e) {
                throw new ModelException(string.Format(Resources.NakedObjects.AmbiguousMethodError, name, type.FullName), e);
            }
        }

        private BindingFlags GetBindingFlagsForMethodType(MethodType methodType) {
            return BindingFlags.Public |
                   (methodType == MethodType.Object ? BindingFlags.Instance : BindingFlags.Static) |
                   (Reflector.IgnoreCase ? BindingFlags.IgnoreCase : BindingFlags.Default);
        }

        protected static void RemoveMethod(IMethodRemover methodRemover, MethodInfo method) {
            if (methodRemover != null && method != null) {
                methodRemover.RemoveMethod(method);
            }
        }

        protected static Type[] ParamTypesOrNull(Type type) {
            return type == null ? Type.EmptyTypes : new[] {type};
        }

        protected void FindAndRemoveDisableMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, ISpecification specification) {
            FindAndRemoveDisableMethod(facets, methodRemover, type, methodType, capitalizedName, (Type) null, specification);
        }

        protected void FindAndRemoveDisableMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, Type paramType, ISpecification specification) {
            FindAndRemoveDisableMethod(facets, methodRemover, type, methodType, capitalizedName, ParamTypesOrNull(paramType), specification);
        }

        protected void FindDefaultDisableMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, Type[] paramTypes, ISpecification specification) {
            MethodInfo method = FindMethodWithOrWithoutParameters(type, methodType, PrefixesAndRecognisedMethods.DisablePrefix + capitalizedName, typeof (string), paramTypes);
            if (method != null) {
                facets.Add(new DisableForContextFacet(method, specification));
            }
        }

        protected void FindAndRemoveDisableMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, Type[] paramTypes, ISpecification specification) {
            MethodInfo method = FindMethodWithOrWithoutParameters(type, methodType, PrefixesAndRecognisedMethods.DisablePrefix + capitalizedName, typeof (string), paramTypes);
            if (method != null) {
                methodRemover.RemoveMethod(method);
                facets.Add(new DisableForContextFacet(method, specification));
            }
        }

        protected void FindAndRemoveHideMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, ISpecification specification) {
            FindAndRemoveHideMethod(facets, methodRemover, type, methodType, capitalizedName, (Type) null, specification);
        }

        protected void FindAndRemoveHideMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, Type collectionType, ISpecification specification) {
            FindAndRemoveHideMethod(facets, methodRemover, type, methodType, capitalizedName, ParamTypesOrNull(collectionType), specification);
        }

        protected void FindDefaultHideMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, Type[] paramTypes, ISpecification specification) {
            MethodInfo method = FindMethodWithOrWithoutParameters(type, methodType, PrefixesAndRecognisedMethods.HidePrefix + capitalizedName, typeof (bool), paramTypes);
            if (method != null) {
                facets.Add(new HideForContextFacet(method, specification));
                AddOrAddToExecutedWhereFacet(method, specification);
            }
        }

        protected void FindAndRemoveHideMethod(IList<IFacet> facets, IMethodRemover methodRemover, Type type, MethodType methodType, string capitalizedName, Type[] paramTypes, ISpecification specification) {
            MethodInfo method = FindMethodWithOrWithoutParameters(type, methodType, PrefixesAndRecognisedMethods.HidePrefix + capitalizedName, typeof (bool), paramTypes);
            if (method != null) {
                methodRemover.RemoveMethod(method);
                facets.Add(new HideForContextFacet(method, specification));
                AddOrAddToExecutedWhereFacet(method, specification);
            }
        }

        protected static void AddHideForSessionFacetNone(IList<IFacet> facets, ISpecification specification) {
            facets.Add(new HideForSessionFacetNone(specification));
        }

        protected static void AddDisableForSessionFacetNone(IList<IFacet> facets, ISpecification specification) {
            facets.Add(new DisableForSessionFacetNone(specification));
        }

        protected static void AddDisableFacetAlways(IList<IFacet> facets, ISpecification specification) {
            facets.Add(new DisabledFacetAlways(specification));
        }

        protected static void AddOrAddToExecutedWhereFacet(MethodInfo method, ISpecification holder) {
            var attribute = AttributeUtils.GetCustomAttribute<ExecutedAttribute>(method);
            if (attribute != null && !attribute.IsAjax) {
                var executedFacet = holder.GetFacet<IExecutedControlMethodFacet>();
                if (executedFacet == null) {
                    FacetUtils.AddFacet(new ExecutedControlMethodFacet(method, attribute.Value, holder));
                }
                else {
                    executedFacet.AddMethodExecutedWhere(method, attribute.Value);
                }
            }
        }

        protected static void AddAjaxFacet(MethodInfo method, ISpecification holder) {
            if (method == null) {
                FacetUtils.AddFacet(new AjaxFacet(holder));
            }
            else {
                var attribute = AttributeUtils.GetCustomAttribute<ExecutedAttribute>(method);
                if (attribute != null && attribute.IsAjax) {
                    if (attribute.AjaxValue == Ajax.Disabled) {
                        FacetUtils.AddFacet(new AjaxFacet(holder));
                    }
                }
            }
        }
    }
}
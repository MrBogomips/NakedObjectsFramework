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
using System.Text;
using NakedObjects.Architecture.Adapter;
using NakedObjects.Architecture.Component;
using NakedObjects.Core.Util;

namespace NakedObjects.Meta.Adapter {
    [Serializable]
    public class IdentifierImpl : IIdentifier {
        private readonly string className;
        private readonly bool isField;
        private readonly string name;
        private readonly string[] parameterNames;
        private readonly string[] parameterTypes;
        private string asString;
        private string identityString;

        public IdentifierImpl(string className)
            : this(className, "", new string[0], new string[0], false) {}

        public IdentifierImpl(string className, string fieldName)
            : this(className, fieldName, new string[0], new string[0], true) {}

        public IdentifierImpl(string className, string methodName, ParameterInfo[] parameters)
            : this(className, methodName, parameters.Select(p => p.Name).ToArray(), ToParameterStringArray(parameters.Select(p => p.ParameterType).ToArray()), false) {}

        public IdentifierImpl(string className, string methodName, string[] parameterTypeNames)
            : this(className, methodName, parameterTypeNames.Select(p => "").ToArray(), parameterTypeNames, false) {}

        private IdentifierImpl(string className, string fieldName, string[] parameterNames, string[] parameterTypeNames, bool isField) {
            this.className = className;
            name = fieldName;
            parameterTypes = parameterTypeNames;
            this.parameterNames = parameterNames;
            this.isField = isField;
        }

        #region IIdentifier Members

        public virtual string ClassName => className;

        public virtual string MemberName => name;

        public virtual string[] MemberParameterTypeNames => parameterTypes;

        public virtual string[] MemberParameterNames => parameterNames;

        public virtual bool IsField => isField;

        public virtual string ToIdentityString(IdentifierDepth depth) {
            Assert.AssertTrue(depth >= IdentifierDepth.Class && depth <= IdentifierDepth.Parms);
            switch (depth) {
                case IdentifierDepth.Class:
                    return ToClassIdentityString();
                case IdentifierDepth.ClassName:
                    return ToClassAndNameIdentityString();
                case IdentifierDepth.ClassNameParams:
                    return ToFullIdentityString();
                case IdentifierDepth.Name:
                    return ToNameIdentityString();
                case IdentifierDepth.Parms:
                    return ToParmsIdentityString();
            }
            return null;
        }

        public virtual string ToIdentityStringWithCheckType(IdentifierDepth depth, CheckType checkType) {
            return ToIdentityString(depth) + ":" + checkType;
        }

        public int CompareTo(object o2) {
            return string.CompareOrdinal(ToString(), o2.ToString());
        }

        #endregion

        private static string FullName(Type type) {
            if (type.IsGenericType) {
                if (CollectionUtils.IsGenericEnumerable(type)) {
                    return TypeNameUtils.EncodeGenericTypeName(type);
                }
                if (type.Name.StartsWith("Nullable")) {
                    return type.GetGenericArguments()[0].FullName + "?";
                }
            }
            return type.FullName;
        }

        private static string[] ToParameterStringArray(Type[] fromArray) {
            var parameters = new List<string>();
            fromArray.ForEach(x => parameters.Add(FullName(x)));
            return parameters.ToArray();
        }

        private static bool Equals(string[] a, string[] b) {
            if (a == null && b == null) {
                return true;
            }

            if (a == null || b == null) {
                // since not both null must be different
                return false;
            }

            if (a.Length != b.Length) {
                return false;
            }

            return !b.Where((t, i) => !string.Equals(a[i], t)).Any();
        }

        private string ToClassIdentityString() {
            return className;
        }

        private string ToNameIdentityString() {
            return name;
        }

        private string ToClassAndNameIdentityString() {
            return ToClassIdentityString() + "#" + name;
        }

        private string ToParmsIdentityString() {
            var str = new StringBuilder();
            if (!IsField) {
                str.Append('(');
                for (int i = 0; i < parameterTypes.Length; i++) {
                    if (i > 0) {
                        str.Append(",");
                    }
                    str.Append(parameterTypes[i]);
                }
                str.Append(')');
            }
            return str.ToString();
        }

        private string ToFullIdentityString() {
            if (identityString == null) {
                if (name.Length == 0) {
                    identityString = ToClassIdentityString();
                }
                else {
                    var str = new StringBuilder();
                    str.Append(ToClassAndNameIdentityString());
                    str.Append(ToParmsIdentityString());
                    identityString = str.ToString();
                }
            }
            return identityString;
        }

        public static IdentifierImpl FromIdentityString(IMetamodel metamodel, string asString) {
            Assert.AssertNotNull(asString);
            int indexOfHash = asString.IndexOf("#", StringComparison.InvariantCulture);
            int indexOfOpenBracket = asString.IndexOf("(", StringComparison.InvariantCulture);
            int indexOfCloseBracket = asString.IndexOf(")", StringComparison.InvariantCulture);
            string className = asString.Substring(0, (indexOfHash == -1 ? asString.Length : indexOfHash) - 0);
            if (indexOfHash == -1 || indexOfHash == asString.Length - 1) {
                return new IdentifierImpl(className);
            }
            string name;
            if (indexOfOpenBracket == -1) {
                name = asString.Substring(indexOfHash + 1);
                return new IdentifierImpl(className, name);
            }
            name = asString.Substring(indexOfHash + 1, indexOfOpenBracket - (indexOfHash + 1));
            string allParms = asString.Substring(indexOfOpenBracket + 1, indexOfCloseBracket - (indexOfOpenBracket + 1)).Trim();
            string[] parms = allParms.Length > 0 ? allParms.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries) : new string[] {};
            return new IdentifierImpl(className, name, parms);
        }

        #region Object overrides 

        public override bool Equals(object obj) {
            if (this == obj) {
                return true;
            }
            var other = obj as IdentifierImpl;
            return other != null && string.Equals(other.className, className) && string.Equals(other.name, name) && Equals(other.parameterTypes, parameterTypes);
        }

        public override string ToString() {
            if (asString == null) {
                var str = new StringBuilder();
                str.Append(className).Append('#').Append(name).Append('(');
                for (int i = 0; i < parameterTypes.Length; i++) {
                    if (i > 0) {
                        str.Append(", ");
                    }
                    str.Append(parameterTypes[i]);
                }
                str.Append(')');
                asString = str.ToString();
            }
            return asString;
        }

        public override int GetHashCode() {
            return (className + name + parameterTypes.Aggregate("", (s, t) => s + t)).GetHashCode();
        }

        #endregion
    }

    // Copyright (c) Naked Objects Group Ltd.
}
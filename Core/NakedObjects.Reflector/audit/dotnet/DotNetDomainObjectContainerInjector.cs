// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using NakedObjects.Architecture.Component;
using NakedObjects.Core.Util;

namespace NakedObjects.Reflect.DotNet {
    public class DotNetDomainObjectContainerInjector : IContainerInjector {
        private object container;
        private bool initialized;
        private List<object> services;

        private List<object> Services {
            get {
                if (services == null) {
                    services = ServiceTypes.Select(Activator.CreateInstance).ToList();
                    services.Add(Framework);
                    services.ForEach(InitDomainObject);
                }
                return services;
            }
        }

        #region IContainerInjector Members

        public INakedObjectsFramework Framework { private get; set; }
        public Type[] ServiceTypes { set; private get; }

        public void InitDomainObject(object obj) {
            Initialize();
            Assert.AssertNotNull("no container", container);
            Assert.AssertNotNull("no services", Services);
            Methods.InjectContainer(obj, container);
            Methods.InjectServices(obj, Services.ToArray());
        }

        public void InitInlineObject(object root, object inlineObject) {
            Initialize();
            Assert.AssertNotNull("no root object", root);
            Methods.InjectRoot(root, inlineObject);
        }

        #endregion

        private void Initialize() {
            if (!initialized) {
                Assert.AssertNotNull(Framework);
                container = new DotNetDomainObjectContainer(Framework);
                initialized = true;
            }
        }
    }

    // Copyright (c) Naked Objects Group Ltd.
}
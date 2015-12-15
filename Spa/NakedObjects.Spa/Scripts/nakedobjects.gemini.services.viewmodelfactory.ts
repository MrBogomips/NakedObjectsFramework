/// <reference path="typings/angularjs/angular.d.ts" />
/// <reference path="typings/lodash/lodash.d.ts" />
/// <reference path="nakedobjects.models.ts" />


module NakedObjects.Angular.Gemini {

    export interface IViewModelFactory {
        toolBarViewModel($scope): ToolBarViewModel;

        errorViewModel(errorRep: ErrorRepresentation): ErrorViewModel;
        linkViewModel(linkRep: Link, paneId: number): LinkViewModel;
        itemViewModel(linkRep: Link, paneId: number, selected : boolean): ItemViewModel;
        actionViewModel(actionRep: ActionMember, paneId: number, ovm?: DomainObjectViewModel): ActionViewModel;
        dialogViewModel($scope: ng.IScope, actionViewModel: ActionViewModel, parms: _.Dictionary<Value>, paneId: number, ovm?: DomainObjectViewModel): DialogViewModel;

        collectionViewModel($scope: ng.IScope, collection: CollectionMember, state: CollectionViewState, paneId: number, selectedItems : boolean[],   recreate: (page: number, newPageSize: number, newState?: CollectionViewState) => void): CollectionViewModel;
        collectionViewModel($scope: ng.IScope, collection: ListRepresentation, state: CollectionViewState, paneId: number, selectedItems: boolean[],  recreate: (page: number, newPageSize: number, newState?: CollectionViewState) => void): CollectionViewModel;

        collectionPlaceholderViewModel(page: number, reload: () => void): CollectionPlaceholderViewModel;

        parameterViewModel(parmRep: Parameter, previousValue: Value, paneId: number): ParameterViewModel;
        propertyViewModel(propertyRep: PropertyMember, id: string, previousValue: Value, paneId: number): PropertyViewModel;

        servicesViewModel(servicesRep: DomainServicesRepresentation): ServicesViewModel;
        menusViewModel(menusRep: MenusRepresentation, paneId: number): MenusViewModel;
        serviceViewModel(serviceRep: DomainObjectRepresentation, paneId: number): ServiceViewModel;
        domainObjectViewModel($scope: ng.IScope, objectRep: DomainObjectRepresentation, collectionStates: _.Dictionary<CollectionViewState>, parms: _.Dictionary<Value>, editing: boolean, paneId: number): DomainObjectViewModel;
        ciceroViewModel(wrapped: any): CiceroViewModel;
        getCiceroVM(): CiceroViewModel;
        setCiceroVMIfNecessary(): CiceroViewModel;
    }

    app.service('viewModelFactory', function ($q: ng.IQService,
        $timeout: ng.ITimeoutService,
        $location: ng.ILocationService,
        $filter: ng.IFilterService,
        $cacheFactory: ng.ICacheFactoryService,
        repLoader: IRepLoader,
        color: IColor,
        context: IContext,
        mask: IMask,
        urlManager: IUrlManager,
        focusManager: IFocusManager,
        navigation: INavigation,
        clickHandler: IClickHandler,
        commandFactory: ICommandFactory) {

        var viewModelFactory = <IViewModelFactory>this;

        viewModelFactory.errorViewModel = (errorRep: ErrorRepresentation) => {
            const errorViewModel = new ErrorViewModel();
            errorViewModel.message = errorRep.message() || "An Error occurred";
            const stackTrace = errorRep.stackTrace();
            errorViewModel.stackTrace = !stackTrace || stackTrace.length === 0 ? ["Empty"] : stackTrace;
            return errorViewModel;
        };

        function initLinkViewModel(linkViewModel: LinkViewModel, linkRep: Link) {
            linkViewModel.title = linkRep.title();
            linkViewModel.color = color.toColorFromHref(linkRep.href());
            linkViewModel.link = linkRep;

            linkViewModel.domainType = linkRep.type().domainType;
            linkViewModel.draggableType = linkViewModel.domainType;

            // for dropping 
            const value = new Value(linkRep);

            linkViewModel.value = value.toString();
            linkViewModel.reference = value.toValueString();
            linkViewModel.choice = ChoiceViewModel.create(value, "");

            linkViewModel.canDropOn = (targetType: string) => context.isSubTypeOf(targetType, linkViewModel.domainType);
        }


        viewModelFactory.linkViewModel = (linkRep: Link, paneId: number) => {
            const linkViewModel = new LinkViewModel();
            linkViewModel.doClick = () => {
                // because may be clicking on menu already open so want to reset focus             
                urlManager.setMenu(linkRep.rel().parms[0].value, paneId);
                focusManager.focusOn(FocusTarget.FirstSubAction, paneId);
            };
            initLinkViewModel(linkViewModel, linkRep);
            return linkViewModel;
        };

        viewModelFactory.itemViewModel = (linkRep: Link, paneId: number, selected : boolean) => {
            const itemViewModel = new ItemViewModel();
            itemViewModel.doClick = (right?: boolean) => urlManager.setItem(linkRep, clickHandler.pane(paneId, right));
            initLinkViewModel(itemViewModel, linkRep);

            itemViewModel.selected = selected;

            itemViewModel.checkboxChange = (index) =>
                urlManager.setListItem(paneId, index, itemViewModel.selected);


            return itemViewModel;
        };

        function addAutoAutoComplete(valueViewModel: ValueViewModel, currentChoice: ChoiceViewModel, id: string, currentValue: Value) {
            valueViewModel.hasAutoAutoComplete = true;

            const cache = $cacheFactory.get("recentlyViewed");

            valueViewModel.choice = currentChoice;

            // make sure current value is cached so can be recovered ! 

            const { returnType: key, reference: subKey } = valueViewModel;
            const dict = <any>cache.get(key) || {}; // todo fix type !
            dict[subKey] = { value: currentValue, name: currentValue.toString() };
            cache.put(key, dict);

            // bind in autoautocomplete into prompt 

            valueViewModel.prompt = (st: string) => {
                const defer = $q.defer<ChoiceViewModel[]>();
                const filtered = _.filter(dict, (i: { value: Value, name: string }) =>
                    i.name.toString().toLowerCase().indexOf(st.toLowerCase()) > -1);
                const ccs = _.map(filtered, (i: { value: Value, name: string }) => ChoiceViewModel.create(i.value, id, i.name));

                defer.resolve(ccs);

                return defer.promise;
            };
        }

        // tested
        viewModelFactory.parameterViewModel = (parmRep: Parameter, previousValue: Value, paneId: number) => {
            var parmViewModel = new ParameterViewModel();

            parmViewModel.type = parmRep.isScalar() ? "scalar" : "ref";
            parmViewModel.dflt = parmRep.default().toValueString();
            parmViewModel.optional = parmRep.extensions().optional();
            var required = "";
            if (!parmViewModel.optional) {
                required = "* ";
            }
            parmViewModel.description = required + parmRep.extensions().description();
            parmViewModel.message = "";
            parmViewModel.id = parmRep.parameterId();
            parmViewModel.argId = parmViewModel.id.toLowerCase();
            parmViewModel.reference = "";

            parmViewModel.mask = parmRep.extensions().mask();
            parmViewModel.title = parmRep.extensions().friendlyName();
            parmViewModel.returnType = parmRep.extensions().returnType();
            parmViewModel.format = parmRep.extensions().format();

            parmViewModel.drop = (newValue: IDraggableViewModel) => {
                context.isSubTypeOf(newValue.draggableType, parmViewModel.returnType).
                    then((canDrop: boolean) => {
                        if (canDrop) {
                            parmViewModel.setNewValue(newValue);
                        }
                    }
                    );
            };

            parmViewModel.choices = _.map(parmRep.choices(), (v, n) => ChoiceViewModel.create(v, parmRep.parameterId(), n));
            parmViewModel.hasChoices = parmViewModel.choices.length > 0;
            parmViewModel.hasPrompt = !!parmRep.promptLink() && !!parmRep.promptLink().arguments()["x-ro-searchTerm"];
            parmViewModel.hasConditionalChoices = !!parmRep.promptLink() && !parmViewModel.hasPrompt;
            parmViewModel.isMultipleChoices = (parmViewModel.hasChoices || parmViewModel.hasConditionalChoices) && parmRep.extensions().returnType() === "list";

            if (parmViewModel.hasPrompt || parmViewModel.hasConditionalChoices) {

                const promptRep = parmRep.getPrompts();
                if (parmViewModel.hasPrompt) {
                    parmViewModel.prompt = <(st: string) => ng.IPromise<ChoiceViewModel[]>>_.partial(context.prompt, promptRep, parmViewModel.id);
                    parmViewModel.minLength = parmRep.promptLink().extensions().minLength();
                }

                if (parmViewModel.hasConditionalChoices) {
                    parmViewModel.conditionalChoices = <(args: _.Dictionary<Value>) => ng.IPromise<ChoiceViewModel[]>>_.partial(context.conditionalChoices, promptRep, parmViewModel.id);
                    parmViewModel.arguments = _.object<_.Dictionary<Value>>(_.map(parmRep.promptLink().arguments(), (v: any, key) => [key, new Value(v.value)]));
                }
            }

            if (parmViewModel.hasChoices || parmViewModel.hasPrompt || parmViewModel.hasConditionalChoices) {

                function setCurrentChoices(vals: Value) {

                    const choicesToSet = _.map(vals.list(), val => ChoiceViewModel.create(val, parmViewModel.id, val.link() ? val.link().title() : null));

                    if (parmViewModel.hasPrompt || parmViewModel.hasConditionalChoices) {
                        parmViewModel.multiChoices = choicesToSet;
                    } else {
                        parmViewModel.multiChoices = _.filter(parmViewModel.choices, c => _.any(choicesToSet, choiceToSet => c.match(choiceToSet)));
                    }
                }

                function setCurrentChoice(val: Value) {
                    const choiceToSet = ChoiceViewModel.create(val, parmViewModel.id, val.link() ? val.link().title() : null);

                    if (parmViewModel.hasPrompt || parmViewModel.hasConditionalChoices) {
                        parmViewModel.choice = choiceToSet;
                    } else {
                        parmViewModel.choice = _.find(parmViewModel.choices, c => c.match(choiceToSet));
                    }
                }

                if (previousValue || parmViewModel.dflt) {
                    const toSet = previousValue || parmRep.default();
                    if (parmViewModel.isMultipleChoices) {
                        setCurrentChoices(toSet);
                    } else {
                        setCurrentChoice(toSet);
                    }
                }
            } else {
                if (parmRep.extensions().returnType() === "boolean") {
                    parmViewModel.value = previousValue ? previousValue.toString().toLowerCase() === "true" : parmRep.default().scalar();
                } else {
                    parmViewModel.value = (previousValue ? previousValue.toString() : null) || parmViewModel.dflt || "";
                }
            }

            var remoteMask = parmRep.extensions().mask();

            if (remoteMask && parmRep.isScalar()) {
                const localFilter = mask.toLocalFilter(remoteMask);
                if (localFilter) {
                    parmViewModel.value = $filter(localFilter.name)(parmViewModel.value, localFilter.mask);
                }
            }

            if (parmViewModel.type === "ref" && !parmViewModel.hasPrompt && !parmViewModel.hasChoices && !parmViewModel.hasConditionalChoices) {

                let currentChoice: ChoiceViewModel = null;

                if (previousValue) {
                    currentChoice = ChoiceViewModel.create(previousValue, parmViewModel.id, previousValue.link() ? previousValue.link().title() : null);
                }
                else if (parmViewModel.dflt) {
                    let dflt = parmRep.default();
                    currentChoice = ChoiceViewModel.create(dflt, parmViewModel.id, dflt.link().title());
                }

                const currentValue = new Value(currentChoice ? { href: currentChoice.value, title: currentChoice.name } : "");

                addAutoAutoComplete(parmViewModel, currentChoice, parmViewModel.id, currentValue);
            }

            parmViewModel.color = parmViewModel.value ? color.toColorFromType(parmViewModel.returnType) : "";

            return parmViewModel;
        };

        viewModelFactory.actionViewModel = (actionRep: ActionMember, paneId: number, ovm?: DomainObjectViewModel) => {
            var actionViewModel = new ActionViewModel();

            actionViewModel.actionRep = actionRep;
            actionViewModel.title = actionRep.extensions().friendlyName();
            actionViewModel.menuPath = actionRep.extensions().menuPath() || "";
            actionViewModel.disabled = () => { return !!actionRep.disabledReason(); }
            if (actionViewModel.disabled()) {
                actionViewModel.description = actionRep.disabledReason();
            } else {
                actionViewModel.description = actionRep.extensions().description();
            }

            actionViewModel.executeInvoke = (dvm: DialogViewModel, right?: boolean) =>
                context.invokeAction(actionRep, clickHandler.pane(paneId, right), dvm);

            // open dialog on current pane always - invoke action goes to pane indicated by click
            actionViewModel.doInvoke = actionRep.extensions().hasParams() ?
            (right?: boolean) => urlManager.setDialog(actionRep.actionId(), paneId) :
            (right?: boolean) => actionViewModel.executeInvoke(null, right);

            return actionViewModel;
        };

        const currentDvms: DialogViewModel[] = [];

        function getDialogViewModel(paneId: number, actionMember: ActionMember) {
            const currentDvm = currentDvms[paneId];
            if (currentDvm && currentDvm.isSame(paneId, actionMember)) {
                return { dialogViewModel: currentDvm, ret: true };
            }
            const dvm = new DialogViewModel();
            currentDvms[paneId] = dvm;
            return { dialogViewModel: dvm, ret: false };
        }


        const currentOvms: DomainObjectViewModel[] = [];

        function getObjectViewModel(paneId: number, obj: DomainObjectRepresentation, editing : boolean) {
            const currentOvm = currentOvms[paneId];
            if (editing && currentOvm && currentOvm.isSameEditView(paneId, obj, editing)) {
                return { objectViewModel: currentOvm, ret: true };
            }
            const ovm = new DomainObjectViewModel();
            currentOvms[paneId] = ovm;
            return { objectViewModel: ovm, ret: false };
        }

        function clearDialog(paneId: number, actionMember: ActionMember) {
            const currentDvm = currentDvms[paneId];
            if (currentDvm && currentDvm.isSame(paneId, actionMember)) {
                currentDvms[paneId] = null;
            }
        }

        viewModelFactory.dialogViewModel = ($scope: ng.IScope, actionViewModel: ActionViewModel, parms: _.Dictionary<Value>, paneId: number, ovm?: DomainObjectViewModel) => {

            const actionMember = actionViewModel.actionRep;
            const {dialogViewModel, ret} = getDialogViewModel(paneId, actionMember);

            if (ret) {
                return dialogViewModel;
            }

            const parameters = _.pick(actionMember.parameters(), p => !p.isCollectionContributed()) as _.Dictionary<Parameter>;

            dialogViewModel.action = actionMember;
            dialogViewModel.title = actionMember.extensions().friendlyName();
            dialogViewModel.isQueryOnly = actionMember.invokeLink().method() === "GET";
            dialogViewModel.message = "";
            dialogViewModel.parameters = _.map(parameters, parm => viewModelFactory.parameterViewModel(parm, parms[parm.parameterId()], paneId));
            dialogViewModel.onPaneId = paneId;

            dialogViewModel.doInvoke = (right?: boolean) => actionViewModel.executeInvoke(dialogViewModel, right);

            const setParms = () => _.forEach(dialogViewModel.parameters, p => urlManager.setParameterValue(actionMember.actionId(), p, paneId, false));

            const deregisterLocationWatch = $scope.$on("$locationChangeStart", setParms);
            const deregisterSearchWatch = $scope.$watch(() => $location.search(), setParms, true);

            dialogViewModel.doClose = () => {
                deregisterLocationWatch();
                deregisterSearchWatch();
                urlManager.closeDialog(paneId);
                clearDialog(paneId, actionMember);
            };

            return dialogViewModel;
        };


        viewModelFactory.propertyViewModel = (propertyRep: PropertyMember, id: string, previousValue: Value, paneId: number) => {
            const propertyViewModel = new PropertyViewModel();


            propertyViewModel.title = propertyRep.extensions().friendlyName();
            propertyViewModel.optional = propertyRep.extensions().optional();

            const required = propertyViewModel.optional ? "" : "* ";

            propertyViewModel.description = required + propertyRep.extensions().description();

            const value = previousValue || propertyRep.value();

            propertyViewModel.value = propertyRep.isScalar() ? value.scalar() : value.isNull() ? propertyViewModel.description : value.toString();
            propertyViewModel.type = propertyRep.isScalar() ? "scalar" : "ref";
            propertyViewModel.returnType = propertyRep.extensions().returnType();
            propertyViewModel.format = propertyRep.extensions().format();
            propertyViewModel.reference = propertyRep.isScalar() || value.isNull() ? "" : value.link().href();
            propertyViewModel.draggableType = propertyViewModel.returnType;

            propertyViewModel.canDropOn = (targetType: string) => context.isSubTypeOf(propertyViewModel.returnType, targetType);

            propertyViewModel.drop = (newValue: IDraggableViewModel) => {
                context.isSubTypeOf(newValue.draggableType, propertyViewModel.returnType).
                    then((canDrop: boolean) => {
                        if (canDrop) {
                            propertyViewModel.setNewValue(newValue);
                        }
                    });
            };

            propertyViewModel.doClick = (right?: boolean) => urlManager.setProperty(propertyRep, clickHandler.pane(paneId, right));
            if (propertyRep.attachmentLink() != null) {
                propertyViewModel.attachment = AttachmentViewModel.create(propertyRep.attachmentLink().href(),
                    propertyRep.attachmentLink().type().asString,
                    propertyRep.attachmentLink().title());
            }

            // only set color if has value 
            propertyViewModel.color = propertyViewModel.value ? color.toColorFromType(propertyRep.extensions().returnType()) : "";

            propertyViewModel.id = id;
            propertyViewModel.argId = id.toLowerCase();
            propertyViewModel.isEditable = !propertyRep.disabledReason();
            propertyViewModel.choices = [];
            propertyViewModel.hasPrompt = propertyRep.hasPrompt();

            if (propertyRep.hasChoices()) {

                const choices = propertyRep.choices();

                if (choices) {
                    propertyViewModel.choices = _.map(choices, (v, n) => ChoiceViewModel.create(v, id, n));
                }
            }

            propertyViewModel.hasChoices = propertyViewModel.choices.length > 0;
            propertyViewModel.hasPrompt = !!propertyRep.promptLink() && !!propertyRep.promptLink().arguments()["x-ro-searchTerm"];
            propertyViewModel.hasConditionalChoices = !!propertyRep.promptLink() && !propertyViewModel.hasPrompt;

            if (propertyViewModel.hasPrompt || propertyViewModel.hasConditionalChoices) {
                const promptRep: PromptRepresentation = propertyRep.getPrompts();

                if (propertyViewModel.hasPrompt) {
                    propertyViewModel.prompt = <(st: string) => ng.IPromise<ChoiceViewModel[]>>_.partial(context.prompt, promptRep, id);
                    propertyViewModel.minLength = propertyRep.promptLink().extensions().minLength();
                }

                if (propertyViewModel.hasConditionalChoices) {
                    propertyViewModel.conditionalChoices = <(args: _.Dictionary<Value>) => ng.IPromise<ChoiceViewModel[]>>_.partial(context.conditionalChoices, promptRep, id);
                    propertyViewModel.arguments = _.object<_.Dictionary<Value>>(_.map(propertyRep.promptLink().arguments(), (v: any, key) => [key, new Value(v.value)]));
                }
            }

            if (propertyViewModel.hasChoices || propertyViewModel.hasPrompt || propertyViewModel.hasConditionalChoices) {

                var currentChoice: ChoiceViewModel = ChoiceViewModel.create(value, id);

                if (propertyViewModel.hasPrompt || propertyViewModel.hasConditionalChoices) {
                    propertyViewModel.choice = currentChoice;
                } else {
                    propertyViewModel.choice = _.find(propertyViewModel.choices, (c: ChoiceViewModel) => c.match(currentChoice));
                }
            }

            if (propertyRep.isScalar()) {
                const remoteMask = propertyRep.extensions().mask();
                const localFilter = mask.toLocalFilter(remoteMask) || mask.defaultLocalFilter(propertyRep.extensions().format());
                if (localFilter) {
                    propertyViewModel.value = $filter(localFilter.name)(propertyViewModel.value, localFilter.mask);
                }
            }

            // if a reference and no way to set (ie not choices or autocomplete) use autoautocomplete
            if (propertyViewModel.type === "ref" && !propertyViewModel.hasPrompt && !propertyViewModel.hasChoices && !propertyViewModel.hasConditionalChoices) {
                addAutoAutoComplete(propertyViewModel, ChoiceViewModel.create(value, id), id, value);
            }

            return propertyViewModel;
        };

        function getItems($scope: ng.IScope, collectionViewModel: CollectionViewModel, links: Link[], populateItems: boolean, selectedItems: boolean[]) {

            if (populateItems) {
                return _.map(links, (link, i) => {
                    const itemViewModel = viewModelFactory.itemViewModel(link, collectionViewModel.onPaneId, selectedItems[i]);
                    const tempTgt = link.getTarget();
                    repLoader.populate<DomainObjectRepresentation>(tempTgt).
                        then((obj: DomainObjectRepresentation) => {
                            itemViewModel.target = viewModelFactory.domainObjectViewModel($scope, obj, {}, {}, false, 1);

                            if (!collectionViewModel.header) {
                                collectionViewModel.header = _.map(itemViewModel.target.properties, property => property.title);
                                focusManager.focusOn(FocusTarget.FirstTableItem, urlManager.currentpane());
                            }
                        });
                    return itemViewModel;
                });
            } else {
                return _.map(links, (link, i) => viewModelFactory.itemViewModel(link, collectionViewModel.onPaneId, selectedItems[i]));
            }
        }

        function create($scope: ng.IScope, collectionRep: CollectionMember, state: CollectionViewState, paneId: number) {
            const collectionViewModel = new CollectionViewModel();
            const links = collectionRep.value();

            collectionViewModel.onPaneId = paneId;

            collectionViewModel.title = collectionRep.extensions().friendlyName();
            collectionViewModel.size = links.length;
            collectionViewModel.pluralName = collectionRep.extensions().pluralName();
            collectionViewModel.color = color.toColorFromType(collectionRep.extensions().elementType());

            collectionViewModel.items = getItems($scope, collectionViewModel, links, state === CollectionViewState.Table, []);

            switch (state) {
                case CollectionViewState.List:
                    collectionViewModel.template = collectionListTemplate;
                    break;
                case CollectionViewState.Table:
                    collectionViewModel.template = collectionTableTemplate;
                    break;
                default:
                    collectionViewModel.template = collectionSummaryTemplate;
            }

            const setState = <(state: CollectionViewState) => void>_.partial(urlManager.setCollectionMemberState, paneId, collectionRep);
            collectionViewModel.doSummary = () => setState(CollectionViewState.Summary);
            collectionViewModel.doList = () => setState(CollectionViewState.List);
            collectionViewModel.doTable = () => setState(CollectionViewState.Table);

            return collectionViewModel;
        }

        function createFromList($scope: ng.IScope, listRep: ListRepresentation, state: CollectionViewState, paneId: number, selectedItems: boolean[], recreate: (page: number, newPageSize: number, newState: CollectionViewState) => void) {
            const collectionViewModel = new CollectionViewModel();
            const links = listRep.value();

            collectionViewModel.onPaneId = paneId;

            collectionViewModel.pluralName = "Objects";
            collectionViewModel.items = getItems($scope, collectionViewModel, links, state === CollectionViewState.Table, selectedItems);

            const page = listRep.pagination().page;
            const pageSize = listRep.pagination().pageSize;
            const numPages = listRep.pagination().numPages;
            const totalCount = listRep.pagination().totalCount;
            const count = links.length;

            collectionViewModel.size = count;

            collectionViewModel.description = () => `Page ${page} of ${numPages}; viewing ${count} of ${totalCount} items`;


            const actions = listRep.actionMembers();
            collectionViewModel.actions = _.map(actions, action => viewModelFactory.actionViewModel(action, paneId));

            // todo do more elegantly 

            _.forEach(collectionViewModel.actions, a => {

                a.executeInvoke = (dvm : DialogViewModel, right?: boolean) => {
                    const selected = _.filter(collectionViewModel.items, i => i.selected);

                    if (selected.length === 0) {
                        collectionViewModel.messages = "Must select items for collection contributed action";
                        return;
                    }

                    const parmValue = new Value(_.map(selected, i => i.link));
                    const parmKey = _.first(_.keys(a.actionRep.parameters()));

                    const parm: _.Dictionary<Value> = _.zipObject([[parmKey, parmValue]]);

                    context.invokeActionWithParms(a.actionRep, clickHandler.pane(paneId, right), parm);
                }

                a.doInvoke = _.keys(a.actionRep.parameters()).length > 1 ?
                    (right?: boolean) => urlManager.setDialog(a.actionRep.actionId(), paneId) :
                    (right?: boolean) => a.executeInvoke(null, right);
            });


            collectionViewModel.toggleActionMenu = () => urlManager.toggleObjectMenu(paneId);

            const setPage = (newPage: number, newState: CollectionViewState) => {
                // todo do we need timeout ?
                $timeout(() => recreate(newPage, pageSize, newState));
            }

            collectionViewModel.pageNext = () => setPage(page < numPages ? page + 1 : page, state);
            collectionViewModel.pagePrevious = () => setPage(page > 1 ? page - 1 : page, state);
            collectionViewModel.pageFirst = () => setPage(1, state);
            collectionViewModel.pageLast = () => setPage(numPages, state);

            const earlierDisabled = () => page === 1 || numPages === 1;
            const laterDisabled = () => page === numPages || numPages === 1;

            collectionViewModel.pageFirstDisabled = earlierDisabled;
            collectionViewModel.pageLastDisabled = laterDisabled;
            collectionViewModel.pageNextDisabled = laterDisabled;
            collectionViewModel.pagePreviousDisabled = earlierDisabled;

            const setState = <(state: CollectionViewState) => void>_.partial(urlManager.setListState, paneId);

            collectionViewModel.doSummary = () => setState(CollectionViewState.Summary);
            collectionViewModel.doList = () => setPage(page, CollectionViewState.List);
            collectionViewModel.doTable = () => setPage(page, CollectionViewState.Table);

            collectionViewModel.reload = () => setPage(page, state);

            return collectionViewModel;
        }


        viewModelFactory.collectionViewModel = ($scope: ng.IScope, collection: CollectionMember | ListRepresentation, state: CollectionViewState, paneId: number, selectedItems: boolean[],  recreate: (page: number) => void) => {
            let collectionVm: CollectionViewModel = null;

            if (collection instanceof CollectionMember) {
                collectionVm = create($scope, collection, state, paneId);
            }

            if (collection instanceof ListRepresentation) {
                collectionVm = createFromList($scope, collection, state, paneId, selectedItems,  recreate);
            }

            return collectionVm;
        };

        viewModelFactory.collectionPlaceholderViewModel = (page: number, reload: () => void) => {
            const collectionPlaceholderViewModel = new CollectionPlaceholderViewModel();

            collectionPlaceholderViewModel.description = () => `Page ${page}`;
            collectionPlaceholderViewModel.reload = reload;
            return collectionPlaceholderViewModel;
        }

        viewModelFactory.servicesViewModel = (servicesRep: DomainServicesRepresentation) => {
            const servicesViewModel = new ServicesViewModel();

            // filter out contributed action services 
            const links = _.filter(servicesRep.value(), m => {
                const sid = m.rel().parms[0].value;
                return sid.indexOf("ContributedActions") === -1;
            });

            servicesViewModel.title = "Services";
            servicesViewModel.color = "bg-color-darkBlue";
            servicesViewModel.items = _.map(links, link => viewModelFactory.linkViewModel(link, 1));
            return servicesViewModel;
        };

        viewModelFactory.menusViewModel = (menusRep: MenusRepresentation, paneId: number) => {
            const menusViewModel = new MenusViewModel();

            menusViewModel.title = "Menus";
            menusViewModel.color = "bg-color-darkBlue";
            menusViewModel.items = _.map(menusRep.value(), link => viewModelFactory.linkViewModel(link, paneId));
            return menusViewModel;
        };


        viewModelFactory.serviceViewModel = (serviceRep: DomainObjectRepresentation, paneId: number) => {
            const serviceViewModel = new ServiceViewModel();
            const actions = serviceRep.actionMembers();
            serviceViewModel.serviceId = serviceRep.serviceId();
            serviceViewModel.title = serviceRep.title();
            serviceViewModel.actions = _.map(actions, action => viewModelFactory.actionViewModel(action, paneId));
            serviceViewModel.color = color.toColorFromType(serviceRep.serviceId());

            return serviceViewModel;
        };
  
        // seperate function so we can reuse in reload
        function setupDomainObjectViewModel(objectViewModel: DomainObjectViewModel, $scope: INakedObjectsScope, objectRep: DomainObjectRepresentation, collectionStates: _.Dictionary<CollectionViewState>, props: _.Dictionary<Value>, editing: boolean, paneId: number) {

            objectViewModel.domainObject = objectRep;
            objectViewModel.onPaneId = paneId;
            objectViewModel.isTransient = !!objectRep.persistLink();
            objectViewModel.color = color.toColorFromType(objectRep.domainType());
            objectViewModel.domainType = objectRep.domainType();
            objectViewModel.instanceId = objectRep.instanceId();
            objectViewModel.draggableType = objectViewModel.domainType;

            objectViewModel.canDropOn = (targetType: string) => context.isSubTypeOf(targetType, objectViewModel.domainType);

            const properties = objectRep.propertyMembers();
            const collections = objectRep.collectionMembers();
            const actions = objectRep.actionMembers();

            objectViewModel.title = objectViewModel.isTransient ? `Unsaved ${objectRep.extensions().friendlyName() }` : objectRep.title();

            objectViewModel.message = "";

            objectViewModel.actions = _.map(actions, action => viewModelFactory.actionViewModel(action, paneId, objectViewModel));
            objectViewModel.properties = _.map(properties, (property, id) => viewModelFactory.propertyViewModel(property, id, props[id], paneId));
            objectViewModel.collections = _.map(collections, collection => viewModelFactory.collectionViewModel($scope, collection, collectionStates[collection.collectionId()], paneId, [], (page: number) => { }));

            // for dropping
            objectViewModel.toggleActionMenu = () => urlManager.toggleObjectMenu(paneId);

            const link = objectRep.selfLink();
            if (link) {
                // not transient - can't drag transients so no need to set up IDraggable members on transients
                //link.set("title", objectViewModel.title);
                link.wrapped.title = objectViewModel.title;


                const value = new Value(link);

                objectViewModel.value = value.toString();
                objectViewModel.reference = value.toValueString();
                objectViewModel.choice = ChoiceViewModel.create(value, "");
            }

            if (editing || objectViewModel.isTransient) {

                const editProperties = _.filter(objectViewModel.properties, p => p.isEditable);
                const setProperties = () => _.forEach(editProperties, p => urlManager.setPropertyValue(objectRep, p, paneId, false));
                const deregisterLocationWatch = $scope.$on("$locationChangeStart", setProperties);
                const deregisterSearchWatch = $scope.$watch(() => $location.search(), setProperties, true);

                const cancelHandler = objectViewModel.isTransient ?
                    () => urlManager.popUrlState(paneId) :
                    () => urlManager.setObjectEdit(false, paneId);


                objectViewModel.editComplete = () => {
                    deregisterLocationWatch();
                    deregisterSearchWatch();
                };

                objectViewModel.doEditCancel = () => {
                    objectViewModel.editComplete();
                    cancelHandler();
                };

                const saveHandler = objectViewModel.isTransient ? context.saveObject : context.updateObject;
                objectViewModel.doSave = viewObject => saveHandler(objectRep, objectViewModel, viewObject);
                objectViewModel.isInEdit = true;
            }

            objectViewModel.doEdit = () => {
                context.reloadObject(paneId, objectRep).
                    then((updatedObject: DomainObjectRepresentation) => {
                        setupDomainObjectViewModel(objectViewModel, $scope, updatedObject, collectionStates, props, editing, paneId);
                        $scope.object = objectViewModel;
                        urlManager.pushUrlState(paneId);
                        urlManager.setObjectEdit(true, paneId);
                    });
            }

            objectViewModel.doReload = (refreshScope?: boolean) =>
                context.reloadObject(paneId, objectRep).
                    then((updatedObject: DomainObjectRepresentation) => {
                        setupDomainObjectViewModel(objectViewModel, $scope, updatedObject, collectionStates, props, editing, paneId);
                        if (refreshScope) {
                            $scope.object = objectViewModel;
                        }
                    });

        };


        viewModelFactory.domainObjectViewModel = ($scope: INakedObjectsScope, objectRep: DomainObjectRepresentation, collectionStates: _.Dictionary<CollectionViewState>, props: _.Dictionary<Value>, editing: boolean, paneId: number): DomainObjectViewModel => {
            const {objectViewModel, ret} = getObjectViewModel(paneId, objectRep, editing);
            if (ret) {
                return objectViewModel;
            }

            setupDomainObjectViewModel(objectViewModel, $scope, objectRep, collectionStates, props, editing, paneId);
            return objectViewModel;
        };

        viewModelFactory.toolBarViewModel = ($scope) => {
            var tvm = new ToolBarViewModel();

            $scope.$on("ajax-change", (event, count) => tvm.loading = count > 0 ? "Loading..." : "");
            $scope.$on("back", () => navigation.back());
            $scope.$on("forward", () => navigation.forward());

            tvm.goHome = (right?: boolean) => urlManager.setHome(clickHandler.pane(1, right));
            tvm.goBack = () => navigation.back();
            tvm.goForward = () => navigation.forward();
            tvm.swapPanes = () => urlManager.swapPanes();
            tvm.singlePane = (right?: boolean) => {
                urlManager.singlePane(clickHandler.pane(1, right));
                focusManager.refresh(1);
            };

            tvm.template = appBarTemplate;
            tvm.footerTemplate = footerTemplate;

            return tvm;
        };

        let cvm: CiceroViewModel = null;

        viewModelFactory.setCiceroVMIfNecessary = () => {
            if (cvm == null) {
                cvm = new CiceroViewModel();
                cvm.parseInput = (input: string) => {
                    cvm.previousInput = input;
                    commandFactory.parseInput(input, cvm);
                };
                cvm.setOutputToSummaryOfRepresentation = (routeData: PaneRouteData) => {
                    
                    if (routeData.objectId) {
                        const [domainType, ...id] = routeData.objectId.split("-");
                        context.getObject(1, domainType, id) //TODO: move following code out into a ICireroRenderers service with methods for rendering each context type
                            .then((obj: DomainObjectRepresentation) => {
                                let output = ""; //TODO: use builder
                                const type = _.last(obj.domainType().split("."));
                                if (routeData.edit) {
                                    output += "Editing ";
                                }
                                output += type + ": " + obj.title() + ". ";
                                output += renderActionDialogIfOpen(obj, routeData, urlManager);
                                cvm.clearInput(); 
                                cvm.output = output;
                            });
                    }
                    else if (routeData.menuId) {
                        context.getMenu(routeData.menuId)
                            .then((menu: MenuRepresentation) => {
                                let output = ""; //TODO: use builder
                                output += menu.title() + " menu" + ". ";
                                output += renderActionDialogIfOpen(menu, routeData, urlManager);
                                cvm.clearInput(); 
                                cvm.output = output;
                            });
                    }
                    else {
                        cvm.clearInput(); 
                        cvm.output = "Welcome to Cicero";
                    }  
                                  
                };
            }
            return cvm;
        };

        viewModelFactory.getCiceroVM = () => {
            return cvm;
        };
    });

    function renderActionDialogIfOpen(
        context: IHasActions,
        routeData: PaneRouteData,
        urlManager: IUrlManager): string {
        let output = "";
        if (routeData.dialogId) {
            const actionMember = context.actionMember(routeData.dialogId);
            const actionName = actionMember.extensions().friendlyName();
            output += "Action dialog: " + actionName + ". ";
            _.forEach(actionMember.parameters(), (param: Parameter) => {
                output += param.extensions().friendlyName();
                
                //TODO: get param value (from UrlManager)
            });
        }
        return output;
    }
}
import { Component, Input, ElementRef, OnInit, HostListener, ViewChildren, QueryList, Renderer, NgZone } from '@angular/core';
import * as Models from '../models';
import { FieldComponent } from '../field/field.component';
import { FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { ErrorService } from '../error.service';
import { ContextService } from '../context.service';
import { AttachmentViewModel } from '../view-models/attachment-view-model';
import { FieldViewModel } from '../view-models/field-view-model';
import { PropertyViewModel } from '../view-models/property-view-model';
import { DomainObjectViewModel } from '../view-models/domain-object-view-model';
import { ChoiceViewModel } from '../view-models/choice-view-model';
import { ConfigService } from '../config.service';
import { LoggerService } from '../logger.service';

@Component({
    selector: 'nof-edit-property',
    template: require('./edit-property.component.html'),
    styles: [require('./edit-property.component.css')]
})
export class EditPropertyComponent extends FieldComponent implements OnInit {

    constructor(
        myElement: ElementRef,
        private readonly router: Router,
        private readonly error: ErrorService,
        context: ContextService,
        configService: ConfigService,
        loggerService: LoggerService,
        renderer : Renderer
    ) {
        super(myElement, context, configService, loggerService, renderer);
    }

    prop: PropertyViewModel;

    @Input()
    parent: DomainObjectViewModel;

    @Input()
    set property(value: PropertyViewModel) {
        this.prop = value;
        this.droppable = value as FieldViewModel;
    }

    get property() {
        return this.prop;
    }

    get propertyPaneId() {
        return this.property.paneArgId;
    }

    get propertyId() {
        return this.property.id;
    }

    get propertyChoices() {
        return this.property.choices;
    }

    get title() {
        return this.property.title;
    }

    get propertyType() {
        return this.property.type;
    }

    get propertyReturnType() {
        return this.property.returnType;
    }

    get propertyEntryType() {
        return this.property.entryType;
    }

    get isEditable() {
        return this.property.isEditable;
    }

    get formattedValue() {
        return this.property.formattedValue;
    }

    get value() {
        return this.property.formattedValue;
    }

    get format() {
        return this.property.format;
    }

    get isBlob() {
        return this.property.format === "blob";
    }

    get isMultiline() {
        return !(this.property.multipleLines === 1);
    }

    get isPassword() {
        return this.property.password;
    }

    get multilineHeight() {
        return `${this.property.multipleLines * 20}px`;
    }

    get rows() {
        return this.property.multipleLines;
    }

    get propertyDescription() {
        return this.property.description;
    }

    get message() {
        return this.property.getMessage();
    }

    get attachment() {
        return this.property.attachment;
    }

    choiceName(choice: ChoiceViewModel) {
        return choice.name;
    }

    classes(): _.Dictionary<boolean> {
        return {
            [this.prop.color]: true,
            "candrop": this.canDrop,
            "mat-input-element": null as boolean // remove this class to prevent angular/materials styling overiding our styling
        };
    }

    @Input()
    set form(fm: FormGroup) {
        this.formGroup = fm;
    }

    get form() {
        return this.formGroup;
    }

    ngOnInit(): void {
        super.init(this.parent, this.property, this.form.controls[this.prop.id]);
    }

    datePickerChanged(evt: Event) {
        const val = (evt.target as HTMLInputElement).value;
        this.formGroup.value[this.property.id] = val;
    }

    @HostListener('keydown', ['$event'])
    onKeydown(event: KeyboardEvent) {
        this.paste(event);
        // catch and filter enters or they will submit form
        this.filterEnter(event);
    }

    @HostListener('keypress', ['$event'])
    onKeypress(event: KeyboardEvent) {
        this.paste(event);
        // catch and filter enters or they will submit form
        this.filterEnter(event);
    }

    @ViewChildren("focus")
    focusList: QueryList<ElementRef>;
}
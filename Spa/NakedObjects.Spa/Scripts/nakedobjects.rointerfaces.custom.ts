﻿module NakedObjects.RoInterfaces.Custom {
 
    export interface ICustomExtensions extends RoInterfaces.IExtensions {
        "x-ro-nof-choices"?: { [index: string]: (string | number | boolean | ILink)[];}
        "x-ro-nof-menuPath"?: string;
        "x-ro-nof-mask"?: string;
        "x-ro-nof-renderInEditMode"? : boolean;
    }  

    export interface IPagination {
        page: number;
        pageSize: number;
        numPages: number;
        totalCount: number;
    }

    export interface ICustomListRepresentation extends RoInterfaces.IListRepresentation {
        pagination? : IPagination;
        members: { [index: string]: IActionMember };
    }

    export interface IMenuRepresentation extends IResourceRepresentation {
        members: { [index: string]: IActionMember };
        title: string;
        menuId: string;
    }
}
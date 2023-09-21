/* tslint:disable */
/* eslint-disable */
/**
 * Approval Service API
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: v1
 * 
 *
 * NOTE: This class is auto generated by OpenAPI Generator (https://openapi-generator.tech).
 * https://openapi-generator.tech
 * Do not edit the class manually.
 */

import { exists, mapValues } from '../runtime';
/**
 * 
 * @export
 * @interface CommonModelsApprovalPersonalIdentityModel
 */
export interface CommonModelsApprovalPersonalIdentityModel {
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalPersonalIdentityModel
     */
    source?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalPersonalIdentityModel
     */
    lastName?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalPersonalIdentityModel
     */
    firstName?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalPersonalIdentityModel
     */
    dateOfBirth?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalPersonalIdentityModel
     */
    phone?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalPersonalIdentityModel
     */
    eMail?: string | null;
}

/**
 * Check if a given object implements the CommonModelsApprovalPersonalIdentityModel interface.
 */
export function instanceOfCommonModelsApprovalPersonalIdentityModel(value: object): boolean {
    let isInstance = true;

    return isInstance;
}

export function CommonModelsApprovalPersonalIdentityModelFromJSON(json: any): CommonModelsApprovalPersonalIdentityModel {
    return CommonModelsApprovalPersonalIdentityModelFromJSONTyped(json, false);
}

export function CommonModelsApprovalPersonalIdentityModelFromJSONTyped(json: any, ignoreDiscriminator: boolean): CommonModelsApprovalPersonalIdentityModel {
    if ((json === undefined) || (json === null)) {
        return json;
    }
    return {
        
        'source': !exists(json, 'source') ? undefined : json['source'],
        'lastName': !exists(json, 'lastName') ? undefined : json['lastName'],
        'firstName': !exists(json, 'firstName') ? undefined : json['firstName'],
        'dateOfBirth': !exists(json, 'dateOfBirth') ? undefined : json['dateOfBirth'],
        'phone': !exists(json, 'phone') ? undefined : json['phone'],
        'eMail': !exists(json, 'eMail') ? undefined : json['eMail'],
    };
}

export function CommonModelsApprovalPersonalIdentityModelToJSON(value?: CommonModelsApprovalPersonalIdentityModel | null): any {
    if (value === undefined) {
        return undefined;
    }
    if (value === null) {
        return null;
    }
    return {
        
        'source': value.source,
        'lastName': value.lastName,
        'firstName': value.firstName,
        'dateOfBirth': value.dateOfBirth,
        'phone': value.phone,
        'eMail': value.eMail,
    };
}

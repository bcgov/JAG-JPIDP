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
import type { CommonModelsApprovalRequestModel } from './CommonModelsApprovalRequestModel';
import {
    CommonModelsApprovalRequestModelFromJSON,
    CommonModelsApprovalRequestModelFromJSONTyped,
    CommonModelsApprovalRequestModelToJSON,
} from './CommonModelsApprovalRequestModel';

/**
 * 
 * @export
 * @interface CommonModelsApprovalApprovalModel
 */
export interface CommonModelsApprovalApprovalModel {
    /**
     * 
     * @type {number}
     * @memberof CommonModelsApprovalApprovalModel
     */
    id?: number;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    reason?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    requiredAccess?: string | null;
    /**
     * 
     * @type {number}
     * @memberof CommonModelsApprovalApprovalModel
     */
    noOfApprovalsRequired?: number;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    approved?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    deleted?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    created?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    modified?: object;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    userId?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    identityProvider?: string | null;
    /**
     * 
     * @type {Array<CommonModelsApprovalRequestModel>}
     * @memberof CommonModelsApprovalApprovalModel
     */
    requests?: Array<CommonModelsApprovalRequestModel> | null;
}

/**
 * Check if a given object implements the CommonModelsApprovalApprovalModel interface.
 */
export function instanceOfCommonModelsApprovalApprovalModel(value: object): boolean {
    let isInstance = true;

    return isInstance;
}

export function CommonModelsApprovalApprovalModelFromJSON(json: any): CommonModelsApprovalApprovalModel {
    return CommonModelsApprovalApprovalModelFromJSONTyped(json, false);
}

export function CommonModelsApprovalApprovalModelFromJSONTyped(json: any, ignoreDiscriminator: boolean): CommonModelsApprovalApprovalModel {
    if ((json === undefined) || (json === null)) {
        return json;
    }
    return {
        
        'id': !exists(json, 'id') ? undefined : json['id'],
        'reason': !exists(json, 'reason') ? undefined : json['reason'],
        'requiredAccess': !exists(json, 'requiredAccess') ? undefined : json['requiredAccess'],
        'noOfApprovalsRequired': !exists(json, 'noOfApprovalsRequired') ? undefined : json['noOfApprovalsRequired'],
        'approved': !exists(json, 'approved') ? undefined : json['approved'],
        'deleted': !exists(json, 'deleted') ? undefined : json['deleted'],
        'created': !exists(json, 'created') ? undefined : json['created'],
        'modified': !exists(json, 'modified') ? undefined : json['modified'],
        'userId': !exists(json, 'userId') ? undefined : json['userId'],
        'identityProvider': !exists(json, 'identityProvider') ? undefined : json['identityProvider'],
        'requests': !exists(json, 'requests') ? undefined : (json['requests'] === null ? null : (json['requests'] as Array<any>).map(CommonModelsApprovalRequestModelFromJSON)),
    };
}

export function CommonModelsApprovalApprovalModelToJSON(value?: CommonModelsApprovalApprovalModel | null): any {
    if (value === undefined) {
        return undefined;
    }
    if (value === null) {
        return null;
    }
    return {
        
        'id': value.id,
        'reason': value.reason,
        'requiredAccess': value.requiredAccess,
        'noOfApprovalsRequired': value.noOfApprovalsRequired,
        'approved': value.approved,
        'deleted': value.deleted,
        'created': value.created,
        'modified': value.modified,
        'userId': value.userId,
        'identityProvider': value.identityProvider,
        'requests': value.requests === undefined ? undefined : (value.requests === null ? null : (value.requests as Array<any>).map(CommonModelsApprovalRequestModelToJSON)),
    };
}


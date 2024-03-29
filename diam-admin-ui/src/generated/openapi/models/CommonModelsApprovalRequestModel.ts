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
import type { CommonModelsApprovalApprovalHistoryModel } from './CommonModelsApprovalApprovalHistoryModel';
import {
    CommonModelsApprovalApprovalHistoryModelFromJSON,
    CommonModelsApprovalApprovalHistoryModelFromJSONTyped,
    CommonModelsApprovalApprovalHistoryModelToJSON,
} from './CommonModelsApprovalApprovalHistoryModel';
import type { CommonModelsApprovalApprovalStatus } from './CommonModelsApprovalApprovalStatus';
import {
    CommonModelsApprovalApprovalStatusFromJSON,
    CommonModelsApprovalApprovalStatusFromJSONTyped,
    CommonModelsApprovalApprovalStatusToJSON,
} from './CommonModelsApprovalApprovalStatus';

/**
 * 
 * @export
 * @interface CommonModelsApprovalRequestModel
 */
export interface CommonModelsApprovalRequestModel {
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalRequestModel
     */
    created?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalRequestModel
     */
    modified?: object;
    /**
     * 
     * @type {number}
     * @memberof CommonModelsApprovalRequestModel
     */
    requestId?: number;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalRequestModel
     */
    requestType?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalRequestModel
     */
    approvalType?: string | null;
    /**
     * 
     * @type {CommonModelsApprovalApprovalStatus}
     * @memberof CommonModelsApprovalRequestModel
     */
    status?: CommonModelsApprovalApprovalStatus;
    /**
     * 
     * @type {Array<CommonModelsApprovalApprovalHistoryModel>}
     * @memberof CommonModelsApprovalRequestModel
     */
    history?: Array<CommonModelsApprovalApprovalHistoryModel> | null;
}

/**
 * Check if a given object implements the CommonModelsApprovalRequestModel interface.
 */
export function instanceOfCommonModelsApprovalRequestModel(value: object): boolean {
    let isInstance = true;

    return isInstance;
}

export function CommonModelsApprovalRequestModelFromJSON(json: any): CommonModelsApprovalRequestModel {
    return CommonModelsApprovalRequestModelFromJSONTyped(json, false);
}

export function CommonModelsApprovalRequestModelFromJSONTyped(json: any, ignoreDiscriminator: boolean): CommonModelsApprovalRequestModel {
    if ((json === undefined) || (json === null)) {
        return json;
    }
    return {
        
        'created': !exists(json, 'created') ? undefined : json['created'],
        'modified': !exists(json, 'modified') ? undefined : json['modified'],
        'requestId': !exists(json, 'requestId') ? undefined : json['requestId'],
        'requestType': !exists(json, 'requestType') ? undefined : json['requestType'],
        'approvalType': !exists(json, 'approvalType') ? undefined : json['approvalType'],
        'status': !exists(json, 'status') ? undefined : CommonModelsApprovalApprovalStatusFromJSON(json['status']),
        'history': !exists(json, 'history') ? undefined : (json['history'] === null ? null : (json['history'] as Array<any>).map(CommonModelsApprovalApprovalHistoryModelFromJSON)),
    };
}

export function CommonModelsApprovalRequestModelToJSON(value?: CommonModelsApprovalRequestModel | null): any {
    if (value === undefined) {
        return undefined;
    }
    if (value === null) {
        return null;
    }
    return {
        
        'created': value.created,
        'modified': value.modified,
        'requestId': value.requestId,
        'requestType': value.requestType,
        'approvalType': value.approvalType,
        'status': CommonModelsApprovalApprovalStatusToJSON(value.status),
        'history': value.history === undefined ? undefined : (value.history === null ? null : (value.history as Array<any>).map(CommonModelsApprovalApprovalHistoryModelToJSON)),
    };
}


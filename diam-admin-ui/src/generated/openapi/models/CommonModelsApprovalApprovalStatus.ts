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


/**
 * 
 * @export
 */
export const CommonModelsApprovalApprovalStatus = {
    NUMBER_0: 0,
    NUMBER_1: 1,
    NUMBER_2: 2,
    NUMBER_3: 3,
    NUMBER_4: 4
} as const;
export type CommonModelsApprovalApprovalStatus = typeof CommonModelsApprovalApprovalStatus[keyof typeof CommonModelsApprovalApprovalStatus];


export function CommonModelsApprovalApprovalStatusFromJSON(json: any): CommonModelsApprovalApprovalStatus {
    return CommonModelsApprovalApprovalStatusFromJSONTyped(json, false);
}

export function CommonModelsApprovalApprovalStatusFromJSONTyped(json: any, ignoreDiscriminator: boolean): CommonModelsApprovalApprovalStatus {
    return json as CommonModelsApprovalApprovalStatus;
}

export function CommonModelsApprovalApprovalStatusToJSON(value?: CommonModelsApprovalApprovalStatus | null): any {
    return value as any;
}

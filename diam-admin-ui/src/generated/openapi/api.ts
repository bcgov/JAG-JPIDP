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


import type { Configuration } from './configuration';
import type { AxiosPromise, AxiosInstance, AxiosRequestConfig } from 'axios';
import globalAxios from 'axios';
// Some imports not used depending on template conditions
// @ts-ignore
import { DUMMY_BASE_URL, assertParamExists, setApiKeyToObject, setBasicAuthToObject, setBearerAuthToObject, setOAuthToObject, setSearchParams, serializeDataIfNeeded, toPathString, createRequestFunction } from './common';
import type { RequestArgs } from './base';
// @ts-ignore
import { BASE_PATH, COLLECTION_FORMATS, BaseAPI, RequiredError } from './base';

/**
 * 
 * @export
 * @interface CommonModelsApprovalApprovalHistoryModel
 */
export interface CommonModelsApprovalApprovalHistoryModel {
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalHistoryModel
     */
    'decisionNote'?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalHistoryModel
     */
    'approver'?: string | null;
    /**
     * 
     * @type {number}
     * @memberof CommonModelsApprovalApprovalHistoryModel
     */
    'approvalRequestId'?: number;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalHistoryModel
     */
    'deleted'?: object;
}
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
    'id'?: number;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'reason'?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'requiredAccess'?: string | null;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'approved'?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'deleted'?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'created'?: object;
    /**
     * 
     * @type {object}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'modified'?: object;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'userId'?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'identityProvider'?: string | null;
    /**
     * 
     * @type {Array<CommonModelsApprovalRequestModel>}
     * @memberof CommonModelsApprovalApprovalModel
     */
    'requests'?: Array<CommonModelsApprovalRequestModel> | null;
}
/**
 * 
 * @export
 * @enum {string}
 */

export const CommonModelsApprovalApprovalStatus = {
    NUMBER_0: 0,
    NUMBER_1: 1,
    NUMBER_2: 2,
    NUMBER_3: 3,
    NUMBER_4: 4
} as const;

export type CommonModelsApprovalApprovalStatus = typeof CommonModelsApprovalApprovalStatus[keyof typeof CommonModelsApprovalApprovalStatus];


/**
 * 
 * @export
 * @interface CommonModelsApprovalApproveDenyInput
 */
export interface CommonModelsApprovalApproveDenyInput {
    /**
     * 
     * @type {number}
     * @memberof CommonModelsApprovalApproveDenyInput
     */
    'approvalRequestId'?: number;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApproveDenyInput
     */
    'created'?: string;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApproveDenyInput
     */
    'decisionNotes'?: string | null;
    /**
     * 
     * @type {boolean}
     * @memberof CommonModelsApprovalApproveDenyInput
     */
    'approved'?: boolean;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalApproveDenyInput
     */
    'approverUserId'?: string | null;
}
/**
 * 
 * @export
 * @interface CommonModelsApprovalRequestModel
 */
export interface CommonModelsApprovalRequestModel {
    /**
     * 
     * @type {number}
     * @memberof CommonModelsApprovalRequestModel
     */
    'requestId'?: number;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalRequestModel
     */
    'requestType'?: string | null;
    /**
     * 
     * @type {string}
     * @memberof CommonModelsApprovalRequestModel
     */
    'approvalType'?: string | null;
    /**
     * 
     * @type {CommonModelsApprovalApprovalStatus}
     * @memberof CommonModelsApprovalRequestModel
     */
    'status'?: CommonModelsApprovalApprovalStatus;
    /**
     * 
     * @type {Array<CommonModelsApprovalApprovalHistoryModel>}
     * @memberof CommonModelsApprovalRequestModel
     */
    'history'?: Array<CommonModelsApprovalApprovalHistoryModel> | null;
}


/**
 * 
 * @export
 * @interface MicrosoftAspNetCoreMvcProblemDetails
 */
export interface MicrosoftAspNetCoreMvcProblemDetails {
    [key: string]: any;

    /**
     * 
     * @type {string}
     * @memberof MicrosoftAspNetCoreMvcProblemDetails
     */
    'type'?: string | null;
    /**
     * 
     * @type {string}
     * @memberof MicrosoftAspNetCoreMvcProblemDetails
     */
    'title'?: string | null;
    /**
     * 
     * @type {number}
     * @memberof MicrosoftAspNetCoreMvcProblemDetails
     */
    'status'?: number | null;
    /**
     * 
     * @type {string}
     * @memberof MicrosoftAspNetCoreMvcProblemDetails
     */
    'detail'?: string | null;
    /**
     * 
     * @type {string}
     * @memberof MicrosoftAspNetCoreMvcProblemDetails
     */
    'instance'?: string | null;
}

/**
 * ApprovalsApi - axios parameter creator
 * @export
 */
export const ApprovalsApiAxiosParamCreator = function (configuration?: Configuration) {
    return {
        /**
         * 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiApprovalsPendingGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/approvals/pending`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, DUMMY_BASE_URL);
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }

            const localVarRequestOptions = { method: 'GET', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            await setApiKeyToObject(localVarHeaderParameter, "Authorization", configuration)


    
            setSearchParams(localVarUrlObj, localVarQueryParameter);
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: toPathString(localVarUrlObj),
                options: localVarRequestOptions,
            };
        },
        /**
         * 
         * @param {CommonModelsApprovalApproveDenyInput} [commonModelsApprovalApproveDenyInput] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiApprovalsResponsePost: async (commonModelsApprovalApproveDenyInput?: CommonModelsApprovalApproveDenyInput, options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/approvals/response`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, DUMMY_BASE_URL);
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }

            const localVarRequestOptions = { method: 'POST', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            await setApiKeyToObject(localVarHeaderParameter, "Authorization", configuration)


    
            localVarHeaderParameter['Content-Type'] = 'application/json';

            setSearchParams(localVarUrlObj, localVarQueryParameter);
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};
            localVarRequestOptions.data = serializeDataIfNeeded(commonModelsApprovalApproveDenyInput, localVarRequestOptions, configuration)

            return {
                url: toPathString(localVarUrlObj),
                options: localVarRequestOptions,
            };
        },
    }
};

/**
 * ApprovalsApi - functional programming interface
 * @export
 */
export const ApprovalsApiFp = function(configuration?: Configuration) {
    const localVarAxiosParamCreator = ApprovalsApiAxiosParamCreator(configuration)
    return {
        /**
         * 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiApprovalsPendingGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => AxiosPromise<Array<CommonModelsApprovalApprovalModel>>> {
            const localVarAxiosArgs = await localVarAxiosParamCreator.apiApprovalsPendingGet(options);
            return createRequestFunction(localVarAxiosArgs, globalAxios, BASE_PATH, configuration);
        },
        /**
         * 
         * @param {CommonModelsApprovalApproveDenyInput} [commonModelsApprovalApproveDenyInput] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput?: CommonModelsApprovalApproveDenyInput, options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => AxiosPromise<CommonModelsApprovalApprovalModel>> {
            const localVarAxiosArgs = await localVarAxiosParamCreator.apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput, options);
            return createRequestFunction(localVarAxiosArgs, globalAxios, BASE_PATH, configuration);
        },
    }
};

/**
 * ApprovalsApi - factory interface
 * @export
 */
export const ApprovalsApiFactory = function (configuration?: Configuration, basePath?: string, axios?: AxiosInstance) {
    const localVarFp = ApprovalsApiFp(configuration)
    return {
        /**
         * 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiApprovalsPendingGet(options?: any): AxiosPromise<Array<CommonModelsApprovalApprovalModel>> {
            return localVarFp.apiApprovalsPendingGet(options).then((request) => request(axios, basePath));
        },
        /**
         * 
         * @param {CommonModelsApprovalApproveDenyInput} [commonModelsApprovalApproveDenyInput] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput?: CommonModelsApprovalApproveDenyInput, options?: any): AxiosPromise<CommonModelsApprovalApprovalModel> {
            return localVarFp.apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput, options).then((request) => request(axios, basePath));
        },
    };
};

/**
 * ApprovalsApi - interface
 * @export
 * @interface ApprovalsApi
 */
export interface ApprovalsApiInterface {
    /**
     * 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof ApprovalsApiInterface
     */
    apiApprovalsPendingGet(options?: AxiosRequestConfig): AxiosPromise<Array<CommonModelsApprovalApprovalModel>>;

    /**
     * 
     * @param {CommonModelsApprovalApproveDenyInput} [commonModelsApprovalApproveDenyInput] 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof ApprovalsApiInterface
     */
    apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput?: CommonModelsApprovalApproveDenyInput, options?: AxiosRequestConfig): AxiosPromise<CommonModelsApprovalApprovalModel>;

}

/**
 * ApprovalsApi - object-oriented interface
 * @export
 * @class ApprovalsApi
 * @extends {BaseAPI}
 */
export class ApprovalsApi extends BaseAPI implements ApprovalsApiInterface {
    /**
     * 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof ApprovalsApi
     */
    public apiApprovalsPendingGet(options?: AxiosRequestConfig) {
        return ApprovalsApiFp(this.configuration).apiApprovalsPendingGet(options).then((request) => request(this.axios, this.basePath));
    }

    /**
     * 
     * @param {CommonModelsApprovalApproveDenyInput} [commonModelsApprovalApproveDenyInput] 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof ApprovalsApi
     */
    public apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput?: CommonModelsApprovalApproveDenyInput, options?: AxiosRequestConfig) {
        return ApprovalsApiFp(this.configuration).apiApprovalsResponsePost(commonModelsApprovalApproveDenyInput, options).then((request) => request(this.axios, this.basePath));
    }
}


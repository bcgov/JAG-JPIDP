<template>
    <div>

        <table class="table table-striped table-hover caption-top ">
            <caption>Approval History</caption>

            <thead class="table-light">
                <th>Created</th>
                <th>User</th>
                <th>Approved</th>
                <th>Access</th>
            </thead>
            <tbody>
                <tr v-for='(approval) in approvalsData' :key="approval.id">
                    <td>{{ getDateFormatted(approval.created) }}</td>

                    <td>{{ approval.userId }}</td>
                    <td v-if="approval.approved">{{ getDateFormatted(approval.approved) }}</td>
                                        <td v-if="!approval.approved">Not approved</td>

                    <td>{{ approval.requiredAccess }}</td>

                </tr>
            </tbody>
        </table>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import * as bootstrap from 'bootstrap';

import { ApprovalsApi, Configuration, type CommonModelsApprovalApprovalModel } from "../../generated/openapi/index";
import { format, parseISO } from "date-fns";
import type { CommonModelsApprovalApproveDenyInput } from "@/generated/openapi/models/CommonModelsApprovalApproveDenyInput";
import { ApprovalService } from "@/services/ApprovalService";
import apiClient from "@/http-common";
import { useApprovalStore } from "@/stores/approvals";
import { storeToRefs } from "pinia";
const approvalsData = ref();


const currentRequest = ref<CommonModelsApprovalApprovalModel>();

function getDateFormatted(dateIn: string) {
    const parsedTime = parseISO(dateIn);

    return format(parsedTime, "MMM-dd p");
}



const hasCurrentRequest = computed(() => currentRequest.value);

const api = new ApprovalService();

async function loadApprovals() {
    api.approvalApi.apiApprovalsPendingGet({
        pendingOnly: false
    }).then((result: CommonModelsApprovalApprovalModel[]) => {
        approvalsData.value = result;
    });

}

onMounted(() => {
    const approvalStore = useApprovalStore();
    const { refreshCount } = storeToRefs(approvalStore);
    loadApprovals();
    watch(refreshCount, () => {
        loadApprovals();
    })

});
</script>

<style lang="scss" scoped>
.main {
    margin-top: 100px;
}
</style>

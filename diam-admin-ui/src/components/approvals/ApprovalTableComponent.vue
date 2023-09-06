<template>
    <div>
        <h5>Approval History</h5>

        <table class="table table-striped table-hover">

            <thead>
                <tr>
                    <th scope="col">Created</th>
                    <th scope="col">User</th>
                    <th scope="col">Reason</th>
                    <th scope="col">Approved?</th>
                    <th scope="col">Decision Date</th>
                    <th scope="col">Approver(s)</th>
                    <th scope="col">Access</th>
                </tr>
            </thead>
            <tbody v-for="(approval, id) in approvalsData" :key="id">
                <tr :class="{ 'table-success': approval.approved !== undefined, 'table-primary': approval.completed === undefined, 'table-danger': approval.approved === undefined && approval.completed !== undefined}" >
                    <td>{{ getDateFormatted(approval.created) }}</td>
                    <td>{{ approval.userId }}</td>
                    <td>{{ approval.reason }}</td>
                    <td class="justify-content-center">{{ approval.approved !== undefined ? 'Y' : 'N' }}</td>

                    <td v-if="approval.completed !== undefined">{{ getDateFormatted(approval.completed) }}</td>
                    <td :class="{ 'pending': approval.completed === undefined }" v-if="approval.completed === undefined">Pending</td>

                    <td>{{ getApprover(approval) }}</td>

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

function getApprover(approval: CommonModelsApprovalApprovalModel) {

    let approvers = new Set;
    approval.requests?.forEach(req => {
        approvers.add(req.history?.map((history) => history.approver));
    });

    const flattened = Array.from(approvers).flat();

    return Array.from(new Set(flattened)).join(",");

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

.pending {
    color: darkblue;
}
</style>

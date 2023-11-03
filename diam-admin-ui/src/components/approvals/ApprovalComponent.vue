<template>
  <div>
    <div v-if="!hasApprovalsPending">
      <h2>Congrats you're all caught up</h2>
    </div>
    <div v-if="hasApprovalsPending">
      <h5>The following approval(s) need attention:</h5>
      <div class="aligns-items-center text-center col-6">
        <div v-for="pending in approvalsPending" :key="pending.id">
          <div class="card text-center shadow" title="approve">
            <div class="card-header text-white bg-dark bg-warning bg-gradient">
              {{ pending.userId }}
            </div>


            <div class="card-body">
              <h5>{{ pending.requiredAccess }}</h5>

              <table class="table table-striped table-hover caption-top ">

                <thead class="table-light">
                  <th>Source</th>
                  <th>First Name</th>
                  <th>Last Name</th>
                </thead>
                <tbody>
                  <tr v-for='(identity) in pending.personalIdentities' :key="identity.id">
                    <td>{{ identity.source }}</td>
                    <td>{{ identity.firstName }}</td>
                    <td>{{ identity.lastName }}</td>
                  </tr>
                </tbody>
              </table>
              <span class="reason">{{ pending.reason }}</span>


              <div class="mt-3">Received: {{ getDateFormatted(pending.created) }}</div>
              <div class="card-footer">
                EMail: <b>{{ pending.emailAddress }}</b><br />
                Phone: <b>{{ pending.phoneNumber }}</b><br />

              </div>
              <div class="mt-3">
                <button type="button" class="btn btn-danger me-5" @click="clickDeny(pending)">
                  Deny
                </button>
                <button type="button" class="btn btn-primary" @click="approve(pending)">
                  Approve
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="modal fade" tabindex="-1" id="approveDialog" v-if="hasCurrentRequest">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Approve</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <p>
              Approve the request for <b>{{ currentRequest?.userId }}</b>?
            </p>
            <input type="textarea" v-model="approvalNotes" name="approvalNotes" id="approvalNotes" class="form-control"
              placeholder="" />
            <small id="denyId" class="text-muted">Optional approval notes</small>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Cancel
            </button>
            <button type="button" class="btn btn-primary" data-bs-dismiss="modal" @click="approveCurrentRequest">
              Approve Request
            </button>
          </div>
        </div>
      </div>
    </div>
    <div class="modal fade" tabindex="-1" id="denyDialog" v-if="hasCurrentRequest">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Deny Request</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <p>Please provide a reason for the request denial for future reference.</p>
            <form>
              <form class="d-flex">
                <div class="col">
                  <div class="mb-3">
                    <input type="textarea" name="denyReason" id="denyReason" v-model="denialNotes" class="form-control"
                      placeholder="" aria-describedby="denyId" />
                    <small id="denyId" class="text-muted">Reason for denial</small>
                  </div>
                </div>
              </form>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              Cancel
            </button>
            <button type="button" class="btn btn-primary" data-bs-dismiss="modal" @click="denyCurrentRequest">
              Deny Request
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import KeyCloakService from "@/security/KeycloakService";
import { computed, nextTick, onMounted, ref, watch } from "vue";
import * as bootstrap from 'bootstrap';

import { ApprovalsApi, Configuration, type CommonModelsApprovalApprovalModel } from "../../generated/openapi/index";
import { format, parseISO } from "date-fns";
import type { CommonModelsApprovalApproveDenyInput } from "@/generated/openapi/models/CommonModelsApprovalApproveDenyInput";
import { ApprovalService } from "@/services/ApprovalService";
import apiClient from "@/http-common";
import { useApprovalStore } from "@/stores/approvals";
import { storeToRefs } from "pinia";
const approvalsPending = ref();
const approvalNotes = ref('')
const denialNotes = ref('')

const currentRequest = ref<CommonModelsApprovalApprovalModel>();

function getDateFormatted(dateIn: string) {
  const parsedTime = parseISO(dateIn);

  return format(parsedTime, "MMM-dd p");
}

/**
 *
 * @param request Submit request for approval
 */
async function approve(request: CommonModelsApprovalApprovalModel) {
  currentRequest.value = request;
  await nextTick();
  var modal = new bootstrap.Modal('#approveDialog');
  modal.show();
}



async function clickDeny(request: CommonModelsApprovalApprovalModel) {
  currentRequest.value = request;
  await nextTick();
  var modal = new bootstrap.Modal('#denyDialog');
  modal.show();
}

async function denyCurrentRequest() {
  await api.approvalApi.apiApprovalsResponsePost({
    commonModelsApprovalApproveDenyInput: {
      approved: false,
      decisionNotes: approvalNotes.value,
      approvalRequestId: currentRequest.value?.id
    }
  }).then((response: CommonModelsApprovalApprovalModel) => {
    loadPending();
  });
}

async function approveCurrentRequest() {
  await api.approvalApi.apiApprovalsResponsePost({
    commonModelsApprovalApproveDenyInput: {
      approved: true,
      decisionNotes: approvalNotes.value,
      approvalRequestId: currentRequest.value?.id
    }
  }).then((response: CommonModelsApprovalApprovalModel) => {
    loadPending();
  });

}

const hasApprovalsPending = computed(() => approvalsPending.value && approvalsPending.value.length > 0);
const hasCurrentRequest = computed(() => currentRequest.value);

const api = new ApprovalService();

async function loadPending() {
  const approvalStore = useApprovalStore();
  approvalStore.incrementRefresh();

  api.approvalApi.apiApprovalsPendingGet({
    pendingOnly: true
  }).then((result: CommonModelsApprovalApprovalModel[]) => {
    approvalsPending.value = result;
  });

}

onMounted(() => {
  const approvalStore = useApprovalStore();
  const { data } = storeToRefs(approvalStore);
  loadPending();
  watch(data, () => {
    loadPending();
  })

});
</script>

<style lang="scss" scoped>
.main {
  margin-top: 100px;
}

.reason {
  font-size: 0.9em;
}

.contact {
  border: 2px solid blue;
  border-radius: 5px;
  margin-bottom: 8px;
  margin-top: 8px
}
</style>

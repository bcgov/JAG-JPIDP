<template>
  <div class="main container-fluid">
    <div v-if="!hasApprovalsPending">
      <h2>Congrats you're all caught up</h2>
    </div>
    <div v-if="hasApprovalsPending">

      <div
        class="aligns-items-center justify-content-center text-center w-50 position-absolute top-50 start-50 translate-middle">

        <div v-for="pending in approvalsPending" :key="pending.id">
          <div class="card text-center" title="test">
            <div class="card-header text-white bg-dark bg-warning bg-gradient">

              {{ pending.userId }}
            </div>

            <div class="card-body">
              <h5>{{ pending.requiredAccess }}</h5>
              {{ pending.reason }}

              <div class="mt-3">
                Received: {{ getDateFormatted(pending.created) }}
              </div>

              <div class="mt-3">

                <button type="button" class="btn btn-warning me-5" @click="clickDeny(pending)">Deny</button>

                <button type="button" class="btn btn-primary" @click="approve(pending)">Approve</button>
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
            <p>Approve the request for <b>{{ currentRequest.userId }}</b>?</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-primary" @click="approveCurrentRequest">Approve Request</button>
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
                    <label for="" class="form-label">Inline Form</label>
                    <input type="textarea" name="denyReason" id="denyReason" class="form-control" placeholder=""
                      aria-describedby="denyId">
                    <small id="denyId" class="text-muted">Reason for denial</small>
                  </div>
                </div>
              </form>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-primary">Submit</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import KeyCloakService from "@/security/KeycloakService";
import { computed, nextTick, onMounted, ref } from "vue";
import * as bootstrap from 'bootstrap';

import { ApprovalsApi, Configuration, type CommonModelsApprovalApprovalModel } from "../generated/openapi/index";
import { format, parseISO } from "date-fns";
import type { CommonModelsApprovalApproveDenyInput } from "@/generated/openapi/models/CommonModelsApprovalApproveDenyInput";
import { ApprovalService } from "@/services/ApprovalService";
import apiClient from "@/http-common";
const approvalsPending = ref();
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

async function approveCurrentRequest() {

  await api.approvalApi.apiApprovalsResponsePost({
    commonModelsApprovalApproveDenyInput: {
      approved: true,
      approvalRequestId: currentRequest.value?.id
    }
  }).then((response: CommonModelsApprovalApprovalModel) => {
    console.log("Got response %o", response);
  });

}

const hasApprovalsPending = computed(() => approvalsPending.value && approvalsPending.value.length > 0);
const hasCurrentRequest = computed(() => currentRequest.value);

const api = new ApprovalService();


onMounted(() => {

  api.approvalApi.apiApprovalsPendingGet().then((result: CommonModelsApprovalApprovalModel[]) => {
    approvalsPending.value = result;
  });

});
</script>

<style lang="scss" scoped>
.main {
  margin-top: 100px;
}
</style>

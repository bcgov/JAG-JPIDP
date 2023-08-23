<template>
    <div class="toast-container position-fixed bottom-0 end-0 p-3">
        <div id="liveToast" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header">
                <i class="bi me-2" :class="toastIcon"></i>
                <strong class="me-auto">{{ toastHeader }}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">{{ toastMessageBody }}</div>
        </div>
    </div>
    <nav class="navbar navbar-expand-md navbar-dark fixed-top bg-dark">
        <div class="container-fluid">
            <img class="ms-4" src="../assets/images/bcid-logo-rev-en.svg" height="32" alt="BC Logo" />

            <span class="navbar-brand mx-auto px-3">Digital Identity Access Management Admin</span>
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent"
                aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarSupportedContent">
                <ul class="navbar-nav me-auto mb-2 mb-lg-0"></ul>
                <i class="bi bi-broadcast-pin me-2" :class="{ connected: connectionStatus, disconnected: !connectionStatus }"></i>
                <span class="align-middle me-4 text-bcgold"> {{ username }}</span>

                <form class="d-flex">
                    <button type="button" class="btn btn-outline-info" aria-current="page" @click="Logout">
                        Logout
                    </button>
                </form>
            </div>
        </div>
    </nav>
</template>

<style scoped>

    .connected {
        color: lightgreen
    }

    .disconnected {
        color: red
    }

</style>

<script lang="ts" setup>
import { onMounted, ref } from "vue";
import KeyCloakService from "../security/KeycloakService";
import { useApprovalStore } from "@/stores/approvals";
import { storeToRefs } from "pinia";
import * as bootstrap from 'bootstrap';
const username = ref();
const toastMessageBody = ref<string>('');
const toastIcon = ref<string>('');
const toastHeader = ref<string>('');
const connectionStatus = ref<boolean>(false);


function Logout() {
    alert("logout");
    KeyCloakService.CallLogout();
}

function connect() {
    username.value = KeyCloakService.GetUserName();
    const approvalStore = useApprovalStore();
    const { data } = storeToRefs(approvalStore);
    const WS_URL = import.meta.env.VITE_WS_URL;
    console.log("Connect to %s", WS_URL);
    debugger;
    const token = KeyCloakService.GetToken();
    const socket = new WebSocket(WS_URL, token);
    socket.onopen = () => {
        console.log("Socket connection established");
        connectionStatus.value = true;
    };
    socket.onclose = () => {
        console.error("Connection lost - will attempt to reconnect");
        connectionStatus.value = false;

        setTimeout(function () {
            connect();
        }, 1000);
    }
    socket.onmessage = (message: MessageEvent) => {
        const toastElem = document.getElementById("liveToast")!;
        const toast = new bootstrap.Toast(toastElem);
        const messageData: string[] = message.data.split("|");
        const messageType = messageData[0];
        switch (messageType) {
            case "0":
                {
                    toastIcon.value = "bi-checklist"
                    toastHeader.value = "Approval"
                    break;
                }
            case "1":
                {
                    toastIcon.value = "bi-broadcast"
                    toastHeader.value = "Broadcast"
                    break;
                }
            case "2":
                {
                    toastIcon.value = "bi-person"
                    toastHeader.value = "New User Onboarded"
                    break;
                }
            case "3":
                {
                    toastIcon.value = "bi-error"
                    toastHeader.value = "Error"
                    break;
                }
        }
        toastMessageBody.value = messageData[1];
        toast.show();

        data.value = message.data;
    };
    socket.onerror = () => {
        console.error("Websocket error");
    };
}

onMounted(() => {
    connect();
});
</script>

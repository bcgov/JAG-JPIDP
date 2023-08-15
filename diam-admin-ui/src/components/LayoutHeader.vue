

<template>
    <nav class="navbar navbar-expand-md navbar-dark fixed-top bg-dark">
        <div class="container-fluid">
            <img class="ms-4" src="../assets/images/bcid-logo-rev-en.svg" height="32" alt="BC Logo" />

            <span class="navbar-brand mx-auto px-3">Digital Identity Access Management Admin</span>
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent"
                aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarSupportedContent">
        
                <ul class="navbar-nav me-auto mb-2 mb-lg-0">
  

                </ul>
                <span class="align-middle me-4 text-bcgold"> {{ username }}</span>

                <form class="d-flex ">

                    <button type="button" class="btn btn-outline-info" aria-current="page" @click="Logout">Logout</button>
                </form>

            </div>

        </div>
    </nav>
</template>


<script lang="ts" setup>
import axios from 'axios';
import { onMounted, ref } from 'vue'

import KeyCloakService from "../security/KeycloakService";

const username = ref();

function Logout() {
    alert('logout');
    KeyCloakService.CallLogout();
}

onMounted(() => {
    username.value = KeyCloakService.GetUserName();
})

</script>
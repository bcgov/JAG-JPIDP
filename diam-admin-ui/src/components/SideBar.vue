<template>
  <div class="d-flex flex-column flex-shrink-0 p-3 bg-dark h-100">
      <a href="/" class="d-flex align-items-center mb-3 mb-md-0 me-md-auto link-light text-decoration-none">
        <svg class="bi me-2" width="40" height="32"><use xlink:href="#bootstrap"/></svg>
        <span class="fs-4">Sidebar</span>
      </a>
      <hr>
      <ul class="nav nav-pills flex-column mb-auto">
        <li class="nav-item">
                            <router-link  class="me-2 nav-link link-light" to="/"><i class="bi bi-house-fill link-light me-2"></i>Home</router-link>

        </li>
        <!-- <li>
          <a href="#" class="nav-link link-light">
            <svg class="bi me-2" width="16" height="16"><use xlink:href="#speedometer2"/></svg>
            Dashboard
          </a>
        </li> -->
   
        <li>
   
                    <router-link  class="nav-link link-light" to="/approvals"> <i class="bi bi-card-checklist link-light me-2"></i>Approvals</router-link>

    
        </li>
        <li>
                      <router-link  class="nav-link link-light" to="/users"><i class="bi bi-people-fill link-light me-2"></i>Users</router-link>

        </li>
      </ul>
      </div>

</template>

<script lang="ts" setup>


</script>

<style lang="scss" scoped>

      .bd-placeholder-img {
        font-size: 1.125rem;
        text-anchor: middle;
        -webkit-user-select: none;
        -moz-user-select: none;
        user-select: none;
      }

      @media (min-width: 768px) {
        .bd-placeholder-img-lg {
          font-size: 3.5rem;
        }
      }


</style>
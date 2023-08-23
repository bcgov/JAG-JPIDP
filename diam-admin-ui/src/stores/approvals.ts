import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useApprovalStore = defineStore('approvals', () => {
  const data = ref([])
  const refreshCount = ref(0);

   function incrementRefresh() {
    refreshCount.value++
  }
  return { data, refreshCount, incrementRefresh }
})

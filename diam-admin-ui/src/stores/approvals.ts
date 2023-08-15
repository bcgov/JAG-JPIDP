import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import axios from "axios"

export const useApprovalStore = defineStore('approvals', () => {
  const data = ref([])

  return { data }
})

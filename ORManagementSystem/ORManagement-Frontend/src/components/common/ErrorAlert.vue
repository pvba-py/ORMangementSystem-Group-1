<script setup>
defineProps({
  message: {
    type: String,
    default: ''
  }
})
</script>

<template>
  <div v-if="message" class="alert alert-danger d-flex align-items-center">
    <i class="bi bi-exclamation-triangle-fill me-2"></i>
    <span>{{ message }}</span>
  </div>
</template>
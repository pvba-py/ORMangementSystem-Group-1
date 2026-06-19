<script setup>
defineProps({
  title: {
    type: String,
    default: 'No records found'
  },
  message: {
    type: String,
    default: 'There is no data to display right now.'
  },
  icon: {
    type: String,
    default: 'bi-inbox'
  }
})
</script>

<template>
  <div class="text-center py-5 text-muted">
    <i :class="`bi ${icon} display-4 d-block mb-3`"></i>
    <h5>{{ title }}</h5>
    <p class="mb-0">{{ message }}</p>
  </div>
</template>
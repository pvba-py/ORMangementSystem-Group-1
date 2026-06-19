<script setup>
defineProps({
  title: {
    type: String,
    required: true
  },
  subtitle: {
    type: String,
    default: ''
  },
  icon: {
    type: String,
    default: 'bi-grid'
  }
})
</script>

<template>
  <div class="d-flex align-items-center justify-content-between mb-4">
    <div>
      <h3 class="mb-1">
        <i :class="`bi ${icon} me-2 text-primary`"></i>
        {{ title }}
      </h3>
      <p v-if="subtitle" class="text-muted mb-0">
        {{ subtitle }}
      </p>
    </div>

    <div>
      <slot name="actions"></slot>
    </div>
  </div>
</template>
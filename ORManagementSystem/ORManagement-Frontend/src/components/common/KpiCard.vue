<script setup>
defineProps({
  label: {
    type: String,
    required: true
  },
  value: {
    type: [String, Number],
    default: 0
  },
  icon: {
    type: String,
    default: 'bi-bar-chart'
  },
  color: {
    type: String,
    default: 'primary'
  }
})
</script>

<template>
  <div class="card border-0 shadow-sm h-100">
    <div class="card-body d-flex align-items-center justify-content-between">
      <div>
        <p class="text-muted mb-1">{{ label }}</p>
        <h3 class="mb-0">{{ value }}</h3>
      </div>

      <div :class="`kpi-icon bg-${color}-subtle text-${color}`">
        <i :class="`bi ${icon}`"></i>
      </div>
    </div>
  </div>
</template>

<style scoped>
.kpi-icon {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}
</style>
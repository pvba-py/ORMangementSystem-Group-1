<script setup>
defineProps({
  show: {
    type: Boolean,
    default: false
  },
  title: {
    type: String,
    default: ''
  },
  size: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['close'])
</script>

<template>
  <teleport to="body">
    <div
      v-if="show"
      class="modal fade show d-block"
      tabindex="-1"
      role="dialog"
    >
      <div
        class="modal-dialog modal-dialog-centered modal-dialog-scrollable"
        :class="size ? `modal-${size}` : ''"
      >
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              {{ title }}
            </h5>

            <button
              type="button"
              class="btn-close"
              @click="emit('close')"
            ></button>
          </div>

          <div class="modal-body">
            <slot></slot>
          </div>

          <div class="modal-footer">
            <slot name="footer">
              <button
                type="button"
                class="btn btn-outline-secondary"
                @click="emit('close')"
              >
                Close
              </button>
            </slot>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="show"
      class="modal-backdrop fade show"
    ></div>
  </teleport>
</template>

<style scoped>
.modal {
  background: rgba(0, 0, 0, 0.05);
}

.modal-content {
  border-radius: 14px;
  border: none;
  box-shadow: 0 20px 40px rgba(15, 23, 42, 0.25);
}

.modal-header {
  border-bottom: 1px solid #e5e7eb;
}

.modal-footer {
  border-top: 1px solid #e5e7eb;
}
</style>
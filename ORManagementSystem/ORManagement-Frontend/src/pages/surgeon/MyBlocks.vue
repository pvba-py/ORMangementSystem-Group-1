<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import { getMyBlocks, releaseBlock } from '../../services/blockService'
import { formatDate, formatTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const releasing = ref(false)
const blocks = ref([])
const selectedBlock = ref(null)

const releaseForm = ref({
  startTime: '',
  endTime: '',
  remarks: ''
})

const loadBlocks = async () => {
  loading.value = true

  try {
    const response = await getMyBlocks()
    blocks.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load blocks.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const openRelease = (block) => {
  selectedBlock.value = block
  releaseForm.value = {
    startTime: formatTime(block.startTime),
    endTime: formatTime(block.endTime),
    remarks: ''
  }
}

const submitRelease = async () => {
  if (!selectedBlock.value) return

  if (!releaseForm.value.startTime || !releaseForm.value.endTime) {
    showToast('Start and end time are required.', 'warning')
    return
  }

  releasing.value = true

  try {
    await releaseBlock(selectedBlock.value.blockId, {
      startTime: `${releaseForm.value.startTime}:00`,
      endTime: `${releaseForm.value.endTime}:00`,
      remarks: releaseForm.value.remarks
    })

    showToast('Block time released successfully.', 'success')
    selectedBlock.value = null
    await loadBlocks()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to release block.'

    showToast(message, 'error')
  } finally {
    releasing.value = false
  }
}

onMounted(loadBlocks)
</script>

<template>
  <div>
    <PageHeader
      title="My Blocks"
      subtitle="View assigned OR blocks and release unused time"
      icon="bi-calendar-week"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else class="page-card">
      <EmptyState
        v-if="blocks.length === 0"
        title="No assigned blocks"
        message="You do not have any assigned OR blocks."
        icon="bi-calendar-x"
      />

      <div v-else class="table-responsive">
        <table class="table table-hover align-middle">
          <thead>
            <tr>
              <th>Block</th>
              <th>Room</th>
              <th>Date</th>
              <th>Time</th>
              <th>Type</th>
              <th>Status</th>
              <th>Remarks</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="block in blocks" :key="block.blockId">
              <td>#{{ block.blockId }}</td>
              <td>{{ block.roomName }}</td>
              <td>{{ formatDate(block.blockDate) }}</td>
              <td>{{ formatTime(block.startTime) }} - {{ formatTime(block.endTime) }}</td>
              <td>{{ block.blockType }}</td>
              <td>
                <StatusBadge :status="block.blockStatus" />
              </td>
              <td>{{ block.remarks || '-' }}</td>
              <td class="text-end">
                <button
                  class="btn btn-sm btn-outline-primary"
                  :disabled="block.blockStatus === 'Released' || block.blockStatus === 'Cancelled'"
                  @click="openRelease(block)"
                >
                  Release Time
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <AppModal
  :show="!!selectedBlock"
  :title="selectedBlock ? `Release Block #${selectedBlock.blockId}` : 'Release Block'"
  size="lg"
  @close="selectedBlock = null"
>
  <div v-if="selectedBlock" class="alert alert-info">
    Block time:
    <strong>
      {{ formatTime(selectedBlock.startTime) }} - {{ formatTime(selectedBlock.endTime) }}
    </strong>
    on
    <strong>{{ formatDate(selectedBlock.blockDate) }}</strong>
  </div>

  <div class="row g-3">
    <div class="col-md-4">
      <label class="form-label">Release Start</label>
      <input
        v-model="releaseForm.startTime"
        type="time"
        class="form-control"
      />
    </div>

    <div class="col-md-4">
      <label class="form-label">Release End</label>
      <input
        v-model="releaseForm.endTime"
        type="time"
        class="form-control"
      />
    </div>

    <div class="col-md-12">
      <label class="form-label">Remarks</label>
      <input
        v-model="releaseForm.remarks"
        class="form-control"
      />
    </div>
  </div>

  <template #footer>
    <button
      class="btn btn-outline-secondary"
      @click="selectedBlock = null"
    >
      Cancel
    </button>

    <button
      class="btn btn-primary"
      :disabled="releasing"
      @click="submitRelease"
    >
      <span
        v-if="releasing"
        class="spinner-border spinner-border-sm me-2"
      ></span>
      Release
    </button>
  </template>
</AppModal>
  </div>
</template>
<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import { getCases, createCase, updateCase, updateCaseStatus } from '../../services/caseService'
import { getRequests } from '../../services/requestService'
import { getBlocks } from '../../services/blockService'
import { formatDate, formatDateTime, formatTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)

const cases = ref([])
const approvedRequests = ref([])
const blocks = ref([])

const statusFilter = ref('')

const selectedCase = ref(null)
const selectedStatusCase = ref(null)

const createForm = ref({
  requestId: '',
  blockId: '',
  scheduledStart: '',
  scheduledEnd: ''
})

const updateForm = ref({
  scheduledStart: '',
  scheduledEnd: ''
})

const statusForm = ref({
  status: 'InProgress',
  actualStart: '',
  actualEnd: '',
  cancellationReason: ''
})

const toDateTimeLocal = value => {
  if (!value) return ''

  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  const localDate = new Date(date.getTime() - offset * 60000)

  return localDate.toISOString().slice(0, 16)
}

const loadCases = async () => {
  loading.value = true

  try {
    const params = {}

    if (statusFilter.value) {
      params.status = statusFilter.value
    }

    const response = await getCases(params)
    cases.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load cases.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const loadSupportingData = async () => {
  try {
    const [approvedResponse, modifiedResponse, blocksResponse] = await Promise.all([
      getRequests({ status: 'Approved' }),
      getRequests({ status: 'Modified' }),
      getBlocks()
    ])

    approvedRequests.value = [
      ...(approvedResponse.data || []),
      ...(modifiedResponse.data || [])
    ]

    blocks.value = blocksResponse.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load scheduling data.'

    showToast(message, 'error')
  }
}

const resetCreateForm = () => {
  createForm.value = {
    requestId: '',
    blockId: '',
    scheduledStart: '',
    scheduledEnd: ''
  }
}

const submitCreateCase = async () => {
  if (!createForm.value.requestId || !createForm.value.blockId) {
    showToast('Request and block are required.', 'warning')
    return
  }

  if (!createForm.value.scheduledStart || !createForm.value.scheduledEnd) {
    showToast('Scheduled start and end are required.', 'warning')
    return
  }

  saving.value = true

  try {
    await createCase({
      requestId: Number(createForm.value.requestId),
      blockId: Number(createForm.value.blockId),
      scheduledStart: createForm.value.scheduledStart,
      scheduledEnd: createForm.value.scheduledEnd
    })

    showToast('Surgical case created successfully.', 'success')
    resetCreateForm()
    await Promise.all([loadCases(), loadSupportingData()])
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to create surgical case.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const openUpdateCase = item => {
  selectedCase.value = item

  updateForm.value = {
    scheduledStart: toDateTimeLocal(item.scheduledStart),
    scheduledEnd: toDateTimeLocal(item.scheduledEnd)
  }
}

const submitUpdateCase = async () => {
  if (!selectedCase.value) return

  if (!updateForm.value.scheduledStart || !updateForm.value.scheduledEnd) {
    showToast('Scheduled start and end are required.', 'warning')
    return
  }

  saving.value = true

  try {
    await updateCase(selectedCase.value.surgeryId, {
      scheduledStart: updateForm.value.scheduledStart,
      scheduledEnd: updateForm.value.scheduledEnd
    })

    showToast('Case schedule updated successfully.', 'success')
    selectedCase.value = null
    await loadCases()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update case.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const openStatusCase = item => {
  selectedStatusCase.value = item

  statusForm.value = {
    status: item.caseStatus === 'Scheduled' ? 'InProgress' : 'Completed',
    actualStart: '',
    actualEnd: '',
    cancellationReason: ''
  }
}

const submitStatusUpdate = async () => {
  if (!selectedStatusCase.value) return

  if (!statusForm.value.status) {
    showToast('Please select a status.', 'warning')
    return
  }

  if (statusForm.value.status === 'Cancelled' && !statusForm.value.cancellationReason) {
    showToast('Cancellation reason is required.', 'warning')
    return
  }

  saving.value = true

  try {
    const payload = {
      status: statusForm.value.status,
      actualStart: statusForm.value.actualStart || null,
      actualEnd: statusForm.value.actualEnd || null,
      cancellationReason: statusForm.value.cancellationReason || null
    }

    await updateCaseStatus(selectedStatusCase.value.surgeryId, payload)

    showToast('Case status updated successfully.', 'success')
    selectedStatusCase.value = null
    await loadCases()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update case status.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const canEditCase = item => {
  return item.caseStatus === 'Scheduled'
}

const canChangeStatus = item => {
  return item.caseStatus !== 'Completed' && item.caseStatus !== 'Cancelled'
}

onMounted(async () => {
  await Promise.all([loadCases(), loadSupportingData()])
})
</script>

<template>
  <div>
    <PageHeader
      title="Case Management"
      subtitle="Schedule, update, start, complete, and cancel surgical cases"
      icon="bi-clipboard2-pulse"
    />

    <!-- Create case -->
    <div class="page-card mb-4">
      <h5 class="mb-3">
        <i class="bi bi-plus-circle me-2 text-primary"></i>
        Create Surgical Case
      </h5>

      <div class="row g-3">
        <div class="col-md-4">
          <label class="form-label">Approved / Modified Request</label>
          <select v-model="createForm.requestId" class="form-select">
            <option value="">Select request</option>
            <option
              v-for="request in approvedRequests"
              :key="request.requestId"
              :value="request.requestId"
            >
              #{{ request.requestId }} - {{ request.patientName }} - {{ request.surgeryType }}
            </option>
          </select>
        </div>

        <div class="col-md-4">
          <label class="form-label">Block</label>
          <select v-model="createForm.blockId" class="form-select">
            <option value="">Select block</option>
            <option
              v-for="block in blocks"
              :key="block.blockId"
              :value="block.blockId"
            >
              #{{ block.blockId }} - {{ block.roomName }} -
              {{ formatDate(block.blockDate) }}
              {{ formatTime(block.startTime) }}-{{ formatTime(block.endTime) }}
            </option>
          </select>
        </div>

        <div class="col-md-2">
          <label class="form-label">Scheduled Start</label>
          <input
            v-model="createForm.scheduledStart"
            type="datetime-local"
            class="form-control"
          />
        </div>

        <div class="col-md-2">
          <label class="form-label">Scheduled End</label>
          <input
            v-model="createForm.scheduledEnd"
            type="datetime-local"
            class="form-control"
          />
        </div>
      </div>

      <div class="text-end mt-3">
        <button
          class="btn btn-primary"
          :disabled="saving"
          @click="submitCreateCase"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          Create Case
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="page-card mb-4">
      <div class="row g-3 align-items-end">
        <div class="col-md-4">
          <label class="form-label">Filter by Status</label>
          <select v-model="statusFilter" class="form-select">
            <option value="">All Statuses</option>
            <option value="Scheduled">Scheduled</option>
            <option value="InProgress">In Progress</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <div class="col-md-3">
          <button class="btn btn-primary w-100" @click="loadCases">
            <i class="bi bi-search me-1"></i>
            Apply Filter
          </button>
        </div>

        <div class="col-md-3">
          <button
            class="btn btn-outline-secondary w-100"
            @click="statusFilter = ''; loadCases()"
          >
            Clear
          </button>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="loading" />

    <!-- Cases table -->
    <div v-else class="page-card">
      <EmptyState
        v-if="cases.length === 0"
        title="No cases found"
        message="No surgical cases match the selected filter."
        icon="bi-hospital"
      />

      <div v-else class="table-responsive">
        <table class="table table-hover align-middle">
          <thead>
            <tr>
              <th>Case</th>
              <th>Patient</th>
              <th>Surgeon</th>
              <th>Room</th>
              <th>Surgery</th>
              <th>Scheduled</th>
              <th>Actual</th>
              <th>Status</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="item in cases" :key="item.surgeryId">
              <td>#{{ item.surgeryId }}</td>

              <td>
                <div>{{ item.patientName }}</div>
                <small class="text-muted">{{ item.patientMrn }}</small>
              </td>

              <td>{{ item.surgeonName }}</td>
              <td>{{ item.roomName }}</td>
              <td>{{ item.surgeryType }}</td>

              <td>
                <div>{{ formatDateTime(item.scheduledStart) }}</div>
                <small class="text-muted">
                  to {{ formatDateTime(item.scheduledEnd) }}
                </small>
              </td>

              <td>
                <div>{{ formatDateTime(item.actualStart) }}</div>
                <small class="text-muted">
                  to {{ formatDateTime(item.actualEnd) }}
                </small>
              </td>

              <td>
                <StatusBadge :status="item.caseStatus" />
              </td>

              <td class="text-end">
                <button
                  class="btn btn-sm btn-outline-primary me-2"
                  :disabled="!canEditCase(item)"
                  @click="openUpdateCase(item)"
                >
                  Edit
                </button>

                <button
                  class="btn btn-sm btn-outline-success"
                  :disabled="!canChangeStatus(item)"
                  @click="openStatusCase(item)"
                >
                  Status
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Update schedule card -->
    <AppModal
  :show="!!selectedCase"
  :title="selectedCase ? `Update Case #${selectedCase.surgeryId}` : 'Update Case'"
  size="lg"
  @close="selectedCase = null"
>
  <div class="row g-3">
    <div class="col-md-6">
      <label class="form-label">Scheduled Start</label>
      <input
        v-model="updateForm.scheduledStart"
        type="datetime-local"
        class="form-control"
      />
    </div>

    <div class="col-md-6">
      <label class="form-label">Scheduled End</label>
      <input
        v-model="updateForm.scheduledEnd"
        type="datetime-local"
        class="form-control"
      />
    </div>
  </div>

  <template #footer>
    <button class="btn btn-outline-secondary" @click="selectedCase = null">
      Cancel
    </button>

    <button
      class="btn btn-primary"
      :disabled="saving"
      @click="submitUpdateCase"
    >
      <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
      Save Schedule
    </button>
  </template>
</AppModal>

    <!-- Update status card -->
    <AppModal
  :show="!!selectedStatusCase"
  :title="selectedStatusCase ? `Update Status — Case #${selectedStatusCase.surgeryId}` : 'Update Status'"
  size="lg"
  @close="selectedStatusCase = null"
>
  <div class="row g-3">
    <div class="col-md-4">
      <label class="form-label">New Status</label>
      <select v-model="statusForm.status" class="form-select">
        <option value="InProgress">In Progress</option>
        <option value="Completed">Completed</option>
        <option value="Cancelled">Cancelled</option>
      </select>
    </div>

    <div v-if="statusForm.status === 'InProgress'" class="col-md-4">
      <label class="form-label">Actual Start</label>
      <input
        v-model="statusForm.actualStart"
        type="datetime-local"
        class="form-control"
      />
    </div>

    <div v-if="statusForm.status === 'Completed'" class="col-md-4">
      <label class="form-label">Actual End</label>
      <input
        v-model="statusForm.actualEnd"
        type="datetime-local"
        class="form-control"
      />
    </div>

    <div v-if="statusForm.status === 'Cancelled'" class="col-md-6">
      <label class="form-label">Cancellation Reason</label>
      <select v-model="statusForm.cancellationReason" class="form-select">
        <option value="">Select reason</option>
        <option value="SurgeonCancelled">Surgeon Cancelled</option>
        <option value="PatientNoShow">Patient No Show</option>
        <option value="PatientNotCleared">Patient Not Cleared</option>
        <option value="EmergencyBump">Emergency Bump</option>
        <option value="Other">Other</option>
      </select>
    </div>
  </div>

  <template #footer>
    <button class="btn btn-outline-secondary" @click="selectedStatusCase = null">
      Cancel
    </button>

    <button
      class="btn btn-success"
      :disabled="saving"
      @click="submitStatusUpdate"
    >
      <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
      Update Status
    </button>
  </template>
</AppModal>
  </div>
</template>
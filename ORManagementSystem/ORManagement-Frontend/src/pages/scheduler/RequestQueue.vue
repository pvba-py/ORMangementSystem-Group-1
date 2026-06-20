<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getRequests,
  updateRequestStatus,
  getRequestScore
} from '../../services/requestService'
import { formatDate, formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)
const requests = ref([])
const statusFilter = ref('')
const selectedRequest = ref(null)
const scoreResult = ref(null)

const statusForm = ref({
  status: 'Approved',
  schedulerRemarks: ''
})

const loadRequests = async () => {
  loading.value = true

  try {
    const params = {}

    if (statusFilter.value) {
      params.status = statusFilter.value
    }

    const response = await getRequests(params)
    requests.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load requests.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const openStatusUpdate = request => {
  selectedRequest.value = request
  scoreResult.value = null

  statusForm.value = {
    status: request.requestStatus === 'PendingReview'
      ? 'Approved'
      : request.requestStatus,
    schedulerRemarks: request.schedulerRemarks || ''
  }
}

const closePanel = () => {
  selectedRequest.value = null
  scoreResult.value = null
}

const submitStatusUpdate = async () => {
  if (!selectedRequest.value) return

  if (!statusForm.value.status) {
    showToast('Please select a status.', 'warning')
    return
  }

  saving.value = true

  try {
    await updateRequestStatus(selectedRequest.value.requestId, {
      status: statusForm.value.status,
      schedulerRemarks: statusForm.value.schedulerRemarks
    })

    showToast('Request status updated successfully.', 'success')
    closePanel()
    await loadRequests()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update request status.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const loadScore = async request => {
  try {
    const response = await getRequestScore(request.requestId)
    scoreResult.value = response.data
    selectedRequest.value = request
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load request score.'

    showToast(message, 'error')
  }
}

onMounted(loadRequests)
</script>

<template>
  <div>
    <PageHeader
      title="Request Queue"
      subtitle="Review surgeon OR requests and update scheduling decisions"
      icon="bi-inbox"
    />

    <div class="page-card mb-4">
      <div class="row g-3 align-items-end">
        <div class="col-md-4">
          <label class="form-label">Filter by Status</label>
          <select v-model="statusFilter" class="form-select">
            <option value="">All Statuses</option>
            <option value="PendingReview">Pending Review</option>
            <option value="Approved">Approved</option>
            <option value="Modified">Modified</option>
            <option value="Waitlisted">Waitlisted</option>
            <option value="Rejected">Rejected</option>
            <option value="Scheduled">Scheduled</option>
          </select>
        </div>

        <div class="col-md-3">
          <button class="btn btn-primary w-100" @click="loadRequests">
            <i class="bi bi-search me-1"></i>
            Apply Filter
          </button>
        </div>

        <div class="col-md-3">
          <button
            class="btn btn-outline-secondary w-100"
            @click="statusFilter = ''; loadRequests()"
          >
            Clear
          </button>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="loading" />

    <div v-else class="page-card">
      <EmptyState
        v-if="requests.length === 0"
        title="No requests found"
        message="There are no OR requests for the selected filter."
        icon="bi-inbox"
      />

      <div v-else class="table-responsive">
        <table class="table table-hover align-middle">
          <thead>
            <tr>
              <th>ID</th>
              <th>Surgeon</th>
              <th>Patient</th>
              <th>Surgery</th>
              <th>Preferred</th>
              <th>Duration</th>
              <th>Priority</th>
              <th>Readiness</th>
              <th>Availability</th>
              <th>Status</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="request in requests" :key="request.requestId">
              <td>#{{ request.requestId }}</td>

              <td>{{ request.surgeonName }}</td>

              <td>
                <div>{{ request.patientName }}</div>
                <small class="text-muted">{{ request.patientMrn }}</small>
              </td>

              <td>{{ request.surgeryType }}</td>

              <td>
                <div>{{ formatDate(request.preferredDate) }}</div>
                <small class="text-muted">{{ request.preferredQuarter }}</small>
              </td>

              <td>{{ request.estimatedDurationMin }} min</td>
              <td>{{ request.priority }}</td>
              <td>{{ request.patientReadiness }}</td>
              <td>{{ request.availableDaysDisplay }}</td>

              <td>
                <StatusBadge :status="request.requestStatus" />
              </td>

              <td class="text-end">
                <button
                  class="btn btn-sm btn-outline-info me-2"
                  @click="loadScore(request)"
                >
                  Score
                </button>

                <button
                  class="btn btn-sm btn-outline-primary"
                  @click="openStatusUpdate(request)"
                >
                  Update
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div v-if="selectedRequest" class="page-card mt-4">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h5 class="mb-0">
          Request #{{ selectedRequest.requestId }}
        </h5>

        <button class="btn btn-sm btn-outline-secondary" @click="closePanel">
          Close
        </button>
      </div>

      <div class="row g-4">
        <div class="col-lg-7">
          <h6 class="mb-3">Update Status</h6>

          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Status</label>
              <select v-model="statusForm.status" class="form-select">
                <option value="Approved">Approved</option>
                <option value="Modified">Modified</option>
                <option value="Waitlisted">Waitlisted</option>
                <option value="Rejected">Rejected</option>
                <option value="Scheduled">Scheduled</option>
              </select>
            </div>

            <div class="col-md-8">
              <label class="form-label">Scheduler Remarks</label>
              <input
                v-model="statusForm.schedulerRemarks"
                class="form-control"
                placeholder="Remarks visible to surgeon"
              />
            </div>
          </div>

          <div class="text-end mt-3">
            <button
              class="btn btn-primary"
              :disabled="saving"
              @click="submitStatusUpdate"
            >
              <span
                v-if="saving"
                class="spinner-border spinner-border-sm me-2"
              ></span>
              Save Status
            </button>
          </div>
        </div>

        <div class="col-lg-5">
          <h6 class="mb-3">Request Score</h6>

          <div v-if="scoreResult" class="score-box">
            <div class="d-flex justify-content-between mb-2">
              <span>Rank Score</span>
              <strong>{{ scoreResult.rankScore ?? scoreResult.score ?? '-' }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Priority</span>
              <strong>{{ selectedRequest.priority }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Cycles Waited</span>
              <strong>{{ selectedRequest.cyclesWaited }}</strong>
            </div>

            <div class="d-flex justify-content-between">
              <span>Availability</span>
              <strong>{{ selectedRequest.availableDaysDisplay }}</strong>
            </div>
          </div>

          <div v-else class="text-muted small">
            Click <strong>Score</strong> on a request to view score details.
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.score-box {
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 16px;
  background: #f9fafb;
}
</style>
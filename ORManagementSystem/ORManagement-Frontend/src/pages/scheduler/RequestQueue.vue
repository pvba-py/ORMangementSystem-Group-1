<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getRequests,
  updateRequestStatus,
  getRequestScore
} from '../../services/requestService'
import { formatDate } from '../../utils/formatters'
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

const getScoreValue = score => {
  return (
    score?.totalScore ??
    score?.rankScore ??
    score?.score ??
    score?.priorityScore ??
    null
  )
}

const formatScore = value => {
  if (value === null || value === undefined) {
    return '-'
  }

  return Number(value).toFixed(2)
}

const loadRequests = async () => {
  loading.value = true

  try {
    const params = {}

    if (statusFilter.value) {
      params.status = statusFilter.value
    }

    const response = await getRequests(params)
    const requestList = response.data || []

    const scoredRequests = await Promise.all(
      requestList.map(async request => {
        try {
          const scoreResponse = await getRequestScore(request.requestId)
          const score = scoreResponse.data
          const totalScore = getScoreValue(score)

          return {
            ...request,
            requestScore: totalScore,
            scoreBreakdown: score
          }
        } catch {
          return {
            ...request,
            requestScore: null,
            scoreBreakdown: null
          }
        }
      })
    )

    requests.value = scoredRequests.sort((a, b) => {
      const scoreA = a.requestScore ?? -1
      const scoreB = b.requestScore ?? -1

      return scoreB - scoreA
    })
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

const openReview = async request => {
  selectedRequest.value = request

  scoreResult.value = request.scoreBreakdown || null

  statusForm.value = {
    status: request.requestStatus === 'PendingReview'
      ? 'Approved'
      : request.requestStatus,
    schedulerRemarks: request.schedulerRemarks || ''
  }

  if (scoreResult.value) {
    return
  }

  try {
    const response = await getRequestScore(request.requestId)
    scoreResult.value = response.data
  } catch {
    scoreResult.value = null
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
  <strong v-if="request.requestScore !== null && request.requestScore !== undefined">
    {{ formatScore(request.requestScore) }}
  </strong>
  <span v-else class="text-muted">-</span>
</td>

              <td>
                <StatusBadge :status="request.requestStatus" />
              </td>

              <td class="text-end">
                <button
                  class="btn btn-sm btn-outline-primary"
                  @click="openReview(request)"
                >
                  Review
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <AppModal
      :show="!!selectedRequest"
      :title="selectedRequest ? `Request #${selectedRequest.requestId}` : 'Request'"
      size="xl"
      @close="closePanel"
    >
      <div v-if="selectedRequest" class="row g-4">
        <div class="col-lg-7">
          <h6 class="mb-3">Scheduler Decision</h6>

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

          <div class="mt-4">
            <h6 class="mb-3">Request Summary</h6>

            <div class="summary-box">
              <div class="d-flex justify-content-between mb-2">
                <span>Surgeon</span>
                <strong>{{ selectedRequest.surgeonName }}</strong>
              </div>

              <div class="d-flex justify-content-between mb-2">
                <span>Patient</span>
                <strong>{{ selectedRequest.patientName }}</strong>
              </div>

              <div class="d-flex justify-content-between mb-2">
                <span>Surgery</span>
                <strong>{{ selectedRequest.surgeryType }}</strong>
              </div>

              <div class="d-flex justify-content-between mb-2">
                <span>Preferred</span>
                <strong>
                  {{ formatDate(selectedRequest.preferredDate) }}
                  · {{ selectedRequest.preferredQuarter }}
                </strong>
              </div>

              <div class="d-flex justify-content-between">
                <span>Duration</span>
                <strong>{{ selectedRequest.estimatedDurationMin }} min</strong>
              </div>
            </div>
          </div>
        </div>

        <div class="col-lg-5">
          <h6 class="mb-3">Request Score</h6>

          <div v-if="scoreResult" class="score-box">
            <div class="d-flex justify-content-between mb-2">
              <span>Total Score</span>
              <strong>{{ formatScore(getScoreValue(scoreResult)) }} / 100</strong>
            </div>

            <hr />

            <div class="d-flex justify-content-between mb-2">
              <span>Weighted Priority</span>
              <strong>{{ formatScore(scoreResult.priorityScore) }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Weighted Waiting</span>
              <strong>{{ formatScore(scoreResult.waitingScore) }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Weighted Readiness</span>
              <strong>{{ formatScore(scoreResult.readinessScore) }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Weighted Cycle Wait</span>
              <strong>{{ formatScore(scoreResult.cycleWaitScore) }}</strong>
            </div>

            <div
              v-if="scoreResult.durationFitScore !== undefined"
              class="d-flex justify-content-between mb-2"
            >
              <span>Weighted Duration Fit</span>
              <strong>{{ formatScore(scoreResult.durationFitScore) }}</strong>
            </div>

            <hr />

            <div class="d-flex justify-content-between mb-2">
              <span>Priority</span>
              <strong>{{ selectedRequest.priority }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Patient Readiness</span>
              <strong>{{ selectedRequest.patientReadiness }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Cycles Waited</span>
              <strong>{{ selectedRequest.cyclesWaited }}</strong>
            </div>

            <div class="d-flex justify-content-between mb-2">
              <span>Availability</span>
              <strong>{{ selectedRequest.availableDaysDisplay }}</strong>
            </div>

            <div class="d-flex justify-content-between">
              <span>Starved</span>
              <strong>
                <span
                  class="badge"
                  :class="scoreResult.isStarved ? 'bg-danger' : 'bg-secondary'"
                >
                  {{ scoreResult.isStarved ? 'Yes' : 'No' }}
                </span>
              </strong>
            </div>
          </div>

          <div v-else class="text-muted small">
            Score is not available for this request.
          </div>
        </div>
      </div>

      <template #footer>
        <button
          class="btn btn-outline-secondary"
          @click="closePanel"
        >
          Cancel
        </button>

        <button
          class="btn btn-primary"
          :disabled="saving"
          @click="submitStatusUpdate"
        >
          <span
            v-if="saving"
            class="spinner-border spinner-border-sm me-2"
          ></span>
          Save Decision
        </button>
      </template>
    </AppModal>
  </div>
</template>

<style scoped>
.score-box,
.summary-box {
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 16px;
  background: #f9fafb;
}
</style>
<script setup>
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import CaseManagement from './CaseManagement.vue'
import AppModal from '../../components/common/AppModal.vue'
import {
  getRequests,
  updateRequestStatus,
  getRequestScore,
  getRequestCapacitySummary
} from '../../services/requestService'
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import { formatDate } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const capacitySummary = ref({
  schedulingHourCapacity: 100,
  allocatedHourCapacity: 0,
  remainingHourCapacity: 100,
  topRecurringDoctors: []
})

const loading = ref(false)
const saving = ref(false)

const activeApprovalTab = ref('requests')

const requests = ref([])
const statusFilter = ref('PendingReview')

const selectedRequest = ref(null)
const scoreResult = ref(null)

const statusForm = ref({
  status: 'Approved',
  schedulerRemarks: ''
})

const getScoreValue = score => {
  return (
    score?.finalPriorityScore ??
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

const getClinicalModelLabel = score => {
  if (!score) {
    return '-'
  }

  return score.clinicalScoringUsedFallback
    ? 'Fallback'
    : 'ClinicalBERT'
}

const getClinicalModelBadgeClass = score => {
  if (!score) {
    return 'bg-secondary'
  }

  return score.clinicalScoringUsedFallback
    ? 'bg-warning text-dark'
    : 'bg-info text-dark'
}

const loadCapacitySummary = async () => {
  try {
    const response = await getRequestCapacitySummary()

    capacitySummary.value = {
      schedulingHourCapacity: response.data?.schedulingHourCapacity ?? 100,
      allocatedHourCapacity: response.data?.allocatedHourCapacity ?? 0,
      remainingHourCapacity: response.data?.remainingHourCapacity ?? 100,
      topRecurringDoctors: response.data?.topRecurringDoctors || []
    }
  } catch (err) {
    console.error('Failed to load capacity summary:', err)

    capacitySummary.value = {
      schedulingHourCapacity: 100,
      allocatedHourCapacity: 0,
      remainingHourCapacity: 100,
      topRecurringDoctors: []
    }
  }
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
        } catch (err) {
          console.error(`Failed to load score for request ${request.requestId}:`, err)

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

    await Promise.all([
      loadRequests(),
      loadCapacitySummary()
    ])
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

onMounted(async () => {
  await Promise.all([
    loadRequests(),
    loadCapacitySummary()
  ])
})
</script>

<template>
  <div>
    <PageHeader
      title="Request Approval"
      subtitle="Review requests, approve scheduling decisions, and create surgical cases"
      icon="bi-inbox"
    />

    <!-- Approval Tabs -->
    <ul class="nav nav-pills mb-4">
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeApprovalTab === 'requests' }"
          @click="activeApprovalTab = 'requests'"
        >
          Requests
        </button>
      </li>

      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeApprovalTab === 'cases' }"
          @click="activeApprovalTab = 'cases'"
        >
          Cases
        </button>
      </li>
    </ul>

    <!-- Capacity Summary Cards -->
    <div class="row g-3 mb-4">
      <div class="col-md-3">
        <div class="capacity-card">
          <div class="capacity-label">Scheduling Hour Capacity</div>
          <div class="capacity-value">
            {{ formatScore(capacitySummary.schedulingHourCapacity) }} hrs
          </div>
        </div>
      </div>

      <div class="col-md-3">
        <div class="capacity-card">
          <div class="capacity-label">Allocated Open Hour Capacity</div>
          <div class="capacity-value text-primary">
            {{ formatScore(capacitySummary.allocatedHourCapacity) }} hrs
          </div>
        </div>
      </div>

      <div class="col-md-3">
        <div class="capacity-card">
          <div class="capacity-label">Remaining Hour Capacity</div>
          <div
            class="capacity-value"
            :class="capacitySummary.remainingHourCapacity <= 10 ? 'text-danger' : 'text-success'"
          >
            {{ formatScore(capacitySummary.remainingHourCapacity) }} hrs
          </div>
        </div>
      </div>

      <div class="col-md-3">
        <div class="capacity-card">
          <div class="capacity-label">Recurring Doctors</div>

          <div v-if="capacitySummary.topRecurringDoctors.length > 0">
            <div
              v-for="doctor in capacitySummary.topRecurringDoctors"
              :key="doctor.surgeonId"
              class="small d-flex justify-content-between gap-2"
            >
              <span class="text-truncate">{{ doctor.surgeonName }}</span>
              <strong class="text-nowrap">
                {{ formatScore(doctor.recurringHours) }} hrs
              </strong>
            </div>
          </div>

          <div v-else class="text-muted small">
            No recurring capacity yet
          </div>
        </div>
      </div>
    </div>

    <!-- Requests Tab -->
    <div v-if="activeApprovalTab === 'requests'">
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
                <th>Duration</th>
                <th>Priority</th>
                <th>Readiness</th>
                <th>Final Score</th>
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

                <td>{{ request.estimatedDurationMin }} min</td>
                <td>{{ request.priority }}</td>
                <td>{{ request.patientReadiness }}</td>

                <td>
                  <div
                    v-if="request.requestScore !== null && request.requestScore !== undefined"
                    class="score-cell"
                  >
                    <strong>{{ formatScore(request.requestScore) }}</strong>

                    <span
                      v-if="request.scoreBreakdown"
                      class="badge ms-1"
                      :class="getClinicalModelBadgeClass(request.scoreBreakdown)"
                    >
                      {{ getClinicalModelLabel(request.scoreBreakdown) }}
                    </span>
                  </div>

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
                  <span>Duration</span>
                  <strong>{{ selectedRequest.estimatedDurationMin }} min</strong>
                </div>

                <div class="d-flex justify-content-between mb-2">
                  <span>Priority</span>
                  <strong>{{ selectedRequest.priority }}</strong>
                </div>

                <div class="d-flex justify-content-between mb-2">
                  <span>Readiness</span>
                  <strong>{{ selectedRequest.patientReadiness }}</strong>
                </div>

                
              </div>
            </div>
          </div>

          <div class="col-lg-5">
            <h6 class="mb-3">Request Score</h6>

            <div v-if="scoreResult" class="score-box">
              <div class="score-highlight mb-3">
                <div>
                  <div class="text-muted small">Final Hybrid Priority Score</div>
                  <div class="final-score">
                    {{ formatScore(scoreResult.finalPriorityScore ?? scoreResult.totalScore) }}
                    <span class="final-score-unit">/ 100</span>
                  </div>
                </div>

                <span
                  class="badge"
                  :class="getClinicalModelBadgeClass(scoreResult)"
                >
                  {{ getClinicalModelLabel(scoreResult) }}
                </span>
              </div>

              <div class="score-formula mb-3">
                Final Score = 0.7 × Rule Score + 0.3 × Clinical Text Score
              </div>

              <div class="d-flex justify-content-between mb-2">
                <span>Rule-Based Score</span>
                <strong>{{ formatScore(scoreResult.ruleBasedScore) }}</strong>
              </div>

              <div class="d-flex justify-content-between mb-2">
                <span>Clinical Text Score</span>
                <strong>{{ formatScore(scoreResult.clinicalTextScore) }}</strong>
              </div>

              <div class="d-flex justify-content-between mb-2">
                <span>Clinical Model</span>
                <strong>{{ scoreResult.clinicalScoringModel || '-' }}</strong>
              </div>

              <hr />

              <h6 class="mb-2">Rule Score Breakdown</h6>

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

              <h6 class="mb-2">Clinical Text Explanation</h6>

              <div class="clinical-explanation">
                {{ scoreResult.clinicalTextExplanation || 'No clinical text explanation available.' }}
              </div>

              <hr />

              <div class="d-flex justify-content-between mb-2">
                <span>Cycles Waited</span>
                <strong>{{ selectedRequest.cyclesWaited }}</strong>
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

    <!-- Cases Tab -->
    <div v-if="activeApprovalTab === 'cases'">
      <CaseManagement embedded />
    </div>
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

.score-cell {
  display: flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

.score-highlight {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  border: 1px solid #cbd5e1;
  border-radius: 12px;
  background: #ffffff;
  padding: 14px;
}

.final-score {
  font-size: 30px;
  font-weight: 800;
  line-height: 1;
  color: #0f172a;
}

.final-score-unit {
  font-size: 14px;
  font-weight: 600;
  color: #64748b;
}

.score-formula {
  font-size: 0.8rem;
  color: #475569;
  background: #eef5ff;
  border: 1px solid #bfdbfe;
  border-radius: 10px;
  padding: 8px 10px;
}

.clinical-explanation {
  font-size: 0.84rem;
  line-height: 1.45;
  color: #334155;
  background: #ffffff;
  border: 1px solid #dbe3ef;
  border-radius: 10px;
  padding: 10px;
}

.capacity-card {
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  padding: 16px;
  background: #ffffff;
  min-height: 100px;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}

.capacity-label {
  font-size: 12px;
  color: #6b7280;
  margin-bottom: 8px;
  font-weight: 600;
}

.capacity-value {
  font-size: 22px;
  font-weight: 700;
  color: #111827;
}
</style>


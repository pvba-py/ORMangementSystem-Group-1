<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getCurrentCycle,
  getRankedRequests,
  cutoffCycle,
  publishCycle
} from '../../services/cycleService'
import { formatDate, formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const actionLoading = ref(false)
const currentCycle = ref(null)
const rankedRequests = ref([])

const loadCycle = async () => {
  loading.value = true

  try {
    const response = await getCurrentCycle()
    currentCycle.value = response.data

    if (currentCycle.value?.cycleId) {
      await loadRankedRequests()
    }
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load current cycle.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const loadRankedRequests = async () => {
  if (!currentCycle.value?.cycleId) return

  try {
    const response = await getRankedRequests(currentCycle.value.cycleId)
    rankedRequests.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load ranked requests.'

    showToast(message, 'error')
  }
}

const handleCutoff = async () => {
  if (!currentCycle.value?.cycleId) return

  if (!confirm(`Cutoff cycle #${currentCycle.value.cycleId}?`)) {
    return
  }

  actionLoading.value = true

  try {
    await cutoffCycle(currentCycle.value.cycleId)
    showToast('Cycle cutoff completed successfully.', 'success')
    await loadCycle()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to cutoff cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
  }
}

const handlePublish = async () => {
  if (!currentCycle.value?.cycleId) return

  if (!confirm(`Publish cycle #${currentCycle.value.cycleId}?`)) {
    return
  }

  actionLoading.value = true

  try {
    await publishCycle(currentCycle.value.cycleId)
    showToast('Cycle published successfully.', 'success')
    await loadCycle()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to publish cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
  }
}

onMounted(loadCycle)
</script>

<template>
  <div>
    <PageHeader
      title="Cycle Management"
      subtitle="Manage the current weekly scheduling cycle and ranked request queue"
      icon="bi-arrow-repeat"
    >
      <template #actions>
        <button
          class="btn btn-outline-primary me-2"
          :disabled="loading || actionLoading"
          @click="loadCycle"
        >
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <div v-if="currentCycle" class="page-card mb-4">
        <div class="row g-3 align-items-center">
          <div class="col-md-3">
            <small class="text-muted">Cycle ID</small>
            <h5 class="mb-0">#{{ currentCycle.cycleId }}</h5>
          </div>

          <div class="col-md-3">
            <small class="text-muted">Week</small>
            <h6 class="mb-0">
              {{ formatDate(currentCycle.weekStartDate) }}
              -
              {{ formatDate(currentCycle.weekEndDate) }}
            </h6>
          </div>

          <div class="col-md-3">
            <small class="text-muted">Cutoff At</small>
            <h6 class="mb-0">
              {{ formatDateTime(currentCycle.cutoffAt) }}
            </h6>
          </div>

          <div class="col-md-3">
            <small class="text-muted d-block">Status</small>
            <StatusBadge :status="currentCycle.cycleStatus" />
          </div>
        </div>

        <hr />

        <div class="d-flex justify-content-end gap-2">
          <button
            class="btn btn-warning"
            :disabled="actionLoading || currentCycle.cycleStatus !== 'Open'"
            @click="handleCutoff"
          >
            <span
              v-if="actionLoading"
              class="spinner-border spinner-border-sm me-2"
            ></span>
            Cutoff Cycle
          </button>

          <button
            class="btn btn-success"
            :disabled="
              actionLoading ||
              !['Cutoff', 'Scheduling'].includes(currentCycle.cycleStatus)
            "
            @click="handlePublish"
          >
            <span
              v-if="actionLoading"
              class="spinner-border spinner-border-sm me-2"
            ></span>
            Publish Schedule
          </button>
        </div>
      </div>

      <EmptyState
        v-else
        title="No current cycle"
        message="No open scheduling cycle was found."
        icon="bi-calendar-x"
      />

      <div class="page-card">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="mb-0">
            <i class="bi bi-list-ol me-2 text-primary"></i>
            Ranked Requests
          </h5>

          <button
            class="btn btn-sm btn-outline-primary"
            :disabled="!currentCycle"
            @click="loadRankedRequests"
          >
            Refresh Queue
          </button>
        </div>

        <EmptyState
          v-if="rankedRequests.length === 0"
          title="No ranked requests"
          message="There are no pending or waitlisted requests in this cycle."
          icon="bi-inbox"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Rank</th>
                <th>Request</th>
                <th>Surgeon</th>
                <th>Patient</th>
                <th>Surgery</th>
                <th>Priority</th>
                <th>Readiness</th>
                <th>Duration</th>
                <th>Availability</th>
                <th>Waited</th>
                <th>Score</th>
                <th>Starved</th>
              </tr>
            </thead>

            <tbody>
              <tr
                v-for="(request, index) in rankedRequests"
                :key="request.requestId"
              >
                <td>
                  <span class="badge bg-primary">
                    #{{ index + 1 }}
                  </span>
                </td>

                <td>#{{ request.requestId }}</td>
                <td>#{{ request.surgeonId }}</td>
                <td>#{{ request.patientId }}</td>
                <td>{{ request.surgeryType }}</td>
                <td>{{ request.priority }}</td>
                <td>{{ request.patientReadiness }}</td>
                <td>{{ request.estimatedDurationMin }} min</td>
                <td>{{ request.availableDaysDisplay }}</td>
                <td>
                  {{ request.cyclesWaited }} cycles
                  <br />
                  <small class="text-muted">
                    {{ request.waitingDays }} days
                  </small>
                </td>
                <td>
                  <strong>{{ Number(request.rankScore).toFixed(2) }}</strong>
                </td>
                <td>
                  <span
                    v-if="request.isStarved"
                    class="badge bg-danger"
                  >
                    Yes
                  </span>
                  <span
                    v-else
                    class="badge bg-secondary"
                  >
                    No
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
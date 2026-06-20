<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getForecastDemand,
  getForecastRecommendations,
  generateForecastRecommendations,
  updateForecastStatus
} from '../../services/forecastService'
import { formatDateTime, formatPercent } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)

const activeTab = ref('recommendations')
const demand = ref([])
const recommendations = ref([])
const statusFilter = ref('')

const selectedRecommendation = ref(null)

const statusForm = ref({
  status: 'Approved',
  schedulerRemarks: ''
})

const loadDemand = async () => {
  const response = await getForecastDemand()
  demand.value = response.data || []
}

const loadRecommendations = async () => {
  const params = {}

  if (statusFilter.value) {
    params.status = statusFilter.value
  }

  const response = await getForecastRecommendations(params)
  recommendations.value = response.data || []
}

const loadPage = async () => {
  loading.value = true

  try {
    await Promise.all([
      loadDemand(),
      loadRecommendations()
    ])
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load forecast data.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const generateRecommendations = async () => {
  saving.value = true

  try {
    const response = await generateForecastRecommendations()
    showToast(`Generated ${response.data.generatedCount} recommendations.`, 'success')
    await loadRecommendations()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to generate recommendations.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const openStatusPanel = recommendation => {
  selectedRecommendation.value = recommendation

  statusForm.value = {
    status: recommendation.recStatus || 'Approved',
    schedulerRemarks: ''
  }
}

const submitStatusUpdate = async () => {
  if (!selectedRecommendation.value) return

  saving.value = true

  try {
    await updateForecastStatus(selectedRecommendation.value.recId, {
      status: statusForm.value.status,
      schedulerRemarks: statusForm.value.schedulerRemarks
    })

    showToast('Forecast recommendation updated successfully.', 'success')
    selectedRecommendation.value = null
    await loadRecommendations()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update forecast recommendation.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const formatEvidence = value => {
  if (!value) return '-'

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Forecast Recommendations"
      subtitle="Review demand signals and rule-based block allocation recommendations"
      icon="bi-graph-up-arrow"
    >
      <template #actions>
        <button
          class="btn btn-primary"
          :disabled="saving"
          @click="generateRecommendations"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          Generate Recommendations
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'recommendations' }"
            @click="activeTab = 'recommendations'"
          >
            Recommendations
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'demand' }"
            @click="activeTab = 'demand'"
          >
            Demand Signals
          </button>
        </li>
      </ul>

      <div v-if="activeTab === 'recommendations'">
        <div class="page-card mb-4">
          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <label class="form-label">Status</label>
              <select v-model="statusFilter" class="form-select">
                <option value="">All</option>
                <option value="Pending">Pending</option>
                <option value="Approved">Approved</option>
                <option value="Rejected">Rejected</option>
                <option value="Modified">Modified</option>
              </select>
            </div>

            <div class="col-md-3">
              <button class="btn btn-primary w-100" @click="loadRecommendations">
                Apply Filter
              </button>
            </div>

            <div class="col-md-3">
              <button
                class="btn btn-outline-secondary w-100"
                @click="statusFilter = ''; loadRecommendations()"
              >
                Clear
              </button>
            </div>
          </div>
        </div>

        <div class="page-card">
          <EmptyState
            v-if="recommendations.length === 0"
            title="No recommendations"
            message="No forecast recommendations were found."
            icon="bi-graph-up"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Rec</th>
                  <th>Rule</th>
                  <th>Description</th>
                  <th>Evidence</th>
                  <th>Status</th>
                  <th>Reviewed By</th>
                  <th>Created</th>
                  <th class="text-end">Actions</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in recommendations" :key="item.recId">
                  <td>#{{ item.recId }}</td>
                  <td>{{ item.ruleId }}</td>
                  <td>{{ item.description }}</td>
                  <td>
                    <pre class="evidence-box">{{ formatEvidence(item.evidenceJson) }}</pre>
                  </td>
                  <td>
                    <StatusBadge :status="item.recStatus" />
                  </td>
                  <td>{{ item.reviewedBy || '-' }}</td>
                  <td>{{ formatDateTime(item.createdAt) }}</td>
                  <td class="text-end">
                    <button class="btn btn-sm btn-outline-primary" @click="openStatusPanel(item)">
                      Review
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-if="selectedRecommendation" class="page-card mt-4">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="mb-0">Review Recommendation #{{ selectedRecommendation.recId }}</h5>
            <button class="btn btn-sm btn-outline-secondary" @click="selectedRecommendation = null">
              Close
            </button>
          </div>

          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Status</label>
              <select v-model="statusForm.status" class="form-select">
                <option value="Approved">Approved</option>
                <option value="Rejected">Rejected</option>
                <option value="Modified">Modified</option>
                <option value="Pending">Pending</option>
              </select>
            </div>

            <div class="col-md-8">
              <label class="form-label">Scheduler Remarks</label>
              <input v-model="statusForm.schedulerRemarks" class="form-control" />
            </div>
          </div>

          <div class="text-end mt-3">
            <button class="btn btn-primary" :disabled="saving" @click="submitStatusUpdate">
              Save Review
            </button>
          </div>
        </div>
      </div>

      <div v-if="activeTab === 'demand'" class="page-card">
        <EmptyState
          v-if="demand.length === 0"
          title="No demand signals"
          message="No demand signals are available."
          icon="bi-activity"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Specialty</th>
                <th>Total Blocks</th>
                <th>Waitlisted Requests</th>
                <th>Average Utilization</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="item in demand" :key="item.specialty">
                <td>{{ item.specialty }}</td>
                <td>{{ item.totalBlocks }}</td>
                <td>{{ item.waitlistedRequests }}</td>
                <td>{{ formatPercent(item.averageUtilizationPercent) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.evidence-box {
  max-width: 300px;
  max-height: 120px;
  overflow: auto;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 8px;
  font-size: 12px;
  margin: 0;
}
</style>
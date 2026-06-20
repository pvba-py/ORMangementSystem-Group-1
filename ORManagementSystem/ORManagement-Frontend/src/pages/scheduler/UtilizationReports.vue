<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import KpiCard from '../../components/common/KpiCard.vue'
import {
  getUtilization,
  getUtilizationSummary,
  getUnderutilizedBlocks,
  calculateUtilization
} from '../../services/utilizationService'
import { formatDate, formatDateTime, formatPercent } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const calculating = ref(false)

const activeTab = ref('records')

const records = ref([])
const summary = ref(null)
const underutilized = ref([])

const filters = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-26',
  surgeonId: '',
  roomId: '',
  status: ''
})

const calculateForm = ref({
  blockId: '',
  fromDate: '2026-06-22',
  toDate: '2026-06-26'
})

const loadSummary = async () => {
  const response = await getUtilizationSummary({
    fromDate: filters.value.fromDate,
    toDate: filters.value.toDate
  })

  summary.value = response.data
}

const loadRecords = async () => {
  const params = {}

  if (filters.value.fromDate) params.fromDate = filters.value.fromDate
  if (filters.value.toDate) params.toDate = filters.value.toDate
  if (filters.value.surgeonId) params.surgeonId = filters.value.surgeonId
  if (filters.value.roomId) params.roomId = filters.value.roomId
  if (filters.value.status) params.status = filters.value.status

  const response = await getUtilization(params)
  records.value = response.data || []
}

const loadUnderutilized = async () => {
  const response = await getUnderutilizedBlocks({
    fromDate: filters.value.fromDate,
    toDate: filters.value.toDate
  })

  underutilized.value = response.data || []
}

const loadPage = async () => {
  loading.value = true

  try {
    await Promise.all([
      loadSummary(),
      loadRecords(),
      loadUnderutilized()
    ])
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load utilization data.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const submitCalculate = async () => {
  calculating.value = true

  try {
    const payload = {}

    if (calculateForm.value.blockId) {
      payload.blockId = Number(calculateForm.value.blockId)
    } else {
      payload.fromDate = calculateForm.value.fromDate
      payload.toDate = calculateForm.value.toDate
    }

    const response = await calculateUtilization(payload)

    showToast(
      `Utilization calculated. Count: ${response.data.calculatedCount}`,
      'success'
    )

    await loadPage()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to calculate utilization.'

    showToast(message, 'error')
  } finally {
    calculating.value = false
  }
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Utilization Reports"
      subtitle="Analyze OR block usage and identify underutilized capacity"
      icon="bi-bar-chart"
    >
      <template #actions>
        <button class="btn btn-outline-primary" @click="loadPage">
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <div class="row g-3 mb-4" v-if="summary">
        <div class="col-md-3">
          <KpiCard
            label="Total Blocks"
            :value="summary.totalBlocks"
            icon="bi-grid-3x3-gap"
            color="primary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Allocated Minutes"
            :value="summary.totalAllocatedMinutes"
            icon="bi-clock"
            color="info"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Used Minutes"
            :value="summary.totalUsedMinutes"
            icon="bi-stopwatch"
            color="success"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Average Utilization"
            :value="formatPercent(summary.averageUtilizationPercent)"
            icon="bi-percent"
            color="warning"
          />
        </div>
      </div>

      <div class="page-card mb-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-2">
            <label class="form-label">From Date</label>
            <input v-model="filters.fromDate" type="date" class="form-control" />
          </div>

          <div class="col-md-2">
            <label class="form-label">To Date</label>
            <input v-model="filters.toDate" type="date" class="form-control" />
          </div>

          <div class="col-md-2">
            <label class="form-label">Status</label>
            <select v-model="filters.status" class="form-select">
              <option value="">All</option>
              <option value="Good">Good</option>
              <option value="Moderate">Moderate</option>
              <option value="Underutilized">Underutilized</option>
              <option value="Unused">Unused</option>
            </select>
          </div>

          <div class="col-md-2">
            <label class="form-label">Surgeon ID</label>
            <input v-model="filters.surgeonId" type="number" class="form-control" />
          </div>

          <div class="col-md-2">
            <label class="form-label">Room ID</label>
            <input v-model="filters.roomId" type="number" class="form-control" />
          </div>

          <div class="col-md-2">
            <button class="btn btn-primary w-100" @click="loadPage">
              Apply
            </button>
          </div>
        </div>
      </div>

      <div class="page-card mb-4">
        <h5 class="mb-3">
          <i class="bi bi-calculator me-2 text-primary"></i>
          Calculate Utilization
        </h5>

        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label">Block ID</label>
            <input
              v-model="calculateForm.blockId"
              type="number"
              class="form-control"
              placeholder="Optional"
            />
          </div>

          <div class="col-md-3">
            <label class="form-label">From Date</label>
            <input
              v-model="calculateForm.fromDate"
              type="date"
              class="form-control"
              :disabled="!!calculateForm.blockId"
            />
          </div>

          <div class="col-md-3">
            <label class="form-label">To Date</label>
            <input
              v-model="calculateForm.toDate"
              type="date"
              class="form-control"
              :disabled="!!calculateForm.blockId"
            />
          </div>

          <div class="col-md-3">
            <button
              class="btn btn-success w-100"
              :disabled="calculating"
              @click="submitCalculate"
            >
              <span v-if="calculating" class="spinner-border spinner-border-sm me-2"></span>
              Calculate
            </button>
          </div>
        </div>
      </div>

      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'records' }"
            @click="activeTab = 'records'"
          >
            Records
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'underutilized' }"
            @click="activeTab = 'underutilized'"
          >
            Underutilized
          </button>
        </li>
      </ul>

      <div v-if="activeTab === 'records'" class="page-card">
        <EmptyState
          v-if="records.length === 0"
          title="No utilization records"
          message="No utilization records were found."
          icon="bi-bar-chart"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>ID</th>
                <th>Block</th>
                <th>Surgeon</th>
                <th>Room</th>
                <th>Date</th>
                <th>Allocated</th>
                <th>Used</th>
                <th>Utilization</th>
                <th>Status</th>
                <th>Calculated</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="record in records" :key="record.utilizationId">
                <td>#{{ record.utilizationId }}</td>
                <td>#{{ record.blockId }}</td>
                <td>{{ record.surgeonName }}</td>
                <td>{{ record.roomName }}</td>
                <td>{{ formatDate(record.blockDate) }}</td>
                <td>{{ record.allocatedMinutes }} min</td>
                <td>{{ record.usedMinutes }} min</td>
                <td>{{ formatPercent(record.utilizationPercent) }}</td>
                <td>
                  <StatusBadge :status="record.utilizationStatus" />
                </td>
                <td>{{ formatDateTime(record.calculatedAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="activeTab === 'underutilized'" class="page-card">
        <EmptyState
          v-if="underutilized.length === 0"
          title="No underutilized blocks"
          message="No underutilized or unused blocks were found."
          icon="bi-check-circle"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Block</th>
                <th>Surgeon</th>
                <th>Room</th>
                <th>Date</th>
                <th>Allocated</th>
                <th>Used</th>
                <th>Utilization</th>
                <th>Status</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="item in underutilized" :key="item.blockId">
                <td>#{{ item.blockId }}</td>
                <td>{{ item.surgeonName }}</td>
                <td>{{ item.roomName }}</td>
                <td>{{ formatDate(item.blockDate) }}</td>
                <td>{{ item.allocatedMinutes }} min</td>
                <td>{{ item.usedMinutes }} min</td>
                <td>{{ formatPercent(item.utilizationPercent) }}</td>
                <td>
                  <StatusBadge :status="item.utilizationStatus" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
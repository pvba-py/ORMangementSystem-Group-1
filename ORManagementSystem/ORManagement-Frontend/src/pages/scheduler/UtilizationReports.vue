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
  calculateUtilization,
  getORRoomUtilization,
  getORRoomUtilizationSummary,
  getUnderutilizedORRooms,
  calculateORRoomWeeklyUtilization,
  generateORRoomWeeklyReport
} from '../../services/utilizationService'
import { formatDate, formatDateTime, formatPercent } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const calculatingBlock = ref(false)
const calculatingRoom = ref(false)
const generatingReport = ref(false)

const activeTab = ref('blocks')

const records = ref([])
const summary = ref(null)
const underutilized = ref([])

const roomRecords = ref([])
const roomSummary = ref(null)
const underutilizedRooms = ref([])

const weeklyReport = ref(null)

const blockFilters = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-26',
  surgeonId: '',
  roomId: '',
  status: ''
})

const roomFilters = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-22',
  roomId: '',
  status: ''
})

const calculateForm = ref({
  blockId: '',
  fromDate: '2026-06-22',
  toDate: '2026-06-26'
})

const roomCalculateForm = ref({
  weekStartDate: '2026-06-22',
  orRoomId: ''
})

const reportForm = ref({
  weekStartDate: '2026-06-22'
})

const loadBlockSummary = async () => {
  const response = await getUtilizationSummary({
    fromDate: blockFilters.value.fromDate,
    toDate: blockFilters.value.toDate
  })

  summary.value = response.data
}

const loadBlockRecords = async () => {
  const params = {}

  if (blockFilters.value.fromDate) params.fromDate = blockFilters.value.fromDate
  if (blockFilters.value.toDate) params.toDate = blockFilters.value.toDate
  if (blockFilters.value.surgeonId) params.surgeonId = blockFilters.value.surgeonId
  if (blockFilters.value.roomId) params.roomId = blockFilters.value.roomId
  if (blockFilters.value.status) params.status = blockFilters.value.status

  const response = await getUtilization(params)
  records.value = response.data || []
}

const loadUnderutilizedBlocks = async () => {
  const response = await getUnderutilizedBlocks({
    fromDate: blockFilters.value.fromDate,
    toDate: blockFilters.value.toDate
  })

  underutilized.value = response.data || []
}

const loadBlockTab = async () => {
  await Promise.all([
    loadBlockSummary(),
    loadBlockRecords(),
    loadUnderutilizedBlocks()
  ])
}

const loadRoomSummary = async () => {
  const response = await getORRoomUtilizationSummary({
    fromDate: roomFilters.value.fromDate,
    toDate: roomFilters.value.toDate
  })

  roomSummary.value = response.data
}

const loadRoomRecords = async () => {
  const params = {}

  if (roomFilters.value.fromDate) params.fromDate = roomFilters.value.fromDate
  if (roomFilters.value.toDate) params.toDate = roomFilters.value.toDate
  if (roomFilters.value.roomId) params.roomId = roomFilters.value.roomId
  if (roomFilters.value.status) params.status = roomFilters.value.status

  const response = await getORRoomUtilization(params)
  roomRecords.value = response.data || []
}

const loadUnderutilizedRooms = async () => {
  const response = await getUnderutilizedORRooms({
    fromDate: roomFilters.value.fromDate,
    toDate: roomFilters.value.toDate
  })

  underutilizedRooms.value = response.data || []
}

const loadRoomTab = async () => {
  await Promise.all([
    loadRoomSummary(),
    loadRoomRecords(),
    loadUnderutilizedRooms()
  ])
}

const loadPage = async () => {
  loading.value = true

  try {
    if (activeTab.value === 'blocks') {
      await loadBlockTab()
    }

    if (activeTab.value === 'rooms') {
      await loadRoomTab()
    }

    if (activeTab.value === 'report' && weeklyReport.value) {
      return
    }
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

const switchTab = async tab => {
  activeTab.value = tab
  await loadPage()
}

const submitCalculateBlock = async () => {
  calculatingBlock.value = true

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
      `Block utilization calculated. Count: ${response.data.calculatedCount}`,
      'success'
    )

    blockFilters.value.fromDate = calculateForm.value.fromDate
    blockFilters.value.toDate = calculateForm.value.toDate

    await loadBlockTab()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to calculate block utilization.'

    showToast(message, 'error')
  } finally {
    calculatingBlock.value = false
  }
}

const submitCalculateRoom = async () => {
  calculatingRoom.value = true

  try {
    const payload = {
      weekStartDate: roomCalculateForm.value.weekStartDate,
      orRoomId: roomCalculateForm.value.orRoomId
        ? Number(roomCalculateForm.value.orRoomId)
        : null
    }

    const response = await calculateORRoomWeeklyUtilization(payload)

    showToast(
      `Room utilization calculated. Count: ${response.data.calculatedCount}`,
      'success'
    )

    roomFilters.value.fromDate = roomCalculateForm.value.weekStartDate
    roomFilters.value.toDate = roomCalculateForm.value.weekStartDate

    await loadRoomTab()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to calculate room utilization.'

    showToast(message, 'error')
  } finally {
    calculatingRoom.value = false
  }
}

const submitGenerateReport = async () => {
  generatingReport.value = true

  try {
    const response = await generateORRoomWeeklyReport({
      weekStartDate: reportForm.value.weekStartDate
    })

    weeklyReport.value = response.data
    showToast('Overall weekly report generated successfully.', 'success')
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to generate weekly report.'

    showToast(message, 'error')
  } finally {
    generatingReport.value = false
  }
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Utilization Reports"
      subtitle="Analyze block utilization, room utilization, and overall weekly OR performance"
      icon="bi-bar-chart"
    >
      <template #actions>
        <button class="btn btn-outline-primary" @click="loadPage">
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <ul class="nav nav-pills mb-4">
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'blocks' }"
          @click="switchTab('blocks')"
        >
          Block Utilization
        </button>
      </li>

      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'rooms' }"
          @click="switchTab('rooms')"
        >
          Room Utilization
        </button>
      </li>

      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'report' }"
          @click="switchTab('report')"
        >
          Overall Weekly Report
        </button>
      </li>
    </ul>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <!-- Block Utilization Tab -->
      <div v-if="activeTab === 'blocks'">
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
          <h5 class="mb-3">Block Filters</h5>

          <div class="row g-3 align-items-end">
            <div class="col-md-2">
              <label class="form-label">From Date</label>
              <input v-model="blockFilters.fromDate" type="date" class="form-control" />
            </div>

            <div class="col-md-2">
              <label class="form-label">To Date</label>
              <input v-model="blockFilters.toDate" type="date" class="form-control" />
            </div>

            <div class="col-md-2">
              <label class="form-label">Status</label>
              <select v-model="blockFilters.status" class="form-select">
                <option value="">All</option>
                <option value="Good">Good</option>
                <option value="Moderate">Moderate</option>
                <option value="Underutilized">Underutilized</option>
                <option value="Unused">Unused</option>
              </select>
            </div>

            <div class="col-md-2">
              <label class="form-label">Surgeon ID</label>
              <input v-model="blockFilters.surgeonId" type="number" class="form-control" />
            </div>

            <div class="col-md-2">
              <label class="form-label">Room ID</label>
              <input v-model="blockFilters.roomId" type="number" class="form-control" />
            </div>

            <div class="col-md-2">
              <button class="btn btn-primary w-100" @click="loadBlockTab">
                Apply
              </button>
            </div>
          </div>
        </div>

        <div class="page-card mb-4">
          <h5 class="mb-3">
            <i class="bi bi-calculator me-2 text-primary"></i>
            Calculate Block Utilization
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
                :disabled="calculatingBlock"
                @click="submitCalculateBlock"
              >
                <span
                  v-if="calculatingBlock"
                  class="spinner-border spinner-border-sm me-2"
                ></span>
                Calculate Blocks
              </button>
            </div>
          </div>
        </div>

        <div class="page-card mb-4">
          <h5 class="mb-3">Block Utilization Records</h5>

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
                  <th>Surgeon / Capacity</th>
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

        <div class="page-card">
          <h5 class="mb-3">Underutilized Blocks</h5>

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
                  <th>Surgeon / Capacity</th>
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

      <!-- Room Utilization Tab -->
      <div v-if="activeTab === 'rooms'">
        <div class="row g-3 mb-4" v-if="roomSummary">
          <div class="col-md-3">
            <KpiCard
              label="Total Rooms"
              :value="roomSummary.totalRooms"
              icon="bi-door-open"
              color="primary"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Allocated Minutes"
              :value="roomSummary.totalAllocatedMinutes"
              icon="bi-clock-history"
              color="info"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Used Minutes"
              :value="roomSummary.totalUsedMinutes"
              icon="bi-stopwatch"
              color="success"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Average Utilization"
              :value="formatPercent(roomSummary.averageUtilizationPercent)"
              icon="bi-percent"
              color="warning"
            />
          </div>
        </div>

        <div class="page-card mb-4">
          <h5 class="mb-3">
            <i class="bi bi-building-check me-2 text-primary"></i>
            Calculate Room Weekly Utilization
          </h5>

          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <label class="form-label">Week Start Date</label>
              <input
                v-model="roomCalculateForm.weekStartDate"
                type="date"
                class="form-control"
              />
              <small class="text-muted">Must be a Monday.</small>
            </div>

            <div class="col-md-4">
              <label class="form-label">OR Room ID</label>
              <input
                v-model="roomCalculateForm.orRoomId"
                type="number"
                class="form-control"
                placeholder="Optional - all rooms"
              />
            </div>

            <div class="col-md-4">
              <button
                class="btn btn-success w-100"
                :disabled="calculatingRoom"
                @click="submitCalculateRoom"
              >
                <span
                  v-if="calculatingRoom"
                  class="spinner-border spinner-border-sm me-2"
                ></span>
                Calculate Rooms
              </button>
            </div>
          </div>
        </div>

        <div class="page-card mb-4">
          <h5 class="mb-3">Room Utilization Filters</h5>

          <div class="row g-3 align-items-end">
            <div class="col-md-3">
              <label class="form-label">From Week Start</label>
              <input v-model="roomFilters.fromDate" type="date" class="form-control" />
            </div>

            <div class="col-md-3">
              <label class="form-label">To Week Start</label>
              <input v-model="roomFilters.toDate" type="date" class="form-control" />
            </div>

            <div class="col-md-2">
              <label class="form-label">Room ID</label>
              <input v-model="roomFilters.roomId" type="number" class="form-control" />
            </div>

            <div class="col-md-2">
              <label class="form-label">Status</label>
              <select v-model="roomFilters.status" class="form-select">
                <option value="">All</option>
                <option value="Good">Good</option>
                <option value="Moderate">Moderate</option>
                <option value="Underutilized">Underutilized</option>
                <option value="Unused">Unused</option>
              </select>
            </div>

            <div class="col-md-2">
              <button class="btn btn-primary w-100" @click="loadRoomTab">
                Apply
              </button>
            </div>
          </div>
        </div>

        <div class="page-card mb-4">
          <h5 class="mb-3">Room Utilization Records</h5>

          <EmptyState
            v-if="roomRecords.length === 0"
            title="No room utilization records"
            message="No OR room utilization records were found."
            icon="bi-building"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Room</th>
                  <th>Week</th>
                  <th>Allocated</th>
                  <th>Used</th>
                  <th>Utilization</th>
                  <th>Status</th>
                  <th>Calculated</th>
                </tr>
              </thead>

              <tbody>
                <tr
                  v-for="record in roomRecords"
                  :key="record.orRoomUtilizationId"
                >
                  <td>#{{ record.orRoomUtilizationId }}</td>
                  <td>{{ record.roomName }}</td>
                  <td>
                    {{ formatDate(record.weekStartDate) }}
                    -
                    {{ formatDate(record.weekEndDate) }}
                  </td>
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

        <div class="page-card">
          <h5 class="mb-3">Underutilized Rooms</h5>

          <EmptyState
            v-if="underutilizedRooms.length === 0"
            title="No underutilized rooms"
            message="No underutilized or unused OR rooms were found."
            icon="bi-check-circle"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Room</th>
                  <th>Week</th>
                  <th>Allocated</th>
                  <th>Used</th>
                  <th>Utilization</th>
                  <th>Status</th>
                </tr>
              </thead>

              <tbody>
                <tr
                  v-for="item in underutilizedRooms"
                  :key="item.orRoomUtilizationId"
                >
                  <td>{{ item.roomName }}</td>
                  <td>
                    {{ formatDate(item.weekStartDate) }}
                    -
                    {{ formatDate(item.weekEndDate) }}
                  </td>
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

      <!-- Overall Weekly Report Tab -->
      <div v-if="activeTab === 'report'">
        <div class="page-card mb-4">
          <h5 class="mb-3">
            <i class="bi bi-file-earmark-bar-graph me-2 text-primary"></i>
            Generate Overall Weekly Report
          </h5>

          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <label class="form-label">Week Start Date</label>
              <input
                v-model="reportForm.weekStartDate"
                type="date"
                class="form-control"
              />
              <small class="text-muted">Must be a Monday.</small>
            </div>

            <div class="col-md-4">
              <button
                class="btn btn-success w-100"
                :disabled="generatingReport"
                @click="submitGenerateReport"
              >
                <span
                  v-if="generatingReport"
                  class="spinner-border spinner-border-sm me-2"
                ></span>
                Generate Weekly Report
              </button>
            </div>
          </div>
        </div>

        <EmptyState
          v-if="!weeklyReport"
          title="No report generated"
          message="Generate a weekly report to view overall room utilization performance."
          icon="bi-file-earmark-bar-graph"
        />

        <div v-else>
          <div class="row g-3 mb-4">
            <div class="col-md-3">
              <KpiCard
                label="Calculated Rooms"
                :value="weeklyReport.calculatedRooms"
                icon="bi-door-open"
                color="primary"
              />
            </div>

            <div class="col-md-3">
              <KpiCard
                label="Allocated Minutes"
                :value="weeklyReport.summary.totalAllocatedMinutes"
                icon="bi-clock-history"
                color="info"
              />
            </div>

            <div class="col-md-3">
              <KpiCard
                label="Used Minutes"
                :value="weeklyReport.summary.totalUsedMinutes"
                icon="bi-stopwatch"
                color="success"
              />
            </div>

            <div class="col-md-3">
              <KpiCard
                label="Average Utilization"
                :value="formatPercent(weeklyReport.summary.averageUtilizationPercent)"
                icon="bi-percent"
                color="warning"
              />
            </div>
          </div>

          <div class="page-card mb-4">
            <h5 class="mb-3">
              Report:
              {{ formatDate(weeklyReport.weekStartDate) }}
              -
              {{ formatDate(weeklyReport.weekEndDate) }}
            </h5>

            <p class="text-muted mb-0">
              Generated at {{ formatDateTime(weeklyReport.generatedAt) }}.
            </p>
          </div>

          <div class="page-card mb-4">
            <h5 class="mb-3">Rooms in Report</h5>

            <div class="table-responsive">
              <table class="table table-hover align-middle">
                <thead>
                  <tr>
                    <th>Room</th>
                    <th>Allocated</th>
                    <th>Used</th>
                    <th>Utilization</th>
                    <th>Status</th>
                  </tr>
                </thead>

                <tbody>
                  <tr
                    v-for="room in weeklyReport.rooms"
                    :key="room.orRoomUtilizationId"
                  >
                    <td>{{ room.roomName }}</td>
                    <td>{{ room.allocatedMinutes }} min</td>
                    <td>{{ room.usedMinutes }} min</td>
                    <td>{{ formatPercent(room.utilizationPercent) }}</td>
                    <td>
                      <StatusBadge :status="room.utilizationStatus" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="page-card">
            <h5 class="mb-3">Underutilized Rooms in Report</h5>

            <EmptyState
              v-if="weeklyReport.underutilizedRooms.length === 0"
              title="No underutilized rooms"
              message="No underutilized rooms were found in this weekly report."
              icon="bi-check-circle"
            />

            <div v-else class="table-responsive">
              <table class="table table-hover align-middle">
                <thead>
                  <tr>
                    <th>Room</th>
                    <th>Allocated</th>
                    <th>Used</th>
                    <th>Utilization</th>
                    <th>Status</th>
                  </tr>
                </thead>

                <tbody>
                  <tr
                    v-for="room in weeklyReport.underutilizedRooms"
                    :key="room.orRoomUtilizationId"
                  >
                    <td>{{ room.roomName }}</td>
                    <td>{{ room.allocatedMinutes }} min</td>
                    <td>{{ room.usedMinutes }} min</td>
                    <td>{{ formatPercent(room.utilizationPercent) }}</td>
                    <td>
                      <StatusBadge :status="room.utilizationStatus" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import KpiCard from '../../components/common/KpiCard.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import { getSchedulerDashboard } from '../../services/dashboardService'
import { formatDateTime, formatPercent } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const dashboard = ref(null)
const dashboardSource = ref('unknown')

const loadDashboard = async () => {
  loading.value = true

  try {
    const response = await getSchedulerDashboard()
    dashboard.value = response.data
    dashboardSource.value = response.headers['x-data-source'] || 'unknown'
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load scheduler dashboard.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)
</script>

<template>
  <div>
    <PageHeader
      title="Scheduler Dashboard"
      subtitle="Monitor requests, waitlist, released slots, cases, and utilization"
      icon="bi-speedometer2"
    >
      <template #actions>
        <router-link
          to="/app/scheduler/requests"
          class="btn btn-primary"
        >
          <i class="bi bi-inbox me-1"></i>
          Review Requests
        </router-link>
        <div class="d-flex justify-content-end mb-2">
          <br>
  <span
    class="badge"
    :class="dashboardSource === 'cache' ? 'bg-info text-dark' : 'bg-success'"
  >
    Source: {{ dashboardSource }}
  </span>
</div>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else-if="dashboard">
      <!-- Top KPI row -->
      <div class="row g-3 mb-4">
        <div class="col-md-3">
          <KpiCard
            label="Active Rooms"
            :value="dashboard.activeRooms"
            icon="bi-door-open"
            color="primary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Pending Requests"
            :value="dashboard.pendingRequests"
            icon="bi-hourglass-split"
            color="warning"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Waitlisted Requests"
            :value="dashboard.waitlistedRequests"
            icon="bi-list-ol"
            color="secondary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Avg Utilization"
            :value="formatPercent(dashboard.averageUtilizationPercent)"
            icon="bi-bar-chart"
            color="success"
          />
        </div>
      </div>

      <!-- Operational KPI row -->
      <div class="row g-3 mb-4">
        <div class="col-md-3">
          <KpiCard
            label="Approved Requests"
            :value="dashboard.approvedRequests"
            icon="bi-check-circle"
            color="success"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Scheduled Requests"
            :value="dashboard.scheduledRequests"
            icon="bi-calendar-check"
            color="primary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Available Released Slots"
            :value="dashboard.availableReleasedSlots"
            icon="bi-box-arrow-up"
            color="info"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Starved Requests"
            :value="dashboard.starvedRequests"
            icon="bi-exclamation-triangle"
            color="danger"
          />
        </div>
      </div>

      <!-- Today status row -->
      <div class="row g-3 mb-4">
        <div class="col-md-4">
          <KpiCard
            label="Today Scheduled Cases"
            :value="dashboard.todayScheduledCases"
            icon="bi-calendar-event"
            color="primary"
          />
        </div>

        <div class="col-md-4">
          <KpiCard
            label="Today In Progress"
            :value="dashboard.todayInProgressCases"
            icon="bi-activity"
            color="info"
          />
        </div>

        <div class="col-md-4">
          <KpiCard
            label="Today Completed"
            :value="dashboard.todayCompletedCases"
            icon="bi-check2-circle"
            color="success"
          />
        </div>
      </div>

      <div class="row g-4">
        <!-- Today's schedule -->
        <div class="col-lg-8">
          <div class="page-card h-100">
            <div class="d-flex align-items-center justify-content-between mb-3">
              <h5 class="mb-0">
                <i class="bi bi-calendar3 me-2 text-primary"></i>
                Today's Schedule
              </h5>

              <router-link
                to="/app/scheduler/cases"
                class="btn btn-sm btn-outline-primary"
              >
                Manage Cases
              </router-link>
            </div>

            <EmptyState
              v-if="!dashboard.todaySchedule || dashboard.todaySchedule.length === 0"
              title="No cases today"
              message="There are no surgical cases scheduled for today."
              icon="bi-calendar-x"
            />

            <div v-else class="table-responsive">
              <table class="table table-hover align-middle">
                <thead>
                  <tr>
                    <th>Case</th>
                    <th>Room</th>
                    <th>Surgeon</th>
                    <th>Surgery</th>
                    <th>Time</th>
                    <th>Status</th>
                  </tr>
                </thead>

                <tbody>
                  <tr
                    v-for="item in dashboard.todaySchedule"
                    :key="item.surgeryId"
                  >
                    <td>#{{ item.surgeryId }}</td>
                    <td>{{ item.roomName }}</td>
                    <td>{{ item.surgeonName }}</td>
                    <td>{{ item.surgeryType }}</td>
                    <td>
                      <div>{{ formatDateTime(item.scheduledStart) }}</div>
                      <small class="text-muted">
                        to {{ formatDateTime(item.scheduledEnd) }}
                      </small>
                    </td>
                    <td>
                      <StatusBadge :status="item.caseStatus" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Quick actions -->
        <div class="col-lg-4">
          <div class="page-card h-100">
            <h5 class="mb-3">
              <i class="bi bi-lightning-charge me-2 text-primary"></i>
              Quick Actions
            </h5>

            <div class="quick-actions">
              <router-link
                to="/app/scheduler/requests"
                class="quick-action"
              >
                <div>
                  <i class="bi bi-inbox"></i>
                </div>
                <span>Review Request Queue</span>
              </router-link>

              <router-link
                to="/app/scheduler/waitlist"
                class="quick-action"
              >
                <div>
                  <i class="bi bi-hourglass-split"></i>
                </div>
                <span>Backfill Released Slots</span>
              </router-link>

              <router-link
                to="/app/scheduler/blocks"
                class="quick-action"
              >
                <div>
                  <i class="bi bi-grid-3x3-gap"></i>
                </div>
                <span>Manage Blocks</span>
              </router-link>

              <router-link
                to="/app/scheduler/utilization"
                class="quick-action"
              >
                <div>
                  <i class="bi bi-bar-chart"></i>
                </div>
                <span>View Utilization</span>
              </router-link>

              <router-link
                to="/app/scheduler/forecast"
                class="quick-action"
              >
                <div>
                  <i class="bi bi-graph-up-arrow"></i>
                </div>
                <span>Forecast Recommendations</span>
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </div>

    <EmptyState
      v-else
      title="Dashboard unavailable"
      message="No dashboard data was returned."
      icon="bi-exclamation-circle"
    />
  </div>
</template>

<style scoped>
.quick-actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.quick-action {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  text-decoration: none;
  color: #111827;
  background: #fff;
  transition: 0.15s ease;
}

.quick-action:hover {
  border-color: #2563eb;
  background: #eff6ff;
  color: #2563eb;
}

.quick-action div {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  background: #eff6ff;
  color: #2563eb;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
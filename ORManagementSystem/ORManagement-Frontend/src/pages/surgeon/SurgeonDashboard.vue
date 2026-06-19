<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import KpiCard from '../../components/common/KpiCard.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import { getSurgeonDashboard } from '../../services/dashboardService'
import { formatDateTime, formatPercent } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const dashboard = ref(null)

const loadDashboard = async () => {
  loading.value = true

  try {
    const response = await getSurgeonDashboard()
    dashboard.value = response.data
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load surgeon dashboard.'

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
      title="Surgeon Dashboard"
      subtitle="View your OR requests, upcoming cases, assigned blocks, and utilization"
      icon="bi-speedometer2"
    >
      <template #actions>
        <router-link
          to="/app/surgeon/request-or-time"
          class="btn btn-primary"
        >
          <i class="bi bi-plus-circle me-1"></i>
          Request OR Time
        </router-link>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else-if="dashboard">
      <!-- KPI cards -->
      <div class="row g-3 mb-4">
        <div class="col-md-3">
          <KpiCard
            label="Assigned Blocks"
            :value="dashboard.assignedBlocks"
            icon="bi-calendar-week"
            color="primary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Upcoming Blocks"
            :value="dashboard.upcomingBlocks"
            icon="bi-calendar-event"
            color="info"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Upcoming Surgeries"
            :value="dashboard.upcomingSurgeries"
            icon="bi-hospital"
            color="success"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Avg Utilization"
            :value="formatPercent(dashboard.averageUtilizationPercent)"
            icon="bi-bar-chart"
            color="warning"
          />
        </div>
      </div>

      <!-- Request status cards -->
      <div class="row g-3 mb-4">
        <div class="col-md-3">
          <KpiCard
            label="Pending Requests"
            :value="dashboard.pendingRequests"
            icon="bi-hourglass-split"
            color="primary"
          />
        </div>

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
            label="Waitlisted Requests"
            :value="dashboard.waitlistedRequests"
            icon="bi-list-ol"
            color="warning"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Scheduled Requests"
            :value="dashboard.scheduledRequests"
            icon="bi-calendar-check"
            color="secondary"
          />
        </div>
      </div>

      <div class="row g-4">
        <!-- Upcoming cases -->
        <div class="col-lg-7">
          <div class="page-card h-100">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <h5 class="mb-0">
                <i class="bi bi-hospital me-2 text-primary"></i>
                Upcoming Cases
              </h5>

              <router-link
                to="/app/surgeon/cases"
                class="btn btn-sm btn-outline-primary"
              >
                View All
              </router-link>
            </div>

            <EmptyState
              v-if="!dashboard.upcomingCases || dashboard.upcomingCases.length === 0"
              title="No upcoming cases"
              message="You do not have any upcoming scheduled cases."
              icon="bi-calendar-x"
            />

            <div v-else class="table-responsive">
              <table class="table table-hover align-middle">
                <thead>
                  <tr>
                    <th>Case</th>
                    <th>Room</th>
                    <th>Surgery</th>
                    <th>Scheduled</th>
                    <th>Status</th>
                  </tr>
                </thead>

                <tbody>
                  <tr
                    v-for="item in dashboard.upcomingCases"
                    :key="item.surgeryId"
                  >
                    <td>#{{ item.surgeryId }}</td>
                    <td>{{ item.roomName }}</td>
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

        <!-- Recent requests -->
        <div class="col-lg-5">
          <div class="page-card h-100">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <h5 class="mb-0">
                <i class="bi bi-list-check me-2 text-primary"></i>
                Recent Requests
              </h5>

              <router-link
                to="/app/surgeon/requests"
                class="btn btn-sm btn-outline-primary"
              >
                View All
              </router-link>
            </div>

            <EmptyState
              v-if="!dashboard.recentRequests || dashboard.recentRequests.length === 0"
              title="No requests"
              message="You have not submitted any OR requests yet."
              icon="bi-inbox"
            />

            <div v-else class="request-list">
              <div
                v-for="item in dashboard.recentRequests"
                :key="item.requestId"
                class="request-item"
              >
                <div class="d-flex justify-content-between gap-2">
                  <div>
                    <strong>#{{ item.requestId }} - {{ item.surgeryType }}</strong>
                    <div class="text-muted small">
                      {{ item.priority }} · {{ item.patientReadiness }}
                    </div>
                    <div class="text-muted small">
                      Created: {{ formatDateTime(item.createdAt) }}
                    </div>
                  </div>

                  <StatusBadge :status="item.requestStatus" />
                </div>
              </div>
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
.request-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.request-item {
  padding: 14px;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  background: #fff;
}
</style>
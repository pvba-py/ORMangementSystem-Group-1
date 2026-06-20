<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import { getMyCases } from '../../services/caseService'
import { formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const cases = ref([])

const loadCases = async () => {
  loading.value = true

  try {
    const response = await getMyCases()
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

onMounted(loadCases)
</script>

<template>
  <div>
    <PageHeader
      title="My Cases"
      subtitle="View your scheduled, in-progress, completed, and cancelled surgical cases"
      icon="bi-hospital"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else class="page-card">
      <EmptyState
        v-if="cases.length === 0"
        title="No cases"
        message="You do not have any surgical cases yet."
        icon="bi-hospital"
      />

      <div v-else class="table-responsive">
        <table class="table table-hover align-middle">
          <thead>
            <tr>
              <th>Case</th>
              <th>Patient</th>
              <th>Room</th>
              <th>Surgery</th>
              <th>Scheduled</th>
              <th>Actual</th>
              <th>Status</th>
              <th>Cancellation</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="item in cases" :key="item.surgeryId">
              <td>#{{ item.surgeryId }}</td>
              <td>
                <div>{{ item.patientName }}</div>
                <small class="text-muted">{{ item.patientMrn }}</small>
              </td>
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
              <td>{{ item.cancellationReason || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
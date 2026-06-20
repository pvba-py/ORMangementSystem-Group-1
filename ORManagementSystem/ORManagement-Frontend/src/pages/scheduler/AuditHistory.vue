<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import { getAuditLogs, getPhiAccessLogs } from '../../services/auditService'
import { formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)

const activeTab = ref('audit')

const auditLogs = ref([])
const phiLogs = ref([])

const auditPaging = ref({
  pageNumber: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false
})

const phiPaging = ref({
  pageNumber: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false
})

const auditFilters = ref({
  entity: '',
  action: '',
  fromDate: '',
  toDate: ''
})

const phiFilters = ref({
  patientId: '',
  userId: '',
  fromDate: '',
  toDate: ''
})

const loadAuditLogs = async () => {
  const params = {
    pageNumber: auditPaging.value.pageNumber,
    pageSize: auditPaging.value.pageSize
  }

  if (auditFilters.value.entity) {
    params.entity = auditFilters.value.entity
  }

  if (auditFilters.value.action) {
    params.action = auditFilters.value.action
  }

  if (auditFilters.value.fromDate) {
    params.fromDate = auditFilters.value.fromDate
  }

  if (auditFilters.value.toDate) {
    params.toDate = auditFilters.value.toDate
  }

  const response = await getAuditLogs(params)

  auditLogs.value = response.data.items || []

  auditPaging.value = {
    pageNumber: response.data.pageNumber,
    pageSize: response.data.pageSize,
    totalCount: response.data.totalCount,
    totalPages: response.data.totalPages,
    hasPreviousPage: response.data.hasPreviousPage,
    hasNextPage: response.data.hasNextPage
  }
}

const loadPhiLogs = async () => {
  const params = {
    pageNumber: phiPaging.value.pageNumber,
    pageSize: phiPaging.value.pageSize
  }

  if (phiFilters.value.patientId) {
    params.patientId = phiFilters.value.patientId
  }

  if (phiFilters.value.userId) {
    params.userId = phiFilters.value.userId
  }

  if (phiFilters.value.fromDate) {
    params.fromDate = phiFilters.value.fromDate
  }

  if (phiFilters.value.toDate) {
    params.toDate = phiFilters.value.toDate
  }

  const response = await getPhiAccessLogs(params)

  phiLogs.value = response.data.items || []

  phiPaging.value = {
    pageNumber: response.data.pageNumber,
    pageSize: response.data.pageSize,
    totalCount: response.data.totalCount,
    totalPages: response.data.totalPages,
    hasPreviousPage: response.data.hasPreviousPage,
    hasNextPage: response.data.hasNextPage
  }
}

const loadPage = async () => {
  loading.value = true

  try {
    await Promise.all([
      loadAuditLogs(),
      loadPhiLogs()
    ])
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load audit history.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const applyAuditFilter = async () => {
  loading.value = true

  try {
    auditPaging.value.pageNumber = 1
    await loadAuditLogs()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load audit logs.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const applyPhiFilter = async () => {
  loading.value = true

  try {
    phiPaging.value.pageNumber = 1
    await loadPhiLogs()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load PHI access logs.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const clearAuditFilters = async () => {
  auditFilters.value = {
    entity: '',
    action: '',
    fromDate: '',
    toDate: ''
  }

  auditPaging.value.pageNumber = 1
  await applyAuditFilter()
}

const clearPhiFilters = async () => {
  phiFilters.value = {
    patientId: '',
    userId: '',
    fromDate: '',
    toDate: ''
  }

  phiPaging.value.pageNumber = 1
  await applyPhiFilter()
}

const goAuditPrevious = async () => {
  if (!auditPaging.value.hasPreviousPage) {
    return
  }

  loading.value = true

  try {
    auditPaging.value.pageNumber -= 1
    await loadAuditLogs()
  } finally {
    loading.value = false
  }
}

const goAuditNext = async () => {
  if (!auditPaging.value.hasNextPage) {
    return
  }

  loading.value = true

  try {
    auditPaging.value.pageNumber += 1
    await loadAuditLogs()
  } finally {
    loading.value = false
  }
}

const goPhiPrevious = async () => {
  if (!phiPaging.value.hasPreviousPage) {
    return
  }

  loading.value = true

  try {
    phiPaging.value.pageNumber -= 1
    await loadPhiLogs()
  } finally {
    loading.value = false
  }
}

const goPhiNext = async () => {
  if (!phiPaging.value.hasNextPage) {
    return
  }

  loading.value = true

  try {
    phiPaging.value.pageNumber += 1
    await loadPhiLogs()
  } finally {
    loading.value = false
  }
}

const changeAuditPageSize = async () => {
  auditPaging.value.pageNumber = 1
  await applyAuditFilter()
}

const changePhiPageSize = async () => {
  phiPaging.value.pageNumber = 1
  await applyPhiFilter()
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Audit History"
      subtitle="Review application audit logs and individual PHI access records"
      icon="bi-shield-check"
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
      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'audit' }"
            @click="activeTab = 'audit'"
          >
            Audit Logs
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'phi' }"
            @click="activeTab = 'phi'"
          >
            PHI Access Logs
          </button>
        </li>
      </ul>

      <!-- Audit Logs Tab -->
      <div v-if="activeTab === 'audit'">
        <div class="page-card mb-4">
          <div class="row g-3 align-items-end">
            <div class="col-md-3">
              <label class="form-label">Entity</label>
              <input
                v-model="auditFilters.entity"
                class="form-control"
                placeholder="e.g. ORRequests"
              />
            </div>

            <div class="col-md-3">
              <label class="form-label">Action</label>
              <input
                v-model="auditFilters.action"
                class="form-control"
                placeholder="e.g. RequestApproved"
              />
            </div>

            <div class="col-md-2">
              <label class="form-label">From Date</label>
              <input
                v-model="auditFilters.fromDate"
                type="date"
                class="form-control"
              />
            </div>

            <div class="col-md-2">
              <label class="form-label">To Date</label>
              <input
                v-model="auditFilters.toDate"
                type="date"
                class="form-control"
              />
            </div>

            <div class="col-md-1">
              <label class="form-label">Size</label>
              <select
                v-model.number="auditPaging.pageSize"
                class="form-select"
                @change="changeAuditPageSize"
              >
                <option :value="10">10</option>
                <option :value="20">20</option>
                <option :value="50">50</option>
                <option :value="100">100</option>
              </select>
            </div>

            <div class="col-md-1">
              <button class="btn btn-primary w-100" @click="applyAuditFilter">
                Apply
              </button>
            </div>

            <div class="col-md-12 text-end">
              <button class="btn btn-outline-secondary btn-sm" @click="clearAuditFilters">
                Clear Filters
              </button>
            </div>
          </div>
        </div>

        <div class="page-card">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <div>
              <strong>Total:</strong> {{ auditPaging.totalCount }}
              <span class="text-muted">
                · Page {{ auditPaging.pageNumber }} of {{ auditPaging.totalPages || 1 }}
              </span>
            </div>

            <div class="btn-group">
              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!auditPaging.hasPreviousPage"
                @click="goAuditPrevious"
              >
                Previous
              </button>

              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!auditPaging.hasNextPage"
                @click="goAuditNext"
              >
                Next
              </button>
            </div>
          </div>

          <EmptyState
            v-if="auditLogs.length === 0"
            title="No audit logs"
            message="No audit records were found."
            icon="bi-shield"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Audit</th>
                  <th>User</th>
                  <th>Role</th>
                  <th>Action</th>
                  <th>Entity</th>
                  <th>Old</th>
                  <th>New</th>
                  <th>Remarks</th>
                  <th>IP</th>
                  <th>Created</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="log in auditLogs" :key="log.auditId">
                  <td>#{{ log.auditId }}</td>
                  <td>{{ log.userId || 'System' }}</td>
                  <td>{{ log.roleName }}</td>
                  <td>
                    <StatusBadge :status="log.action" />
                  </td>
                  <td>
                    {{ log.entity }}
                    <span v-if="log.entityId">#{{ log.entityId }}</span>
                  </td>
                  <td>{{ log.oldValue || '-' }}</td>
                  <td>{{ log.newValue || '-' }}</td>
                  <td>{{ log.remarks || '-' }}</td>
                  <td>{{ log.ipAddress || '-' }}</td>
                  <td>{{ formatDateTime(log.createdAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div
            v-if="auditLogs.length > 0"
            class="d-flex justify-content-end mt-3"
          >
            <div class="btn-group">
              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!auditPaging.hasPreviousPage"
                @click="goAuditPrevious"
              >
                Previous
              </button>

              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!auditPaging.hasNextPage"
                @click="goAuditNext"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- PHI Access Logs Tab -->
      <div v-if="activeTab === 'phi'">
        <div class="page-card mb-4">
          <div class="row g-3 align-items-end">
            <div class="col-md-3">
              <label class="form-label">Patient ID</label>
              <input
                v-model="phiFilters.patientId"
                type="number"
                class="form-control"
              />
            </div>

            <div class="col-md-3">
              <label class="form-label">User ID</label>
              <input
                v-model="phiFilters.userId"
                type="number"
                class="form-control"
              />
            </div>

            <div class="col-md-2">
              <label class="form-label">From Date</label>
              <input
                v-model="phiFilters.fromDate"
                type="date"
                class="form-control"
              />
            </div>

            <div class="col-md-2">
              <label class="form-label">To Date</label>
              <input
                v-model="phiFilters.toDate"
                type="date"
                class="form-control"
              />
            </div>

            <div class="col-md-1">
              <label class="form-label">Size</label>
              <select
                v-model.number="phiPaging.pageSize"
                class="form-select"
                @change="changePhiPageSize"
              >
                <option :value="10">10</option>
                <option :value="20">20</option>
                <option :value="50">50</option>
                <option :value="100">100</option>
              </select>
            </div>

            <div class="col-md-1">
              <button class="btn btn-primary w-100" @click="applyPhiFilter">
                Apply
              </button>
            </div>

            <div class="col-md-12 text-end">
              <button class="btn btn-outline-secondary btn-sm" @click="clearPhiFilters">
                Clear Filters
              </button>
            </div>
          </div>
        </div>

        <div class="page-card">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <div>
              <strong>Total:</strong> {{ phiPaging.totalCount }}
              <span class="text-muted">
                · Page {{ phiPaging.pageNumber }} of {{ phiPaging.totalPages || 1 }}
              </span>
            </div>

            <div class="btn-group">
              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!phiPaging.hasPreviousPage"
                @click="goPhiPrevious"
              >
                Previous
              </button>

              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!phiPaging.hasNextPage"
                @click="goPhiNext"
              >
                Next
              </button>
            </div>
          </div>

          <EmptyState
            v-if="phiLogs.length === 0"
            title="No PHI access logs"
            message="No individual patient access records were found."
            icon="bi-file-lock"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Access</th>
                  <th>User</th>
                  <th>Patient</th>
                  <th>Access Type</th>
                  <th>Context</th>
                  <th>IP</th>
                  <th>User Agent</th>
                  <th>Accessed</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="log in phiLogs" :key="log.accessId">
                  <td>#{{ log.accessId }}</td>
                  <td>#{{ log.userId }}</td>
                  <td>#{{ log.patientId }}</td>
                  <td>
                    <StatusBadge :status="log.accessType" />
                  </td>
                  <td>{{ log.context || '-' }}</td>
                  <td>{{ log.ipAddress || '-' }}</td>
                  <td class="text-truncate" style="max-width: 240px;">
                    {{ log.userAgent || '-' }}
                  </td>
                  <td>{{ formatDateTime(log.accessedAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div
            v-if="phiLogs.length > 0"
            class="d-flex justify-content-end mt-3"
          >
            <div class="btn-group">
              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!phiPaging.hasPreviousPage"
                @click="goPhiPrevious"
              >
                Previous
              </button>

              <button
                class="btn btn-outline-primary btn-sm"
                :disabled="!phiPaging.hasNextPage"
                @click="goPhiNext"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
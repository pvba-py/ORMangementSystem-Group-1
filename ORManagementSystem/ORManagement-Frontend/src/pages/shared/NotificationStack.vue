<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getNotificationStack,
  getReceivedNotifications,
  getSentNotifications
} from '../../services/notificationService'
import { formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const activeTab = ref('all')
const notifications = ref([])
const loading = ref(false)

const loadNotifications = async () => {
  loading.value = true

  try {
    let response

    if (activeTab.value === 'received') {
      response = await getReceivedNotifications(50)
    } else if (activeTab.value === 'sent') {
      response = await getSentNotifications(50)
    } else {
      response = await getNotificationStack(50)
    }

    notifications.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load notifications.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const setTab = async (tab) => {
  activeTab.value = tab
  await loadNotifications()
}

onMounted(loadNotifications)
</script>

<template>
  <div>
    <PageHeader
      title="Notification Stack"
      subtitle="Recent workflow events from the audit event feed"
      icon="bi-bell"
    />

    <div class="page-card">
      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'all' }"
            @click="setTab('all')"
          >
            All
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'received' }"
            @click="setTab('received')"
          >
            Received
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'sent' }"
            @click="setTab('sent')"
          >
            Sent
          </button>
        </li>
      </ul>

      <LoadingSpinner v-if="loading" />

      <EmptyState
        v-else-if="notifications.length === 0"
        title="No notifications"
        message="There are no workflow events to display."
        icon="bi-bell-slash"
      />

      <div v-else class="notification-list">
        <div
          v-for="item in notifications"
          :key="item.auditId"
          class="notification-item"
        >
          <div class="notification-icon">
            <i class="bi bi-info-circle"></i>
          </div>

          <div class="flex-grow-1">
            <div class="d-flex align-items-center justify-content-between mb-1">
              <strong>{{ item.message }}</strong>
              <small class="text-muted">
                {{ formatDateTime(item.createdAt) }}
              </small>
            </div>

            <div class="d-flex align-items-center gap-2 flex-wrap">
              <StatusBadge :status="item.action" />
              <span class="text-muted small">
                Entity: {{ item.entity }}
                <span v-if="item.entityId">#{{ item.entityId }}</span>
              </span>
              <span class="text-muted small">
                By: {{ item.roleName }}
              </span>
            </div>

            <p v-if="item.remarks" class="text-muted mb-0 mt-2 small">
              {{ item.remarks }}
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.notification-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.notification-item {
  display: flex;
  gap: 14px;
  padding: 16px;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  background: #fff;
}

.notification-icon {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: #eff6ff;
  color: #2563eb;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.nav-link {
  border-radius: 8px;
}
</style>
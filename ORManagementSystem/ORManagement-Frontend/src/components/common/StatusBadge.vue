<script setup>
const props = defineProps({
  status: {
    type: String,
    required: true
  }
})

const getClass = () => {
  const status = props.status

  if (['Approved', 'Scheduled', 'Completed', 'Good', 'Assigned', 'Published'].includes(status)) {
    return 'bg-success'
  }

  if (['PendingReview', 'Pending', 'Open', 'Scheduled'].includes(status)) {
    return 'bg-primary'
  }

  if (['Waitlisted', 'Modified', 'Moderate', 'Matched', 'Scheduling', 'Cutoff'].includes(status)) {
    return 'bg-warning text-dark'
  }

  if (['Rejected', 'Cancelled', 'Expired', 'Unused'].includes(status)) {
    return 'bg-danger'
  }

  if (['Underutilized', 'Released', 'PartiallyBooked'].includes(status)) {
    return 'bg-secondary'
  }

  if (['InProgress'].includes(status)) {
    return 'bg-info text-dark'
  }

  return 'bg-light text-dark'
}
</script>

<template>
  <span :class="`badge ${getClass()}`">
    {{ status }}
  </span>
</template>
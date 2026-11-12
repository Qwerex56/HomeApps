<script setup lang="ts">
import HomeMixSettings from './HomeMixSettings.vue'
import MixWidgets from './MixWidgets.vue'

// "Moje aplikacje": add-ons on my own account, their widgets for the place Account, and adding more.
</script>

<template>
  <div class="flex flex-col gap-6">
    <MixWidgets :household="null" place="Account" />
    <HomeMixSettings :target="{ kind: 'User' }" can-manage />
  </div>
</template>

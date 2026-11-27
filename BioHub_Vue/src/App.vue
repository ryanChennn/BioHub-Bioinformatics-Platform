<script setup>
import SiteNavbar from './components/SiteNavbar.vue'
import SiteFooter from './components/SiteFooter.vue'
</script>

<template>
  <div class="app-shell">
    <SiteNavbar />
    <main id="main-content"><RouterView /></main>
    <SiteFooter />
  </div>
</template>

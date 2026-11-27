<script setup>
defineProps({icon: String, tag: String, title: String, description: String})
</script>
<template>
  <article class="feature-card">
    <div class="feature-icon"><i :class="`bi bi-${icon}`" aria-hidden="true"></i></div>
    <span class="eyebrow feature-tag">{{ tag }}</span>
    <h3>{{ title }}</h3>
    <p>{{ description }}</p>
    <RouterLink to="/features" class="feature-link">了解更多 <i class="bi bi-arrow-right"></i></RouterLink>
  </article>
</template>

<script setup>
import { ref, watch } from "vue";
import { useRoute } from "vue-router";
const route = useRoute();
const menuOpen = ref(false);
watch(
  () => route.fullPath,
  () => {
    menuOpen.value = false;
  },
);
const backendUrl = import.meta.env.VITE_BACKEND_URL || "";
// const loginUrl = backendUrl ? `${backendUrl.replace(/\/$/, '')}/Account/Login` : null
const loginUrl = "/Account/Login";
</script>

<template>
  <header class="site-header">
    <div class="container-xl header-inner">
      <RouterLink to="/" class="brand" aria-label="BioHub 首頁">
        <span class="brand-mark"
          ><i class="bi bi-activity" aria-hidden="true"></i
        ></span>
        <span class="brand-copy"
          ><strong>BioHub</strong><small>BIOMEDICAL RESEARCH</small></span
        >
      </RouterLink>
      <button
        class="mobile-menu-toggle"
        type="button"
        @click="menuOpen = !menuOpen"
        :aria-expanded="menuOpen"
        aria-controls="primary-nav"
        aria-label="切換導覽選單"
      >
        <i :class="menuOpen ? 'bi bi-x-lg' : 'bi bi-list'"></i>
      </button>
      <nav
        id="primary-nav"
        class="primary-nav"
        :class="{ 'is-open': menuOpen }"
        aria-label="主要導覽"
      >
        <RouterLink to="/">首頁</RouterLink>
        <RouterLink to="/features">平台功能</RouterLink>
        <RouterLink to="/about">關於 BioHub</RouterLink>
        <a v-if="loginUrl" :href="loginUrl" class="btn-login"
          >登入平台 <i class="bi bi-arrow-up-right ms-1"></i
        ></a>
        <span
          v-else
          class="btn-login disabled-link"
          title="請先設定 VITE_BACKEND_URL"
          >登入平台 <i class="bi bi-arrow-up-right ms-1"></i
        ></span>
      </nav>
    </div>
  </header>
</template>
